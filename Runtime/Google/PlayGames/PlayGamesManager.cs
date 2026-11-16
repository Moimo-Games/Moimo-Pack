#if GOOGLE_MOBILE

using System;
using System.Collections;
using System.Collections.Generic;
using GooglePlayGames;
using Moimo.Core;
using Moimo.Utility;
using UnityEngine;


namespace Moimo.Google
{
    public class PlayGamesManager : GlobalBehaviour
    { 
        private bool hasInitialize;
        public event Action OnInitialize;
        private Dictionary<Type, PlayGamesBase> instances = new();

        [Obsolete("Obsolete")]
        public override IEnumerator InitializeCoroutine()
        {
            if (!SO.Get<PackSetting>().isGoogle) yield break;
            PlayGamesPlatform.Activate();
            
            var _isCompleted = false;
            Social.localUser.Authenticate(success =>
            {
                if (success)
                {
                    hasInitialize = true;
                    instances = InstanceUtility.Create<PlayGamesBase>();
                    foreach (var _google in instances.Values)
                    {
                        _google.Initialize();
                    }
                    OnInitialize?.Invoke();
                    Debug.Log("[Google Play Games] Authentication successfully");
                }
                else
                {
                    Debug.Log("[Google Play Games] Authentication failed");
                }

                _isCompleted = true;
            });
            
            yield return new WaitUntil(() => _isCompleted);
        }
        
        public bool IsActive<T>() where T : PlayGamesBase
        {
            return hasInitialize;
        }
        
        public T Get<T>() where T : PlayGamesBase
        {
            if (instances.TryGetValue(typeof(T), out var _instance))
            {
                return (T)_instance;
            }

            return null;
        }
        
        public bool TryGet<T>(out T instance) where T : PlayGamesBase
        {
            if (hasInitialize)
            {
                var _instance = Get<T>();
                if (_instance != null)
                {
                    instance = _instance;
                    return true;
                }
            }

            instance = null;
            return false;
        }
    }
}

#endif