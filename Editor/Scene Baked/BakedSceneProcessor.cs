using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Moimo.Editor.BakedScene
{
    public class BakedSceneProcessor
    {
        public enum State
        {
            Idle,
            Opening,
            ApplyingSettings,
            BakingLighting,
            BakingOcclusion,
            BakingReflectionProbes,
            FinishingScene,
            Completed,
            Stopped
        }

        public State CurrentState { get; private set; } = State.Idle;
        public int CurrentIndex { get; private set; }
        public string CurrentSceneName { get; private set; } = "-";
        public float Progress { get; private set; }
        public bool IsRunning => CurrentState != State.Idle && CurrentState != State.Completed && CurrentState != State.Stopped;

        public event Action<string> OnLog;
        public event Action OnProgressChanged;
        public event Action OnFinished;

        private List<string> _scenePaths;
        private BakeOptions _options;
        private EnvironmentSettingsData _envSettings;
        private bool _applyEnv;
        private bool _stopRequested;
        private List<SceneBakeItem> _items;
        private Queue<State> _pendingSteps;

        private int _countDone;
        private int _countFailed;
        private int _countSkipped;

        public void Start(List<string> scenePaths, List<SceneBakeItem> items, BakeOptions options, bool applyEnv, EnvironmentSettingsData envSettings)
        {
            _scenePaths = scenePaths;
            _items = items;
            _options = options;
            _applyEnv = applyEnv;
            _envSettings = envSettings;
            _stopRequested = false;
            CurrentIndex = 0;
            Progress = 0f;
            CurrentState = State.Idle;
            CurrentSceneName = "-";
            _pendingSteps = new Queue<State>();
            _countDone = 0;
            _countFailed = 0;
            _countSkipped = 0;

            EditorApplication.update += Tick;
            Log("=== Bake process started ===");
            if (_options.dryRun)
                Log(">>> DRY RUN MODE: No actual baking will be performed <<<");
        }

        public void RequestStop()
        {
            _stopRequested = true;
            Log("Stop requested...");
        }

        private void Tick()
        {
            if (_stopRequested)
            {
                Finish(false, "Stopped by user");
                return;
            }

            switch (CurrentState)
            {
                case State.Idle:
                    if (CurrentIndex >= _scenePaths.Count)
                    {
                        Finish(true, "All scenes finished");
                        return;
                    }
                    CurrentState = State.Opening;
                    break;

                case State.Opening:
                    OpenScene();
                    break;

                case State.ApplyingSettings:
                    ApplySettings();
                    StartNextPendingStep();
                    break;

                case State.BakingLighting:
                    if (!Lightmapping.isRunning)
                    {
                        Log("  → Lighting finished");
                        StartNextPendingStep();
                    }
                    break;

                case State.BakingOcclusion:
                    if (!StaticOcclusionCulling.isRunning)
                    {
                        Log("  → Occlusion finished");
                        StartNextPendingStep();
                    }
                    break;

                case State.BakingReflectionProbes:
                    BakeReflectionProbes();
                    StartNextPendingStep();
                    break;

                case State.FinishingScene:
                    FinishCurrentScene();
                    break;
            }

            OnProgressChanged?.Invoke();
        }

        private void OpenScene()
        {
            var path = _scenePaths[CurrentIndex];
            CurrentSceneName = System.IO.Path.GetFileNameWithoutExtension(path);
            SetItemStatus("Processing");

            Progress = (float)CurrentIndex / Mathf.Max(1, _scenePaths.Count) * 100f;
            Log($"[{CurrentIndex + 1}/{_scenePaths.Count}] Opening → {path}");

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Log("  → ERROR: Failed to open scene");
                SetItemStatus("Failed");
                _countFailed++;
                CurrentIndex++;
                CurrentState = State.Idle;
                return;
            }

            if (_options.skipEmptyStaticScenes && !HasStaticRenderers())
            {
                Log("  → Skipped (no static objects)");
                SetItemStatus("Skipped");
                _countSkipped++;
                CurrentIndex++;
                CurrentState = State.Idle;
                return;
            }

            if (!_options.forceRebake && !_options.dryRun && _options.bakeLighting && Lightmapping.lightingDataAsset != null)
            {
                Log("  → Skipped (already has lighting data)");
                SetItemStatus("Skipped");
                _countSkipped++;
                CurrentIndex++;
                CurrentState = State.Idle;
                return;
            }

            BuildPendingSteps();
            CurrentState = State.ApplyingSettings;
        }

        private void BuildPendingSteps()
        {
            _pendingSteps.Clear();

            if (!_options.dryRun)
            {
                if (_options.bakeLighting)
                    _pendingSteps.Enqueue(State.BakingLighting);

                if (_options.bakeOcclusion)
                    _pendingSteps.Enqueue(State.BakingOcclusion);

                if (_options.bakeReflectionProbes)
                    _pendingSteps.Enqueue(State.BakingReflectionProbes);
            }

            _pendingSteps.Enqueue(State.FinishingScene);
        }

        private void ApplySettings()
        {
            if (_applyEnv && _envSettings != null)
            {
                string sunLog = _envSettings.ApplyToCurrentScene();
                Log("  → Environment settings applied");
                Log("  → " + sunLog);
            }
        }

        private void StartNextPendingStep()
        {
            if (_pendingSteps.Count == 0)
            {
                CurrentState = State.FinishingScene;
                return;
            }

            var next = _pendingSteps.Dequeue();
            CurrentState = next;

            switch (next)
            {
                case State.BakingLighting:
                    StartLightingBake();
                    break;
                case State.BakingOcclusion:
                    StartOcclusionBake();
                    break;
                case State.BakingReflectionProbes:
                    break;
                case State.FinishingScene:
                    break;
            }
        }

        private void StartLightingBake()
        {
            Log("  → Bake Lighting...");
            if (_options.clearLightingData)
                Lightmapping.Clear();
            Lightmapping.BakeAsync();
        }

        private void StartOcclusionBake()
        {
            Log("  → Bake Occlusion...");
            StaticOcclusionCulling.GenerateInBackground();
        }

        private void BakeReflectionProbes()
        {
            Log("  → Baking Reflection Probes...");
            var probes = Object.FindObjectsOfType<ReflectionProbe>();
            int count = 0;
            foreach (var probe in probes)
            {
                if (probe.mode == UnityEngine.Rendering.ReflectionProbeMode.Baked)
                    count++;
            }
            Log($"  → Found {count} baked reflection probe(s)");
        }

        private void FinishCurrentScene()
        {
            if (_options.saveAfterBake)
            {
                EditorSceneManager.MarkAllScenesDirty();
                EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
            }

            SetItemStatus("Done");
            _countDone++;
            Log($"  → Done: {CurrentSceneName}");

            CurrentIndex++;
            Progress = (float)CurrentIndex / Mathf.Max(1, _scenePaths.Count) * 100f;
            CurrentState = State.Idle;
        }

        private void Finish(bool success, string message)
        {
            EditorApplication.update -= Tick;
            CurrentState = success ? State.Completed : State.Stopped;
            Progress = 100f;

            Log("----------------------------------------");
            Log($"Summary → Done: {_countDone} | Failed: {_countFailed} | Skipped: {_countSkipped}");
            Log($"=== {message} ===");

            OnFinished?.Invoke();
        }

        private void SetItemStatus(string status)
        {
            if (_items != null && CurrentIndex < _items.Count)
                _items[CurrentIndex].status = status;
        }

        private bool HasStaticRenderers()
        {
            var renderers = Object.FindObjectsOfType<Renderer>();
            foreach (var r in renderers)
            {
                if (GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.ContributeGI))
                    return true;
            }
            return false;
        }

        private void Log(string msg)
        {
            OnLog?.Invoke(msg);
        }
    }
}
