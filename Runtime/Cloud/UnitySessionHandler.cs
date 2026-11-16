#if SUPABASE

using System;
using System.IO;
using Moimo.Modules;
using Newtonsoft.Json;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using UnityEngine;

namespace Moimo.Cloud
{
    public class UnitySessionHandler : IGotrueSessionPersistence<Session>
    {
        private const string FileName = "cloud.cache";
        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        private static readonly JsonSerializerSettings jsonSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        public static bool HasCachedSession()
        {
            if (!File.Exists(FilePath))
                return false;

            try
            {
                var _bytes = File.ReadAllBytes(FilePath);
                if (_bytes == null || _bytes.Length == 0)
                    return false;

                var _decrypt = Encryption.Decrypt(_bytes);
                if (string.IsNullOrEmpty(_decrypt) || _decrypt == "null")
                    return false;

                var _session = JsonConvert.DeserializeObject<Session>(_decrypt, jsonSettings);
                return _session != null && !string.IsNullOrEmpty(_session.AccessToken);
            }
            catch
            {
                return false;
            }
        }

        public static void ClearCachedSession()
        {
            try
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
            catch
            {
            }
        }

        public void SaveSession(Session session)
        {
            if (session == null)
            {
                DestroySession();
                return;
            }

            if (Application.internetReachability == NetworkReachability.NotReachable)
                return;

            try
            {
                var _json = JsonConvert.SerializeObject(session, jsonSettings);
                if (string.IsNullOrEmpty(_json) || _json == "null")
                {
                    DestroySession();
                    return;
                }

                var _encrypt = Encryption.Encrypt(_json);
                File.WriteAllBytes(FilePath, _encrypt);
            }
            catch (Exception _e)
            {
                Debug.LogWarning($"[Supabase] Failed to save session: {_e.Message}");
            }
        }

        public Session LoadSession()
        {
            if (!File.Exists(FilePath))
                return null;

            try
            {
                var _bytes = File.ReadAllBytes(FilePath);
                if (_bytes == null || _bytes.Length == 0)
                {
                    DestroySession();
                    return null;
                }

                var _decrypt = Encryption.Decrypt(_bytes);
                if (string.IsNullOrEmpty(_decrypt) || _decrypt == "null")
                {
                    DestroySession();
                    return null;
                }

                var _session = JsonConvert.DeserializeObject<Session>(_decrypt, jsonSettings);
                if (_session == null || string.IsNullOrEmpty(_session.AccessToken))
                {
                    DestroySession();
                    return null;
                }

                return _session;
            }
            catch (Exception _e)
            {
                Debug.LogWarning($"[Supabase] Failed to load session: {_e.Message}");
                DestroySession();
                return null;
            }
        }

        public void DestroySession()
        {
            ClearCachedSession();
        }
    }
}

#endif
