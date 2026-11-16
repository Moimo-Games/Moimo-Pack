using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;

namespace Moimo.Editor.BakedScene
{
    public enum SunSourceMode
    {
        Manual,
        AutoFindInScene,
        AutoFindByName
    }

    [System.Serializable]
    public class EnvironmentSettingsData
    {
        [FoldoutGroup("Environment", Expanded = true)]
        [LabelText("Skybox Material")]
        public Material skyboxMaterial;

        [FoldoutGroup("Environment")]
        [LabelText("Sun Source Mode")]
        public SunSourceMode sunSourceMode = SunSourceMode.Manual;

        [FoldoutGroup("Environment")]
        [ShowIf("@sunSourceMode == SunSourceMode.Manual")]
        [LabelText("Sun Source (Manual)")]
        public Light sunSourceManual;

        [FoldoutGroup("Environment")]
        [ShowIf("@sunSourceMode == SunSourceMode.AutoFindByName")]
        [LabelText("Sun GameObject Name")]
        public string sunGameObjectName = "Directional Light";

        [FoldoutGroup("Environment")]
        [ShowIf("@sunSourceMode == SunSourceMode.AutoFindInScene")]
        [InfoBox("Akan mencari Directional Light pertama di scene. Jika ada beberapa, dipilih yang aktif & intensity tertinggi.")]
        [LabelText("Prefer Active Only")]
        public bool preferActiveDirectional = true;

        [FoldoutGroup("Environment")]
        [LabelText("Realtime Shadow Color")]
        public Color realtimeShadowColor = new Color(0.259f, 0.278f, 0.337f, 1f);

        [FoldoutGroup("Environment")]
        [LabelText("Environment Lighting Source")]
        public AmbientMode ambientMode = AmbientMode.Skybox;

        [FoldoutGroup("Environment")]
        [ShowIf("@ambientMode == AmbientMode.Flat")]
        [LabelText("Ambient Color")]
        [ColorUsage(true, true)]
        public Color ambientColor = new Color(0.212f, 0.227f, 0.259f, 1f);

        [FoldoutGroup("Environment")]
        [LabelText("Environment Reflections Source")]
        public DefaultReflectionMode reflectionMode = DefaultReflectionMode.Skybox;

        [FoldoutGroup("Environment")]
        [LabelText("Resolution")]
        [ValueDropdown("ReflectionResolutions")]
        public int reflectionResolution = 128;

        private static readonly int[] ReflectionResolutions = { 16, 32, 64, 128, 256, 512, 1024, 2048 };

        [FoldoutGroup("Environment")]
        [LabelText("Compression")]
        public ReflectionCubemapCompression reflectionCompression = ReflectionCubemapCompression.Auto;

        [FoldoutGroup("Environment")]
        [LabelText("Intensity Multiplier")]
        [Range(0f, 1f)]
        public float reflectionIntensity = 1f;

        [FoldoutGroup("Environment")]
        [LabelText("Bounces")]
        [Range(1, 5)]
        public int reflectionBounces = 1;

        [FoldoutGroup("Other Settings")]
        [LabelText("Fog")]
        public bool fog = false;

        [FoldoutGroup("Other Settings")]
        [ShowIf("fog")]
        [LabelText("Fog Color")]
        public Color fogColor = Color.gray;

        [FoldoutGroup("Other Settings")]
        [ShowIf("fog")]
        [LabelText("Fog Mode")]
        public FogMode fogMode = FogMode.ExponentialSquared;

        [FoldoutGroup("Other Settings")]
        [ShowIf("fog")]
        [LabelText("Fog Density")]
        public float fogDensity = 0.01f;

        [FoldoutGroup("Other Settings")]
        [LabelText("Halo Strength")]
        [Range(0f, 1f)]
        public float haloStrength = 0.5f;

        [FoldoutGroup("Other Settings")]
        [LabelText("Flare Fade Speed")]
        public float flareFadeSpeed = 3f;

        [FoldoutGroup("Other Settings")]
        [LabelText("Flare Strength")]
        [Range(0f, 1f)]
        public float flareStrength = 1f;

        public Light ResolveSunSource()
        {
            switch (sunSourceMode)
            {
                case SunSourceMode.Manual:
                    return sunSourceManual;

                case SunSourceMode.AutoFindByName:
                    return FindSunByName(sunGameObjectName);

                case SunSourceMode.AutoFindInScene:
                    return FindBestDirectionalLight(preferActiveDirectional);

                default:
                    return null;
            }
        }

        public string ApplyToCurrentScene()
        {
            RenderSettings.skybox = skyboxMaterial;

            var resolvedSun = ResolveSunSource();
            RenderSettings.sun = resolvedSun;

            string sunLog = resolvedSun != null
                ? $"Sun set to: {resolvedSun.name}"
                : "Sun Source not found";

            RenderSettings.subtractiveShadowColor = realtimeShadowColor;
            RenderSettings.ambientMode = ambientMode;

            if (ambientMode == AmbientMode.Flat)
                RenderSettings.ambientLight = ambientColor;

            RenderSettings.defaultReflectionMode = reflectionMode;
            RenderSettings.defaultReflectionResolution = reflectionResolution;
            RenderSettings.reflectionIntensity = reflectionIntensity;
            RenderSettings.reflectionBounces = reflectionBounces;

            RenderSettings.fog = fog;
            if (fog)
            {
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogMode = fogMode;
                RenderSettings.fogDensity = fogDensity;
            }

            RenderSettings.haloStrength = haloStrength;
            RenderSettings.flareFadeSpeed = flareFadeSpeed;
            RenderSettings.flareStrength = flareStrength;

            return sunLog;
        }

        public void CopyFromCurrentScene()
        {
            skyboxMaterial = RenderSettings.skybox;
            sunSourceManual = RenderSettings.sun;
            sunSourceMode = SunSourceMode.Manual;
            realtimeShadowColor = RenderSettings.subtractiveShadowColor;
            ambientMode = RenderSettings.ambientMode;
            ambientColor = RenderSettings.ambientLight;
            reflectionMode = RenderSettings.defaultReflectionMode;
            reflectionResolution = RenderSettings.defaultReflectionResolution;
            reflectionIntensity = RenderSettings.reflectionIntensity;
            reflectionBounces = RenderSettings.reflectionBounces;
            fog = RenderSettings.fog;
            fogColor = RenderSettings.fogColor;
            fogMode = RenderSettings.fogMode;
            fogDensity = RenderSettings.fogDensity;
            haloStrength = RenderSettings.haloStrength;
            flareFadeSpeed = RenderSettings.flareFadeSpeed;
            flareStrength = RenderSettings.flareStrength;
        }

        private static Light FindSunByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            var go = GameObject.Find(objectName);
            if (go == null)
                return null;

            return go.GetComponent<Light>();
        }

        private static Light FindBestDirectionalLight(bool preferActive)
        {
            var lights = Object.FindObjectsOfType<Light>();
            Light best = null;
            float bestScore = -1f;

            foreach (var light in lights)
            {
                if (light.type != LightType.Directional)
                    continue;

                if (preferActive && !light.gameObject.activeInHierarchy)
                    continue;

                float score = light.intensity;
                if (light.enabled)
                    score += 1000f;
                if (light.gameObject.activeInHierarchy)
                    score += 100f;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = light;
                }
            }

            return best;
        }
    }
}
