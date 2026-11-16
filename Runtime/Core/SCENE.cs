using System.Collections;
using System;
using Moimo.Modules;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Moimo.Core
{
    public static class SCENE
    {
        public static string Current { get; private set; }
        public static event Action OnLoaded;
        public static event Action OnUnloaded;

        public static void Initialize()
        {
            Current = SceneManager.GetActiveScene().name;
            SceneManager.sceneLoaded += (_, _) => OnLoaded?.Invoke();
            SceneManager.sceneUnloaded += _ => OnUnloaded?.Invoke();
        }

        public static void Restart(LoadSceneSetting setting = null)
        {
            Load(Current, setting);
        }
        
        public static void RestartWithFade()
        {
            var _setting = new LoadSceneSetting();
            LoadWithFade(Current, _setting);
        }
        
        public static void Restart(Action onComplete)
        {
            var _setting = new LoadSceneSetting { onComplete = onComplete };
            Load(Current, _setting);
        }
        
        public static IEnumerator RestartCoroutine(LoadSceneSetting setting = null)
        {
            var _setting = new LoadSceneSetting();
            yield return LoadCoroutine(Current, _setting);
        }

        public static void Load(string sceneName)
        {
            var _setting = new LoadSceneSetting();
            Load(sceneName, _setting);
        }
        public static void Load(string sceneName, Action onComplete)
        {
            var _setting = new LoadSceneSetting { onComplete = onComplete };
            Load(sceneName, _setting);
        }
        
        public static void Load(string sceneName, LoadSceneSetting setting)
        {
            GAME.Manager.StartCoroutine(LoadCoroutine(sceneName, setting));
        }
        
        public static void LoadWithFade(string sceneName)
        {
            var _setting = new LoadSceneSetting();
            LoadWithFade(sceneName, _setting);
        }
        public static void LoadWithFade(string sceneName, Action onComplete)
        {
            var _setting = new LoadSceneSetting { onComplete = onComplete };
            LoadWithFade(sceneName, _setting);
        }

        public static void LoadWithFade(string sceneName, LoadSceneSetting setting)
        {
            GAME.Manager.StartCoroutine(LoadWithFadeCoroutine(sceneName, setting));
        }
        public static void LoadWithFade(string sceneName, BlackscreenType enter, BlackscreenType exit)
        {
            var _setting = new LoadSceneSetting();
            _setting.enter = enter;
            _setting.exit = exit;
            LoadWithFade(sceneName, _setting);
        }

        public static IEnumerator LoadWithFadeCoroutine(string sceneName, LoadSceneSetting setting)
        {
            if (setting == null) setting = new LoadSceneSetting();

            BlackscreenPopup _popup = null;
            if (setting.enter != BlackscreenType.None || setting.exit != BlackscreenType.None)
            {
                _popup = OBJECT.Show<BlackscreenPopup>();
            }

            if (setting.enter != BlackscreenType.None)
            {
                yield return _popup.InCoroutine(setting.enter == BlackscreenType.Instant);
            }
            
            yield return LoadCoroutine(sceneName, setting);
            
            if (setting.exit != BlackscreenType.None)
            {
                yield return _popup.OutCoroutine(setting.exit == BlackscreenType.Instant);
            }
            
            setting.onComplete?.Invoke();
        }

        public static IEnumerator LoadWithFadeCoroutine(string sceneName, BlackscreenType enter, BlackscreenType exit)
        {
            var _setting = new LoadSceneSetting();
            _setting.enter = enter;
            _setting.exit = exit;
            yield return LoadWithFadeCoroutine(sceneName, _setting);
        }
        
        public static IEnumerator LoadCoroutine(string sceneName)
        {
            var _setting = new LoadSceneSetting();
            yield return LoadCoroutine(sceneName, _setting);
        }
        public static IEnumerator LoadCoroutine(string sceneName, Action onComplete)
        {
            var _setting = new LoadSceneSetting { onComplete = onComplete };
            yield return LoadCoroutine(sceneName, _setting);
        }
        
        public static IEnumerator LoadCoroutine(string sceneName, LoadSceneSetting setting)
        {
            if (setting == null) setting = new LoadSceneSetting();
            
            OBJECT.RemoveOnLoaded(setting.removeAllPopup);
            var _async = SceneManager.LoadSceneAsync(sceneName);

            Current = sceneName;
            while (!_async.isDone)
            {
                setting.onProgress?.Invoke(Mathf.Clamp01(_async.progress / 0.9f));
                yield return null;
            }

            var _isCompleted = false;
            _async.completed += _ => _isCompleted = true;
            yield return new WaitUntil(() => _isCompleted);
            
            setting.onComplete?.Invoke();
        }
    }
}