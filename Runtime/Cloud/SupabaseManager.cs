#if SUPABASE

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moimo.Core;
using Moimo.Modules;
using Moimo.Utility;
using UnityEngine;
using Supabase;

namespace Moimo.Cloud
{
    public class SupabaseManager : GlobalBehaviour
    {
        public static Client Client { get; private set; }

        public static bool IsEnabled
        {
            get
            {
                var _pack = SO.Get<PackSetting>();
                return _pack != null && _pack.isSupabase;
            }
        }

        private static bool cloudDirty;
        private static CancellationTokenSource cloudCts;
        private static int cloudSaveVersion;
        private static readonly SemaphoreSlim cloudUpsertGate = new(1, 1);

        public static void MarkDirty()
        {
            if (!IsEnabled)
                return;

            cloudDirty = true;
        }

        public static void ClearDirty()
        {
            cloudDirty = false;
        }

        public static void SaveCloud()
        {
            if (!IsEnabled)
                return;

            var _json = STORAGE.GetJson();
            var _version = Interlocked.Increment(ref cloudSaveVersion);

            var _cts = new CancellationTokenSource();
            var _old = Interlocked.Exchange(ref cloudCts, _cts);
            try
            {
                _old?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                _old?.Dispose();
            }

            _ = SaveCloudSafe(_version, _json, _cts.Token);
        }

        public static void SaveCloudIfDirty()
        {
            if (!cloudDirty)
                return;

            SaveCloud();
        }

        private static bool IsCurrent(int version, CancellationToken token)
        {
            return !token.IsCancellationRequested && version == Volatile.Read(ref cloudSaveVersion);
        }

        private static async Task SaveCloudSafe(int version, string gameData, CancellationToken token)
        {
            try
            {
                if (!IsCurrent(version, token))
                    return;

                if (Client == null)
                {
                    if (IsCurrent(version, token))
                        Debug.LogWarning("[Supabase] Cloud save skipped: client is not initialized.");
                    return;
                }

                if (!await NETWORK.IsConnection())
                {
                    if (IsCurrent(version, token))
                        Debug.LogWarning("[Supabase] Cloud save skipped: no internet connection.");
                    return;
                }

                if (!IsCurrent(version, token))
                    return;

                var _user = Client.Auth.CurrentUser;
                if (_user == null)
                {
                    if (IsCurrent(version, token))
                        Debug.LogWarning("[Supabase] Cloud save skipped: user is not authenticated.");
                    return;
                }

                await cloudUpsertGate.WaitAsync(token);

                try
                {
                    if (!IsCurrent(version, token))
                        return;

                    Debug.Log("[Supabase] Saving game data...");

                    await Client
                        .From<GameDatabase>()
                        .Upsert(new GameDatabase
                        {
                            Id = _user.Id,
                            GameData = gameData
                        });

                    if (!IsCurrent(version, token))
                        return;

                    cloudDirty = false;
                    Debug.Log("[Supabase] Game data saved successfully.");
                }
                finally
                {
                    cloudUpsertGate.Release();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception _e)
            {
                if (IsCurrent(version, token))
                    Debug.LogWarning($"[Supabase] Cloud save skipped: {_e.Message}");
            }
        }

        public override async Task InitializeAsync()
        {
            if (!IsEnabled)
            {
                Client = null;
                Debug.Log("[Supabase] Disabled (isSupabase = false).");
                return;
            }

            try
            {
                var _result = await Setup().WithTimeout(5);
                if (!_result.IsSuccess)
                    Debug.LogError($"[Supabase] Setup failed: {_result.Error}");
            }
            catch (Exception _e)
            {
                Client = null;
                Debug.LogError($"[Supabase] Initialization timed out or failed: {_e.Message}");
            }
        }

        private async Task<NetworkResult> Setup()
        {
            try
            {
                Debug.Log("[Supabase] Initializing...");

                var _pack = SO.Get<PackSetting>();
                if (_pack == null || !_pack.isSupabase)
                    return new NetworkResult("Supabase is disabled.");

                var _setting = _pack.supabase;

                if (_setting == null)
                    return new NetworkResult("Supabase settings are null.");

                Client = new Client(
                    _setting.url,
                    _setting.key,
                    new SupabaseOptions
                    {
                        AutoRefreshToken = true,
                        AutoConnectRealtime = false,
                        SessionHandler = new UnitySessionHandler()
                    }
                );

                await Client.InitializeAsync();

                var _session = Client.Auth.CurrentSession;
                var _user = Client.Auth.CurrentUser;

                if (_session != null && _user != null)
                {
                    Debug.Log($"[Supabase] Existing session found. User: {_user.Id}");
                }
                else
                {
                    Debug.Log("[Supabase] No existing session found.");
                }

                Debug.Log("[Supabase] Initialization completed.");

                return new NetworkResult();
            }
            catch (Exception _e)
            {
                Client = null;

                Debug.LogError($"[Supabase] Initialization failed: {_e}");

                return new NetworkResult($"Supabase initialization failed: {_e.Message}");
            }
        }

        public static async Task<NetworkResult<GameDatabase>> GetData()
        {
            Debug.Log("[Supabase] get game data...");

            if (!IsEnabled)
                return new NetworkResult<GameDatabase>("Supabase is disabled.");

            if (Client == null)
                return new NetworkResult<GameDatabase>("Supabase client is not initialized.");

            if (!await NETWORK.IsConnection())
                return new NetworkResult<GameDatabase>("No internet connection.");

            try
            {
                var _user = Client.Auth.CurrentUser;

                if (_user == null)
                    return new NetworkResult<GameDatabase>("User is not authenticated.");

                var _response =
                    await Client
                        .From<GameDatabase>()
                        .Where(x => x.Id == _user.Id)
                        .Get();

                if (_response.Models == null ||
                    _response.Models.Count == 0)
                {
                    return new NetworkResult<GameDatabase>("Game data not found.");
                }

                var _model = _response.Models[0];
                if (_model == null || string.IsNullOrEmpty(_model.GameData))
                {
                    return new NetworkResult<GameDatabase>("Game data not found.");
                }

                Debug.Log("[Supabase] Game data get successfully.");

                return new NetworkResult<GameDatabase>(_model);
            }
            catch (Exception _e)
            {
                Debug.LogError($"[Supabase] Failed to fetch game data: {_e}");

                return new NetworkResult<GameDatabase>(
                    _e.Message
                );
            }
        }

        public static async Task<NetworkResult> SaveData()
        {
            return await SaveData(STORAGE.GetJson());
        }

        public static async Task<NetworkResult> SaveData(
            string gameData)
        {
            Debug.Log("[Supabase] Saving game data...");

            if (!IsEnabled)
                return new NetworkResult("Supabase is disabled.");

            if (Client == null)
                return new NetworkResult("Supabase client is not initialized.");

            if (!await NETWORK.IsConnection())
                return new NetworkResult("No internet connection.");

            try
            {
                var _user = Client.Auth.CurrentUser;

                if (_user == null)
                    return new NetworkResult("User is not authenticated.");

                await Client
                    .From<GameDatabase>()
                    .Upsert(new GameDatabase
                    {
                        Id = _user.Id,
                        GameData = gameData
                    });

                Debug.Log("[Supabase] Game data saved successfully.");

                return new NetworkResult();
            }
            catch (Exception _e)
            {
                Debug.LogError($"[Supabase] Failed to save game data: {_e}");

                return new NetworkResult(
                    _e.Message
                );
            }
        }

        public static async Task<NetworkResult<DateTime>> GetTime()
        {
            Debug.Log("[Supabase] Fetching server time...");

            if (!IsEnabled)
                return new NetworkResult<DateTime>("Supabase is disabled.");

            if (Client == null)
                return new NetworkResult<DateTime>("Supabase client is not initialized.");

            if (!await NETWORK.IsConnection())
                return new NetworkResult<DateTime>("No internet connection.");

            try
            {
                var _response =
                    await Client
                        .Rpc(
                            "get_server_time",
                            new Dictionary<string, object>()
                        )
                        .WithTimeout(5f);

                var _serverTime =
                    DateTime.Parse(_response.Content)
                        .ToUniversalTime();

                Debug.Log($"[Supabase] Server time received: {_serverTime:O}");

                return new NetworkResult<DateTime>(_serverTime);
            }
            catch (Exception _e)
            {
                Debug.LogError($"[Supabase] Failed to fetch server time: {_e}");

                return new NetworkResult<DateTime>($"Failed to fetch server time: {_e.Message}");
            }
        }
    }
}

#endif