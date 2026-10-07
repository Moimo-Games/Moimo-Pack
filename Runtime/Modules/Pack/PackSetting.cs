using System.Collections;
using System.IO;
using Moimo.Cloud;
using Moimo.Core;
using Moimo.Google;
using Moimo.Modules;
using Moimo.Utility;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Moimo
{
    [CreateAssetMenu(menuName = "Moimo/Setting", fileName = "ASetting")]
    public class PackSetting : ScriptableObjectAuto
    {
        [TabGroup("Moimo","General")] public bool autoSave = true;
        [TabGroup("Moimo","General")] public string language; 
        [TabGroup("Moimo","General")] public FPSLimit fps;
        
        [TabGroup("Moimo","Google Play")] public bool isGoogle;
        [TabGroup("Moimo","Google Play"), ShowIf(nameof(isGoogle)), HideLabel] 
        public GoogleSetting google;
        
        [TabGroup("Moimo","Supabase")] public bool isSupabase;
        [TabGroup("Moimo","Supabase"), ShowIf(nameof(isSupabase)), HideLabel] 
        public SupabaseSetting supabase;

        public virtual IEnumerator FirstScene()
        {
            if (SceneManager.GetActiveScene().buildIndex == 0)
            {
                var _scenePath = SceneUtility.GetScenePathByBuildIndex(1);
                var _sceneName = Path.GetFileNameWithoutExtension(_scenePath);
                yield return SCENE.LoadCoroutine(_sceneName);
            }
        }
    }
}
