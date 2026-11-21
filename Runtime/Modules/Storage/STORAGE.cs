using System;
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
        private static string pathTemp => Path.Combine(Application.persistentDataPath, "Save.tmp");
        private static string pathBackup => Path.Combine(Application.persistentDataPath, "Save.bak");
        private static GameData data = new();
        private static bool autoSave = true;
        private static readonly object saveLock = new();
        private static bool loadedFromDisk;

        public static T Get<T>() where T : BaseStorage, new() => data.Get<T>();
        public static string GetJson() => data.GetJson();
        public static void SetJson(string json) => data.SetJson(json);
        public static bool LoadedFromDisk => loadedFromDisk;

        public static void Initialize()
        {
            Debug.LogWarning("[Storage] Initialize");
            Load();
            if (SO.TryGet<PackSetting>(out var _so))
            {
                autoSave = _so.autoSave;
            }
            Application.quitting += TrySaveLocal;
        }

        private static void Load()
        {
            loadedFromDisk = false;

            if (TryLoadFromFile(pathFile))
            {
                loadedFromDisk = true;
                return;
            }

            if (TryLoadFromFile(pathBackup))
            {
                loadedFromDisk = true;
                Debug.LogWarning("[Storage] Loaded game data from backup");
                PersistLocalInternal();
                return;
            }

            Debug.LogWarning("[Storage] local game data not found");
            data.New();
        }

        private static bool TryLoadFromFile(string path)
        {
            if (!File.Exists(path))
                return false;

            try
            {
                var _bytes = File.ReadAllBytes(path);
                if (_bytes == null || _bytes.Length == 0)
                    return false;

                var _decrypt = Encryption.Decrypt(_bytes);
                if (string.IsNullOrEmpty(_decrypt))
                    return false;

                data.SetJson(_decrypt);
                Debug.Log("[Storage] Local game data loaded");
                return true;
            }
            catch (Exception _e)
            {
                Debug.LogWarning($"[Storage] Failed to load from {path}: {_e.Message}");
                return false;
            }
        }

        public static void Save()
        {
            if (data == null) return;

            SaveLocal();
            SaveCloud();
        }

        public static void SaveLocal()
        {
            if (data == null) return;

            lock (saveLock)
            {
                data.Save();
                PersistLocalInternal();
            }

#if SUPABASE
            SupabaseManager.MarkDirty();
#endif

            Debug.Log($"[Storage] Saving local game data\nPath : {pathFile}");
        }

        public static void SaveCloud()
        {
#if SUPABASE
            SupabaseManager.SaveCloud();
#endif
        }

        public static void SaveCloudIfDirty()
        {
#if SUPABASE
            SupabaseManager.SaveCloudIfDirty();
#endif
        }

        public static void PersistLocal()
        {
            if (data == null) return;
            lock (saveLock)
            {
                PersistLocalInternal();
            }
        }

        private static void PersistLocalInternal()
        {
            try
            {
                var _json = data.GetJson();
                var _encrypt = Encryption.Encrypt(_json);

                File.WriteAllBytes(pathTemp, _encrypt);

                if (File.Exists(pathFile))
                {
                    File.Replace(pathTemp, pathFile, pathBackup);
                }
                else
                {
                    File.Move(pathTemp, pathFile);
                }

#if UNITY_EDITOR
                var _pathEditorSave = Path.Combine(Application.persistentDataPath, pathFile.Replace(".", "Editor."));
                File.WriteAllText(_pathEditorSave, _json);
#endif
            }
            catch (Exception _e)
            {
                Debug.LogError($"[Storage] Failed to persist local game data: {_e.Message}");
                try
                {
                    if (File.Exists(pathTemp))
                        File.Delete(pathTemp);
                }
                catch
                {
                }
            }
        }

        public static void TrySaveLocal()
        {
            if (autoSave)
            {
                SaveLocal();
            }
        }

        public static DateTime GetLocalLastSave()
        {
            if (data == null)
                return default;

            return data.Get<MetaStorage>().lastSave;
        }

        public static bool IsLocalBlank()
        {
            if (data == null)
                return true;

            if (loadedFromDisk)
                return false;

            return data.Get<MetaStorage>().lastSave == default;
        }

        public static bool IsValidCloudJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return false;

            var _trim = json.TrimStart();
            return _trim.Length > 0 && (_trim[0] == '{' || _trim[0] == '[');
        }

        public static bool TryGetCloudForceVersion(string json, out int forceVersion)
        {
            forceVersion = 1;

            if (!IsValidCloudJson(json))
                return false;

            try
            {
                var _parsed = new GameData();
                _parsed.SetJson(json);
                forceVersion = _parsed.Get<MetaStorage>().forceVersion;
                if (forceVersion < 1)
                    forceVersion = 1;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryParseCloudData(string json, out DateTime cloudLastSave)
        {
            cloudLastSave = default;

            if (!IsValidCloudJson(json))
                return false;

            try
            {
                var _parsed = new GameData();
                _parsed.SetJson(json);
                cloudLastSave = _parsed.Get<MetaStorage>().lastSave;
                return true;
            }
            catch (Exception _e)
            {
                Debug.LogWarning($"[Storage] Failed to parse cloud data: {_e.Message}");
                return false;
            }
        }

        public static bool ApplyCloudData(string json)
        {
            if (!IsValidCloudJson(json))
                return false;

            try
            {
                var _parsed = new GameData();
                _parsed.SetJson(json);

                if (_parsed.Get<MetaStorage>().lastSave == default)
                    return false;

                return ApplyParsedCloudData(_parsed);
            }
            catch (Exception _e)
            {
                Debug.LogWarning($"[Storage] Apply cloud failed, keep local: {_e.Message}");
                return false;
            }
        }

        public static bool ApplyForceCloudData(string json)
        {
            if (!IsValidCloudJson(json))
                return false;

            try
            {
                var _parsed = new GameData();
                _parsed.SetJson(json);
                return ApplyParsedCloudData(_parsed);
            }
            catch (Exception _e)
            {
                Debug.LogWarning($"[Storage] Force apply cloud failed, keep local: {_e.Message}");
                return false;
            }
        }

        private static bool ApplyParsedCloudData(GameData parsed)
        {
            if (parsed == null)
                return false;

            lock (saveLock)
            {
                data = parsed;
                PersistLocalInternal();
            }

            loadedFromDisk = true;
            return true;
        }

        public static bool TryReplace(string json)
        {
            if (!TryParseCloudData(json, out var _cloudLast))
            {
                Debug.LogWarning("[Storage] Cloud json invalid, keep local.");
                return false;
            }

            var _localLast = GetLocalLastSave();

            if (_cloudLast > _localLast)
                return ApplyCloudData(json);

            Debug.Log($"[Storage] Keep local data (cloud={_cloudLast:O}, local={_localLast:O}).");
            return false;
        }

        public static void Replace(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            lock (saveLock)
            {
                data.SetJson(json);
                PersistLocalInternal();
            }

            loadedFromDisk = true;

#if SUPABASE
            SupabaseManager.MarkDirty();
#endif
            Debug.Log("[Storage] Successfully force-replaced game data to local");
        }
    }
}
