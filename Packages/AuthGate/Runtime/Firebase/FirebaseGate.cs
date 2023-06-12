using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using UnityEngine;

namespace AuthGate.Firebase
{
    public class FirebaseGate : IGate
    {
        private const string KeyLatestProviderId = "AUTHGATE_LATEST_PROVIDER_ID";

        private readonly FirebaseAuth _auth;
        private readonly FirebaseConfig _config;

        private string LatestProviderId
        {
            get => PlayerPrefs.GetString(KeyLatestProviderId);
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    PlayerPrefs.DeleteKey(KeyLatestProviderId);
                }
                else
                {
                    PlayerPrefs.SetString(KeyLatestProviderId, value);
                }
            }
        }

        public FirebaseGate(FirebaseAuth auth, FirebaseConfig config)
        {
            _auth = auth;
            _config = config;
        }

        public UserInfo CurrentUser => _auth.CurrentUser?.ToUserInfo() ?? new UserInfo();

        public async UniTask<UserInfo> InitializeAsync()
        {
            if (_auth.CurrentUser == null)
            {
                return new UserInfo();
            }

            var user = _auth.CurrentUser;

            switch (_config.CredentialValidationOnLogin)
            {
                case LoginCredentialValidateMode.Latest:
                    return await ValidateLatestProvider(user);
                case LoginCredentialValidateMode.All:
                    return await ValidateAllProvider(user);
            }

            return user.ToUserInfo();
        }


        public async UniTask<UserInfo> SignInAnonymous()
        {
            ExceptionUtil.ThrowIfCreatedUser(CurrentUser);

            var result = await _auth.SignInAnonymouslyAsync();

            Debug.Log($"anonymous: {result.Credential.Provider}");
            foreach (var userInfo in result.User.ProviderData)
            {
                Debug.Log(userInfo.ProviderId);
            }

            Debug.Log($"is anonymous: {result.User.IsAnonymous}");

            return result.User.ToUserInfo();
        }

        private async UniTask<UserInfo> ValidateLatestProvider(FirebaseUser user)
        {
            var providerUsers = user.ProviderData.Select(_ => new SafeUserInfo(_)).ToArray();

            IUserInfo signedUser = null;
            if (providerUsers.Any())
            {
                var latestProviderId = LatestProviderId;
                signedUser = providerUsers.FirstOrDefault(_ => _.ProviderId == latestProviderId) ??
                             providerUsers.FirstOrDefault();
            }
            else
            {
                return user.ToUserInfo();
            }

            if (signedUser == null || !_config.Providers.TryGetValue(signedUser.ProviderId, out var provider))
            {
                _auth.SignOut();
                throw new InvalidCredentialException(InvalidCredentialReason.NotSupportedProvider);
            }

            var isValid = await provider.ValidateAsync(signedUser);
            if (!isValid)
            {
                _auth.SignOut();
                throw new InvalidCredentialException(InvalidCredentialReason.InvalidCredential);
            }

            return user.ToUserInfo();
        }

        private async UniTask<UserInfo> ValidateAllProvider(FirebaseUser user)
        {
            var providerUsers = user.ProviderData.Select(_ => new SafeUserInfo(_)).ToArray();
            foreach (var providerUser in providerUsers)
            {
                if (!_config.Providers.TryGetValue(providerUser.ProviderId, out var provider)) continue;

                var isValid = await provider.ValidateAsync(providerUser);
                if (isValid) continue;

                try
                {
                    var unlinkResult = await user.UnlinkAsync(providerUser.ProviderId);
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            return user.ToUserInfo();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="providerId"></param>
        /// <returns></returns>
        /// <exception cref="SignInFailedException"></exception>
        public async UniTask<UserInfo> SignInAsync(string providerId)
        {
            ExceptionUtil.ThrowIfCreatedUser(CurrentUser);

            if (!_config.Providers.TryGetValue(providerId, out var provider))
            {
                throw new SignInFailedException(SignInFailReason.NotSupportProvider, providerId);
            }

            var credential = await provider.SignInAsync();
            FirebaseUser user;
            try
            {
                user = await _auth.SignInWithCredentialAsync(credential);
            }
            catch (Exception e)
            {
                throw new SignInFailedException(SignInFailReason.PlatformCredentialFailed, providerId, e);
            }

            Debug.Log($"sign in with {credential.Provider} and anonymous: {user.IsAnonymous}");
            LatestProviderId = providerId;
            return user.ToUserInfo();
        }

        public async UniTask<bool> SignOutAsync(string providerId)
        {
            AuthResult result = null;
            try
            {
                result = await _auth.CurrentUser.UnlinkAsync(providerId);
            }
            catch (Exception e)
            {
                return false;
            }

            if (LatestProviderId == result.Credential.Provider)
            {
                LatestProviderId = null;
            }

            Debug.Log($"sign out {providerId}: {result.User.IsAnonymous}");

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="providerId"></param>
        /// <returns>success provider id</returns>
        /// <exception cref="SignInFailedException"></exception>
        /// <exception cref="LinkFailedException"></exception>
        public async UniTask<string> LinkAsync(string providerId)
        {
            if (!_config.Providers.TryGetValue(providerId, out var provider))
            {
                throw new SignInFailedException(SignInFailReason.NotSupportProvider, providerId);
            }

            var credential = await provider.SignInAsync();

            Debug.Log($"link get credential: {credential.IsValid()} {credential.Provider}");

            AuthResult result = null;
            try
            {
                result = await _auth.CurrentUser.LinkWithCredentialAsync(credential);
                Debug.Log($"link success: {result.User.ProviderData.Count()}");
            }
            // FirebaseAccountLinkException: 기존 Firebase 계정이 이미 Apple 계정에 연결되었다는 오류
            // https://firebase.google.com/docs/auth/unity/apple?hl=ko#on-apple-platforms
            catch (Exception e)
            {
                Debug.Log($"link failed: {e}");
                throw new LinkFailedException(providerId, e);
            }

            foreach (var userInfo in result.User.ProviderData)
            {
                Debug.Log($"linked: {userInfo.ProviderId}");
            }

            // 링크된 공급자가 없을 경우 signout시 missing되어 버리면서 기존 연동했던 이메일이 남아있는 상태가 되어버려 
            // 사용할 수 없게 된다

            var linkedProviderId = result.AdditionalUserInfo.ProviderId;
            Debug.Log($"linked user: {result.User.IsAnonymous}");
            return linkedProviderId;
        }

        public async UniTask UnlinkAsync(string providerId)
        {
            var authResult = await _auth.CurrentUser.UnlinkAsync(providerId);
            Debug.Log($"unlink success: {authResult.Credential.Provider}, {authResult.User.ProviderData.Count()}");
        }

        public UniTask SignOutAsync()
        {
            ExceptionUtil.ThrowIfInvalidUser(CurrentUser);
            _auth.SignOut();
            return UniTask.CompletedTask;
        }

        public async UniTask DeleteAsync()
        {
            ExceptionUtil.ThrowIfInvalidUser(CurrentUser);

            try
            {
                await _auth.CurrentUser.DeleteAsync();
                _auth.SignOut();
            }
            catch (Exception)
            {
                // 재인증이 필요할 경우 Exception 발생
                // Reauthenticate로 재인증 성공 시 Delete 재진행
                throw;
            }
        }


        public bool IsSupportCredential(string providerId)
        {
            return _config.Providers.ContainsKey(providerId);
        }

        public bool IsConnectedProvider(string providerId)
        {
            if (_auth.CurrentUser == null) return false;

            foreach (var userInfo in _auth.CurrentUser.ProviderData)
            {
                if (userInfo.ProviderId == providerId)
                {
                    return true;
                }
            }

            return false;
        }

        public string[] GetConnectedProviders()
        {
            if (_auth.CurrentUser == null) return Array.Empty<string>();
            return _auth.CurrentUser.ProviderData.Select(_ => _.ProviderId).ToArray();
        }
    }

    internal class SafeUserInfo : IUserInfo
    {
        public string DisplayName { get; }
        public string Email { get; }
        public Uri PhotoUrl { get; }
        public string ProviderId { get; }
        public string UserId { get; }

        public SafeUserInfo(IUserInfo userInfo)
        {
            DisplayName = userInfo.DisplayName;
            Email = userInfo.Email;
            PhotoUrl = userInfo.PhotoUrl;
            ProviderId = userInfo.ProviderId;
            UserId = userInfo.UserId;
        }
    }

    public static class UserInfoExtensions
    {
        public static UserInfo ToUserInfo(this FirebaseUser user)
        {
            return new UserInfo(
                user.UserId,
                user.Email,
                user.IsAnonymous
            );
        }
    }
}