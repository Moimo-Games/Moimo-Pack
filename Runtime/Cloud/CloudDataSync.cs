#if SUPABASE

using System;
using System.Threading.Tasks;
using Moimo.Modules;
using UnityEngine;

namespace Moimo.Cloud
{
    public static class CloudDataSync
    {
        private const string ForceVersionPrefsKey = "ForceVersion";
        private const int DefaultForceVersion = 1;

        public static async Task<NetworkResult> SyncAfterAuth(bool firstLoginNoSession = false)
        {
            if (!SupabaseManager.IsEnabled)
                return new NetworkResult("Supabase is disabled.");

            if (!await NETWORK.IsConnection())
            {
                Debug.LogWarning("[CloudSync] No internet, keep local data.");
                return new NetworkResult("No internet connection.");
            }

            try
            {
                Debug.Log($"[CloudSync] Sync start. firstLoginNoSession={firstLoginNoSession}, localBlank={STORAGE.IsLocalBlank()}, loadedFromDisk={STORAGE.LoadedFromDisk}");

                var _gameResult = await SupabaseManager.GetData();

                if (!_gameResult.IsSuccess)
                {
                    if (_gameResult.Error == "Game data not found.")
                        return await UploadLocalIfReady();

                    Debug.LogWarning($"[CloudSync] Fetch failed, keep local: {_gameResult.Error}");
                    return new NetworkResult(_gameResult.Error);
                }

                if (_gameResult.Value == null || string.IsNullOrEmpty(_gameResult.Value.GameData))
                {
                    Debug.LogWarning("[CloudSync] Cloud empty, keep local.");
                    return new NetworkResult("Cloud game data is empty.");
                }

                return await MergeCloudAndLocal(_gameResult.Value.GameData, firstLoginNoSession);
            }
            catch (Exception _e)
            {
                Debug.LogWarning($"[CloudSync] Sync failed, keep local: {_e.Message}");
                return new NetworkResult(_e.Message);
            }
        }

        private static async Task<NetworkResult> MergeCloudAndLocal(string cloudJson, bool firstLoginNoSession)
        {
            if (!STORAGE.IsValidCloudJson(cloudJson))
            {
                Debug.LogWarning("[CloudSync] Invalid cloud data, keep local.");
                return new NetworkResult("Invalid cloud data.");
            }

            if (STORAGE.TryGetCloudForceVersion(cloudJson, out var _cloudForceVersion))
            {
                var _localForceVersion = GetLocalForceVersion();

                if (_cloudForceVersion > _localForceVersion)
                {
                    Debug.Log($"[CloudSync] forceVersion cloud={_cloudForceVersion} > local={_localForceVersion}, applying cloud.");

                    if (!STORAGE.ApplyForceCloudData(cloudJson))
                    {
                        Debug.LogWarning("[CloudSync] Failed forceVersion apply, keep local.");
                        return new NetworkResult("Failed to apply cloud data.");
                    }

                    SetLocalForceVersion(_cloudForceVersion);
                    SupabaseManager.ClearDirty();
                    Debug.Log("[CloudSync] forceVersion applied.");
                    return new NetworkResult();
                }
            }

            var _forceCloud = firstLoginNoSession || STORAGE.IsLocalBlank();

            if (_forceCloud)
            {
                Debug.Log($"[CloudSync] Force cloud to local. firstLoginNoSession={firstLoginNoSession}, localBlank={STORAGE.IsLocalBlank()}");

                if (STORAGE.ApplyForceCloudData(cloudJson))
                {
                    if (STORAGE.TryGetCloudForceVersion(cloudJson, out var _v))
                        SetLocalForceVersion(_v);

                    Debug.Log("[CloudSync] First login / local missing, applied cloud to local.");
                    SupabaseManager.ClearDirty();
                    return new NetworkResult();
                }

                Debug.LogWarning("[CloudSync] Failed to apply cloud, keep local.");
                return new NetworkResult("Failed to apply cloud data.");
            }

            if (!STORAGE.TryParseCloudData(cloudJson, out var _cloudLast))
            {
                Debug.LogWarning("[CloudSync] Invalid cloud data, keep local.");
                return new NetworkResult("Invalid cloud data.");
            }

            var _localLast = STORAGE.GetLocalLastSave();

            if (_cloudLast > _localLast)
            {
                if (STORAGE.ApplyCloudData(cloudJson))
                {
                    if (STORAGE.TryGetCloudForceVersion(cloudJson, out var _v))
                        SetLocalForceVersion(_v);

                    Debug.Log("[CloudSync] Cloud newer, applied to local.");
                    SupabaseManager.ClearDirty();
                    return new NetworkResult();
                }

                Debug.LogWarning("[CloudSync] Failed to apply newer cloud, keep local.");
                return new NetworkResult("Failed to apply cloud data.");
            }

            Debug.Log("[CloudSync] Local newer or equal, upload local.");
            return await UploadLocalIfReady();
        }

        private static async Task<NetworkResult> UploadLocalIfReady()
        {
            if (STORAGE.IsLocalBlank())
            {
                Debug.Log("[CloudSync] Local blank, create initial cloud backup.");
            }

            var _result = await SupabaseManager.SaveData(STORAGE.GetJson());
            if (_result.IsSuccess)
                SupabaseManager.ClearDirty();
            return _result;
        }

        private static int GetLocalForceVersion()
        {
            return PlayerPrefs.GetInt(ForceVersionPrefsKey, DefaultForceVersion);
        }

        private static void SetLocalForceVersion(int version)
        {
            if (version < DefaultForceVersion)
                version = DefaultForceVersion;

            PlayerPrefs.SetInt(ForceVersionPrefsKey, version);
            PlayerPrefs.Save();
        }
    }
}

#endif
