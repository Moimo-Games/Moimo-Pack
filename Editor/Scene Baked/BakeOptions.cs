using Sirenix.OdinInspector;
using UnityEngine;

namespace Moimo.Editor.BakedScene
{
    [System.Serializable]
    public class BakeOptions
    {
        [BoxGroup("Bake Targets")]
        [LabelText("Bake Lighting")]
        public bool bakeLighting = true;

        [BoxGroup("Bake Targets")]
        [LabelText("Bake Occlusion")]
        public bool bakeOcclusion = true;

        [BoxGroup("Bake Targets")]
        [LabelText("Bake Reflection Probes")]
        public bool bakeReflectionProbes = false;

        [BoxGroup("Lighting Options")]
        [LabelText("Clear Lighting Data Before Bake")]
        public bool clearLightingData = true;

        [BoxGroup("Lighting Options")]
        [LabelText("Force Rebake Even If Already Baked")]
        public bool forceRebake = true;

        [BoxGroup("Scene Handling")]
        [LabelText("Save Scene After Bake")]
        public bool saveAfterBake = true;

        [BoxGroup("Scene Handling")]
        [LabelText("Skip Scenes Without Static Objects")]
        public bool skipEmptyStaticScenes = false;

        [BoxGroup("Advanced")]
        [LabelText("Dry Run (Apply Settings Only, No Bake)")]
        [Tooltip("Hanya apply environment settings + buka scene, tidak menjalankan bake.")]
        public bool dryRun = false;

        [BoxGroup("Advanced")]
        [LabelText("Log Detailed Sun Search")]
        public bool logSunSearchDetails = true;

        public bool HasAnyBakeTarget => bakeLighting || bakeOcclusion || bakeReflectionProbes || dryRun;
    }
}
