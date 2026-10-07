using System.IO;
using Moimo.Cloud;
using Moimo.Core;
using UnityEngine;

namespace Moimo.Modules
{
    public static class STORAGE
    {
        private const string FileName = "Save.json";
        private static string pathFile => Path.Combine(Application.persistentDataPath, FileName);
        private static GameData data = new();
        private static bool autoSave = true;

        public static T Get<T>() where T : BaseStorage, new() => data.Get<T>();
        public static string GetJson() => data.GetJson();
        public static void SetJson(string json) => data.SetJson(json);

        public static void Initialize()
        {
            Load();
            if (GAME.TryGetSO<PackSetting>(out var _so))
            {
                autoSave = _so.autoSave;
            }
            Application.quitting += TrySave;
        }

        private static void Load()
        {
            if (File.Exists(pathFile))
            {
                var _json = File.ReadAllBytes(pathFile);
                var _decrypt = Encryption.Decrypt(_json);
                data.SetJson(_decrypt);
            }
            else
            {
                Debug.LogWarning("[Storage] local game data not found");
                data.New();
            }
        }

        public static void Save()
        {
            if (data == null) return;

            data.Save();

            var _json = data.GetJson();
            var _encrypt = Encryption.Encrypt(_json);
            File.WriteAllBytes(pathFile, _encrypt);

#if UNITY_EDITOR
            var _pathEditorSave = Path.Combine(Application.persistentDataPath, pathFile.Replace(".", "Editor."));
            File.WriteAllText(_pathEditorSave, _json);
#endif

#if SUPABASE
            SupabaseManager.SaveData();
#endif
            Debug.Log($"[Storage] Saving local game data \n" +
                      $"Path : {pathFile}");
        }

        public static void TrySave()
        {
            if (autoSave)
            {
                Save();
            }
        }

        public static bool TryReplace(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;

            var _newStorages = new GameData();
            _newStorages.SetJson(json);
            return TryReplace(_newStorages);
        }

        public static bool TryReplace(GameData newData)
        {
            if (newData == null) return false;

            if (newData.Get<MetaStorage>().lastSave > data.Get<MetaStorage>().lastSave)
            {
                Debug.Log("[Storage] Successfully try replaced game data to local");
                data = newData;
                return true;
            }

            Debug.Log("[Storage] Try replace failed: new game data is older or equal to local data");
            return false;
        }

        public static void Replace(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            data.SetJson(json);
            Debug.Log("[Storage] Successfully replaced game data to local");
        }
    }
}
