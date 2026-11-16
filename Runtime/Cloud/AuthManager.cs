#if SUPABASE

using System;
using System.Threading.Tasks;
using UnityEngine;
using Google;
using Moimo.Core;
using Moimo.Modules;

namespace Moimo.Cloud
{
    public class AuthManager : GlobalBehaviour
    {
#if UNITY_EDITOR
        private const string editorEmail = "test@gmail.com";
        private const string editorPassword = "test123";
#endif

        private bool isAuthenticating;
        private static bool googleConfigured;

        public bool IsAuthenticating => isAuthenticating;
        public bool IsLoggedIn =>
            SupabaseManager.Client?.Auth?.CurrentUser != null;

        public override async Task PostInitializeAsync()
        {
            try
            {
                await Setup();
            }
            catch (Exception _e)
            {
                Debug.LogWarning($"[Auth] Initialization failed: {_e.Message}");
            }
        }

        private async Task Setup()
        {
            if (!SupabaseManager.IsEnabled)
            {
                Debug.Log("[Auth] Disabled (isSupabase = false).");
                return;
            }

            Debug.Log("[Auth] Initializing...");

            var _waitStart = DateTime.UtcNow;
            while (SupabaseManager.Client == null)
            {
                if ((DateTime.UtcNow - _waitStart).TotalSeconds > 5)
                {
                    Debug.LogWarning("[Auth] Supabase client initialization timed out.");
                    return;
                }
                await Task.Delay(100);
            }

            if (!IsAutoLoginEnabled())
            {
                Debug.Log("[Auth] Auto login disabled.");
                return;
            }

            await TryAutoLogin();
        }

        private static bool IsAutoLoginEnabled()
        {
            var _pack = SO.Get<PackSetting>();
            return _pack != null && _pack.autoLogin;
        }

        public async Task<NetworkResult> TryAutoLogin()
        {
            if (!SupabaseManager.IsEnabled)
                return new NetworkResult("Supabase is disabled.");

            if (SupabaseManager.Client == null)
                return new NetworkResult("Supabase client is not initialized.");

            if (!await NETWORK.IsConnection())
            {
                Debug.LogWarning("[Auth] No internet connection, keep local data.");
                return new NetworkResult("No internet connection.");
            }

            Debug.Log("[Auth] Auto login started.");

#if UNITY_EDITOR
            var _result = await LoginEditorAccount();
#else
            var _result = await RestoreSession();
            if (_result.IsSuccess)
            {
                Debug.Log("[Auth] Auto login completed (session restored).");
                return _result;
            }

            Debug.Log($"[Auth] Session restore failed: {_result.Error}");

            _result = await LoginGoogleSilent();
            if (_result.IsSuccess)
            {
                Debug.Log("[Auth] Auto login completed (Google silent).");
                return _result;
            }

            Debug.Log($"[Auth] Google silent failed: {_result.Error}");

            _result = await LoginGoogle();
#endif
            if (_result.IsSuccess)
                Debug.Log("[Auth] Auto login completed.");
            else
                Debug.LogWarning($"[Auth] Auto login failed: {_result.Error}");

            return _result;
        }

        public async Task<NetworkResult> LoginGoogle()
        {
            if (!SupabaseManager.IsEnabled)
                return new NetworkResult("Supabase is disabled.");

            if (SupabaseManager.Client == null)
                return new NetworkResult("Supabase client is not initialized.");

            if (!await NETWORK.IsConnection())
                return new NetworkResult("No internet connection.");

            if (isAuthenticating)
                return new NetworkResult("Authentication is already in progress.");

            isAuthenticating = true;

            try
            {
                var _configurationResult = ConfigureGoogleSignIn();
                if (!_configurationResult.IsSuccess)
                    return _configurationResult;

                GoogleSignInUser _googleUser;
                try
                {
                    _googleUser = await GoogleSignIn.DefaultInstance.SignIn();
                }
                catch (Exception _e)
                {
                    HandleAuthException(_e);
                    return new NetworkResult(_e.Message);
                }

                if (_googleUser == null)
                    return new NetworkResult("Google authentication returned a null user.");

                if (string.IsNullOrEmpty(_googleUser.IdToken))
                    return new NetworkResult("Google authentication returned an empty ID token.");

                Debug.Log($"[Auth] Google sign-in succeeded: {_googleUser.Email}");
                return await LoginWithGoogleIdToken(_googleUser.IdToken);
            }
            finally
            {
                isAuthenticating = false;
            }
        }

        public async Task<NetworkResult> LoginGoogleSilent()
        {
            if (!SupabaseManager.IsEnabled)
                return new NetworkResult("Supabase is disabled.");

            if (SupabaseManager.Client == null)
                return new NetworkResult("Supabase client is not initialized.");

            if (!await NETWORK.IsConnection())
                return new NetworkResult("No internet connection.");

            if (isAuthenticating)
                return new NetworkResult("Authentication is already in progress.");

            isAuthenticating = true;

            try
            {
                var _configurationResult = ConfigureGoogleSignIn();
                if (!_configurationResult.IsSuccess)
                    return _configurationResult;

                GoogleSignInUser _googleUser;
                try
                {
                    _googleUser = await GoogleSignIn.DefaultInstance.SignInSilently();
                }
                catch (Exception _e)
                {
                    return new NetworkResult($"Google silent sign-in failed: {_e.Message}");
                }

                if (_googleUser == null)
                    return new NetworkResult("Google silent sign-in returned null user.");

                if (string.IsNullOrEmpty(_googleUser.IdToken))
                    return new NetworkResult("Google silent sign-in returned an empty ID token.");

                Debug.Log($"[Auth] Google silent sign-in succeeded: {_googleUser.Email}");
                return await LoginWithGoogleIdToken(_googleUser.IdToken);
            }
            finally
            {
                isAuthenticating = false;
            }
        }

        public async Task<NetworkResult> Logout()
        {
            if (!SupabaseManager.IsEnabled)
                return new NetworkResult("Supabase is disabled.");

            if (SupabaseManager.Client == null)
                return new NetworkResult("Supabase client is not initialized.");

            try
            {
                if (await NETWORK.IsConnection())
                    await SupabaseManager.Client.Auth.SignOut();

                if (googleConfigured)
                {
                    try
                    {
                        GoogleSignIn.DefaultInstance.SignOut();
                    }
                    catch (Exception)
                    {
                    }
                }

                UnitySessionHandler.ClearCachedSession();
                Debug.Log("[Auth] Logged out.");
                return new NetworkResult();
            }
            catch (Exception _e)
            {
                UnitySessionHandler.ClearCachedSession();
                Debug.LogError($"[Auth] Sign out failed: {_e.Message}");
                return new NetworkResult(_e.Message);
            }
        }

        public AuthProfile GetProfile()
        {
            var _user = SupabaseManager.Client?.Auth?.CurrentUser;
            return new AuthProfile
            {
                UserId = _user?.Id,
                Email = _user?.Email,
                IsLoggedIn = _user != null,
                HasCachedSession = UnitySessionHandler.HasCachedSession()
            };
        }

        public async Task<NetworkResult> SyncGameData()
        {
            return await CloudDataSync.SyncAfterAuth();
        }

        private async Task<NetworkResult> RestoreSession()
        {
            try
            {
                if (SupabaseManager.Client == null)
                    return new NetworkResult("Supabase client is not initialized.");

                if (!UnitySessionHandler.HasCachedSession())
                    return new NetworkResult("No cached session.");

                var _session = SupabaseManager.Client.Auth.CurrentSession;
                var _user = SupabaseManager.Client.Auth.CurrentUser;

                if (_session == null || _user == null)
                    return new NetworkResult("No active session found.");

                Debug.Log($"[Auth] Session restored: {_user.Id}");
                return await CloudDataSync.SyncAfterAuth();
            }
            catch (Exception _e)
            {
                HandleAuthException(_e);
                return new NetworkResult(_e.Message);
            }
        }

        private async Task<NetworkResult> LoginWithGoogleIdToken(string idToken)
        {
            if (string.IsNullOrEmpty(idToken))
                return new NetworkResult("Google ID token is empty.");

            try
            {
                await SupabaseManager.Client.Auth.SignInWithIdToken(
                    Supabase.Gotrue.Constants.Provider.Google,
                    idToken
                );

                var _user = SupabaseManager.Client.Auth.CurrentUser;
                if (_user == null)
                    return new NetworkResult("Google authentication succeeded but Supabase user is null.");

                Debug.Log($"[Auth] Supabase Google login succeeded: {_user.Id}");
                return await CloudDataSync.SyncAfterAuth();
            }
            catch (Exception _e)
            {
                HandleAuthException(_e);
                UnitySessionHandler.ClearCachedSession();
                Debug.LogWarning($"[Auth] Google auth failed, keep local data: {_e.Message}");
                return new NetworkResult(_e.Message);
            }
        }

#if UNITY_EDITOR
        private async Task<NetworkResult> LoginEditorAccount()
        {
            if (isAuthenticating)
                return new NetworkResult("Authentication is already in progress.");

            isAuthenticating = true;

            try
            {
                var _user = SupabaseManager.Client.Auth.CurrentUser;

                if (_user == null)
                {
                    await SupabaseManager.Client.Auth.SignIn(editorEmail, editorPassword);
                    _user = SupabaseManager.Client.Auth.CurrentUser;
                }

                if (_user == null)
                    return new NetworkResult("Editor authentication succeeded but user is null.");

                return await CloudDataSync.SyncAfterAuth();
            }
            catch (Exception _e)
            {
                HandleAuthException(_e);
                return new NetworkResult(_e.Message);
            }
            finally
            {
                isAuthenticating = false;
            }
        }
#endif

        private NetworkResult ConfigureGoogleSignIn()
        {
            if (googleConfigured)
                return new NetworkResult();

            var _settings = SO.Get<PackSetting>();
            if (_settings == null)
                return new NetworkResult("ASettingData is null.");

            if (_settings.supabase == null)
                return new NetworkResult("Supabase settings are null.");

            var _webClientId = _settings.supabase.webClientID;
            if (string.IsNullOrWhiteSpace(_webClientId))
                return new NetworkResult("Google Web Client ID is empty.");

            try
            {
                GoogleSignIn.Configuration = new GoogleSignInConfiguration
                {
                    WebClientId = _webClientId,
                    RequestIdToken = true,
                    RequestEmail = true
                };

                googleConfigured = true;
                return new NetworkResult();
            }
            catch (Exception _e)
            {
                HandleAuthException(_e);
                return new NetworkResult(_e.Message);
            }
        }

        private void HandleAuthException(Exception e)
        {
            if (e == null)
            {
                Debug.LogError("[Auth] Unknown authentication error.");
                return;
            }

            var _message = e.Message;

            if (_message.Contains("over_email_send_rate_limit"))
            {
                Debug.LogError("[Auth] Supabase email rate limit exceeded.");
                return;
            }

            if (_message.Contains("Invalid API key"))
            {
                Debug.LogError("[Auth] Supabase API key is invalid.");
                return;
            }

            if (_message.Contains("invalid_credentials"))
            {
                Debug.LogError("[Auth] Editor authentication failed: invalid credentials.");
                return;
            }

            if (_message.Contains("DefaultInstance already created"))
            {
                Debug.LogError("[Auth] Google Sign-In DefaultInstance was already created.");
                return;
            }

            if (_message.Contains("Deserialization") || _message.Contains("Session"))
            {
                Debug.LogWarning($"[Auth] Session error, keep local data: {_message}");
                UnitySessionHandler.ClearCachedSession();
                return;
            }

            Debug.LogError($"[Auth] Authentication error: {_message}");
        }
    }
}

#endif
