using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Moimo.Editor.BakedScene
{
    public class BakedSceneWindow : OdinEditorWindow
    {
        [MenuItem("Tools/Moimo/Baked Scene")]
        private static void Open()
        {
            var window = GetWindow<BakedSceneWindow>();
            window.titleContent = new GUIContent("Baked Scene");
            window.minSize = new Vector2(520, 640);
            window.Show();
        }

        [BoxGroup("Bake Options")]
        [HideLabel]
        public BakeOptions options = new BakeOptions();

        [BoxGroup("Environment Settings")]
        [LabelText("Apply Environment Settings To All Scenes")]
        public bool applyEnvironmentSettings = false;

        [BoxGroup("Environment Settings")]
        [ShowIf("applyEnvironmentSettings")]
        [HideLabel]
        public EnvironmentSettingsData environmentSettings = new EnvironmentSettingsData();

        [BoxGroup("Environment Settings")]
        [ShowIf("applyEnvironmentSettings")]
        [HorizontalGroup("Environment Settings/Buttons")]
        [Button("Copy From Current Scene", ButtonSizes.Medium)]
        private void CopyEnvFromCurrent()
        {
            environmentSettings.CopyFromCurrentScene();
            Log("Environment settings copied from current scene");
        }

        [BoxGroup("Environment Settings")]
        [ShowIf("applyEnvironmentSettings")]
        [HorizontalGroup("Environment Settings/Buttons")]
        [Button("Apply To Current Scene", ButtonSizes.Medium)]
        private void ApplyEnvToCurrent()
        {
            string sunLog = environmentSettings.ApplyToCurrentScene();
            EditorSceneManager.MarkAllScenesDirty();
            Log("Environment settings applied to current scene");
            Log(sunLog);
        }

        [BoxGroup("Scene Source")]
        [LabelText("Target Folders")]
        [ListDrawerSettings(Expanded = true, DraggableItems = true, ShowPaging = false)]
        [FolderPath]
        public List<string> targetFolders = new List<string> { "Assets" };

        [BoxGroup("Scene Source")]
        [LabelText("Include Subfolders")]
        public bool includeSubfolders = true;

        [BoxGroup("Scene Source")]
        [LabelText("Name Filter (optional)")]
        public string nameFilter = "";

        [BoxGroup("Scenes")]
        [InfoBox("Klik tombol X di kanan baris untuk menghapus scene dari list (exclude).")]
        [TableList(AlwaysExpanded = true, DrawScrollView = true, MaxScrollViewHeight = 280, NumberOfItemsPerPage = 50)]
        [EnableIf("@!isProcessing")]
        public List<SceneBakeItem> scenes = new List<SceneBakeItem>();

        [BoxGroup("Scenes")]
        [HorizontalGroup("Scenes/ListButtons")]
        [Button("Remove Selected", ButtonSizes.Medium), GUIColor(1f, 0.6f, 0.3f)]
        [EnableIf("@!isProcessing && scenes.Count > 0")]
        private void RemoveSelectedScenes()
        {
            int removed = scenes.RemoveAll(s => s.selected);
            if (removed > 0)
                Log($"Removed {removed} scene(s) from list");
            else
                Log("No scene selected to remove");
        }

        [BoxGroup("Scenes")]
        [HorizontalGroup("Scenes/ListButtons")]
        [Button("Clear List", ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f)]
        [EnableIf("@!isProcessing && scenes.Count > 0")]
        private void ClearSceneList()
        {
            scenes.Clear();
            Log("Scene list cleared");
        }

        [BoxGroup("Scenes")]
        [HorizontalGroup("Scenes/ListButtons")]
        [Button("Select All", ButtonSizes.Medium)]
        [EnableIf("@!isProcessing && scenes.Count > 0")]
        private void SelectAllScenes()
        {
            foreach (var s in scenes)
                s.selected = true;
        }

        [BoxGroup("Scenes")]
        [HorizontalGroup("Scenes/ListButtons")]
        [Button("Deselect All", ButtonSizes.Medium)]
        [EnableIf("@!isProcessing && scenes.Count > 0")]
        private void DeselectAllScenes()
        {
            foreach (var s in scenes)
                s.selected = false;
        }

        [BoxGroup("Progress"), ShowIf("@isProcessing || hasFinished")]
        [ProgressBar(0, 100), ReadOnly]
        public float progress;

        [BoxGroup("Progress"), ShowIf("@isProcessing || hasFinished")]
        [ReadOnly, LabelText("Current Scene")]
        public string currentSceneName = "-";

        [BoxGroup("Progress"), ShowIf("@isProcessing || hasFinished")]
        [ReadOnly, LabelText("Status")]
        public string statusText = "Idle";

        [BoxGroup("Progress"), ShowIf("@isProcessing || hasFinished")]
        [ReadOnly, LabelText("Log")]
        [MultiLineProperty(10)]
        public string log = "";

        private bool isProcessing;
        private bool hasFinished;
        private BakedSceneProcessor processor;

        [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
        [EnableIf("@!isProcessing")]
        [PropertyOrder(100)]
        private void ScanScenes()
        {
            scenes.Clear();

            if (targetFolders == null || targetFolders.Count == 0)
            {
                EditorUtility.DisplayDialog("Baked Scene", "Tambahkan minimal satu folder.", "OK");
                return;
            }

            var validFolders = new List<string>();
            foreach (var folder in targetFolders)
            {
                if (string.IsNullOrWhiteSpace(folder))
                    continue;

                var normalized = folder.Replace('\\', '/').TrimEnd('/');
                if (!AssetDatabase.IsValidFolder(normalized))
                {
                    Log($"Folder tidak valid, dilewati: {folder}");
                    continue;
                }
                validFolders.Add(normalized);
            }

            if (validFolders.Count == 0)
            {
                EditorUtility.DisplayDialog("Baked Scene", "Tidak ada folder yang valid.", "OK");
                return;
            }

            var filter = string.IsNullOrWhiteSpace(nameFilter) ? null : nameFilter.Trim().ToLowerInvariant();
            var addedPaths = new HashSet<string>();

            foreach (var folder in validFolders)
            {
                var guids = AssetDatabase.FindAssets("t:Scene", new[] { folder });

                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');

                    if (!includeSubfolders)
                    {
                        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
                        if (parent != folder)
                            continue;
                    }

                    if (addedPaths.Contains(path))
                        continue;

                    var sceneName = Path.GetFileNameWithoutExtension(path);
                    if (filter != null && !sceneName.ToLowerInvariant().Contains(filter))
                        continue;

                    addedPaths.Add(path);
                    scenes.Add(new SceneBakeItem
                    {
                        path = path,
                        name = sceneName,
                        status = "Waiting",
                        hasLightingData = false,
                        hasOcclusionData = false,
                        selected = false
                    });
                }
            }

            Log($"Found {scenes.Count} scene(s) from {validFolders.Count} folder(s)");
        }

        [Button(ButtonSizes.Large), GUIColor(0.3f, 1f, 0.3f)]
        [EnableIf("@!isProcessing && scenes.Count > 0")]
        [PropertyOrder(101)]
        private void StartBakeAll()
        {
            if (!options.HasAnyBakeTarget)
            {
                EditorUtility.DisplayDialog("Baked Scene", "Pilih minimal satu target bake atau aktifkan Dry Run.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var pathsToBake = scenes.Select(s => s.path).ToList();
            if (pathsToBake.Count == 0)
            {
                EditorUtility.DisplayDialog("Baked Scene", "Tidak ada scene di list.", "OK");
                return;
            }

            isProcessing = true;
            hasFinished = false;
            progress = 0f;
            log = "";
            currentSceneName = "-";
            statusText = "Starting...";

            foreach (var s in scenes)
                s.status = "Waiting";

            processor = new BakedSceneProcessor();
            processor.OnLog += Log;
            processor.OnProgressChanged += OnProcessorProgress;
            processor.OnFinished += OnProcessorFinished;

            processor.Start(pathsToBake, scenes, options, applyEnvironmentSettings, environmentSettings);
        }

        [Button(ButtonSizes.Medium), GUIColor(1f, 0.45f, 0.35f)]
        [EnableIf("@isProcessing")]
        [PropertyOrder(102)]
        private void StopBake()
        {
            processor?.RequestStop();
            statusText = "Stopping...";
        }

        [Button("Clear Log"), PropertyOrder(103)]
        [EnableIf("@!isProcessing")]
        private void ClearLog()
        {
            log = "";
        }

        private void OnProcessorProgress()
        {
            if (processor == null) return;

            progress = processor.Progress;
            currentSceneName = processor.CurrentSceneName;

            switch (processor.CurrentState)
            {
                case BakedSceneProcessor.State.Opening:
                    statusText = $"Opening: {currentSceneName}";
                    break;
                case BakedSceneProcessor.State.ApplyingSettings:
                    statusText = $"Applying Settings: {currentSceneName}";
                    break;
                case BakedSceneProcessor.State.BakingLighting:
                    statusText = $"Baking Lighting: {currentSceneName}";
                    break;
                case BakedSceneProcessor.State.BakingOcclusion:
                    statusText = $"Baking Occlusion: {currentSceneName}";
                    break;
                case BakedSceneProcessor.State.BakingReflectionProbes:
                    statusText = $"Baking Reflection Probes: {currentSceneName}";
                    break;
                case BakedSceneProcessor.State.FinishingScene:
                    statusText = $"Saving: {currentSceneName}";
                    break;
                default:
                    statusText = processor.CurrentState.ToString();
                    break;
            }

            Repaint();
        }

        private void OnProcessorFinished()
        {
            isProcessing = false;
            hasFinished = true;
            progress = 100f;
            statusText = processor != null && processor.CurrentState == BakedSceneProcessor.State.Completed
                ? "All scenes finished"
                : "Stopped";

            if (processor != null)
            {
                processor.OnLog -= Log;
                processor.OnProgressChanged -= OnProcessorProgress;
                processor.OnFinished -= OnProcessorFinished;
            }

            Repaint();
        }

        private void Log(string msg)
        {
            var time = System.DateTime.Now.ToString("HH:mm:ss");
            log += $"[{time}] {msg}\n";
            if (log.Length > 6000)
                log = log.Substring(log.Length - 5000);
            Repaint();
        }

        protected override void OnDestroy()
        {
            if (processor != null && processor.IsRunning)
                processor.RequestStop();
            base.OnDestroy();
        }
    }
}
