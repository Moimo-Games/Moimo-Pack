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

        public void SaveSession(Session session)
        {
            if (session == null)
            {
                DestroySession();
                return;
            }

            try
            {
                var _json = JsonConvert.SerializeObject(session);
                var _encrypt = Encryption.Encrypt(_json);
                File.WriteAllBytes(FilePath, _encrypt);
            }
            catch (Exception _e)
            {
                Debug.LogError($"[Supabase] Failed to save session: {_e}");
                throw;
            }
        }

        public Session LoadSession()
        {
            if (!File.Exists(FilePath)) return null;

            try
            {
                var _json = File.ReadAllBytes(FilePath);
                var _decrypt = Encryption.Decrypt(_json);
                return JsonConvert.DeserializeObject<Session>(_decrypt);
            }
            catch (Exception _e)
            {
                Debug.LogError($"[Supabase] Failed to load session: {_e}");
                return null;
            }
        }

        public void DestroySession()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }
    }
}

#endif