#if GOOGLE_MOBILE

using System.Collections;
using Google.Play.AppUpdate;
using Google.Play.Common;
using Moimo.Core;
using UnityEngine;

namespace Moimo.Google
{
    public class ForceUpdateManager : GlobalBehaviour
    {
        public override IEnumerator InitializeCoroutine()
        {
            var _setting = SO.Get<PackSetting>();
            if (_setting == null || !_setting.isGoogle) yield break;

#if UNITY_ANDROID && !UNITY_EDITOR
            var _appUpdateManager = new AppUpdateManager();
            var _appUpdateInfoOperation = _appUpdateManager.GetAppUpdateInfo();
            yield return _appUpdateInfoOperation;

            if (_appUpdateInfoOperation.Error != AppUpdateErrorCode.NoError)
            {
                Debug.LogWarning($"[AppUpdate] GetAppUpdateInfo error: {_appUpdateInfoOperation.Error}");
                yield break;
            }

            var _appUpdateInfo = _appUpdateInfoOperation.GetResult();
            if (_appUpdateInfo.UpdateAvailability != UpdateAvailability.UpdateAvailable)
            {
                yield break;
            }

            var _options = AppUpdateOptions.ImmediateAppUpdateOptions();
            if (!_appUpdateInfo.IsUpdateTypeAllowed(_options))
            {
                yield break;
            }

            var _startUpdateRequest = _appUpdateManager.StartUpdate(_appUpdateInfo, _options);
            yield return _startUpdateRequest;

            if (_startUpdateRequest.Error != AppUpdateErrorCode.NoError)
            {
                Debug.LogWarning($"[AppUpdate] StartUpdate error: {_startUpdateRequest.Error}");
            }
#endif
            yield break;
        }
    }
}

#endif
