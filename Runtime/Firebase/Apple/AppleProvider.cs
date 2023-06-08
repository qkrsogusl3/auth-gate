using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
using AppleAuth.Native;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AuthGate.Firebase.Apple
{
    public class AppleProvider : IFirebaseProvider
    {
        public const string Id = "apple.com";

        public string ProviderId => Id;
        public bool IsSupported => AppleAuthManager.IsCurrentPlatformSupported;

        private IAppleAuthManager _appleAuthManager;
        private AppleAuthManagerUpdater _updater;

        private IAppleAuthManager Manager
        {
            get
            {
                if (_appleAuthManager == null)
                {
                    // Creates a default JSON deserializer, to transform JSON Native responses to C# instances
                    var deserializer = new PayloadDeserializer();
                    // Creates an Apple Authentication manager with the deserializer
                    _appleAuthManager = new AppleAuthManager(deserializer);
                    _appleAuthManager.SetCredentialsRevokedCallback(OnCredentialRevoked);

                    _updater = new GameObject(nameof(AppleAuthManagerUpdater)).AddComponent<AppleAuthManagerUpdater>();
                    _updater.Manager = _appleAuthManager;
                    Object.DontDestroyOnLoad(_updater.gameObject);
                }

                return _appleAuthManager;
            }
        }

        public AppleProvider()
        {
        }

        private void OnCredentialRevoked(string result)
        {
            Debug.LogError($"[apple] revoked {result}");
        }

        private UniTask<CredentialState> GetCredentialStateAsync(string userId)
        {
            var source = new UniTaskCompletionSource<CredentialState>();
            Manager.GetCredentialState(userId, state => { source.TrySetResult(state); },
                err => { source.TrySetException(new AppleErrorException(err)); });
            return source.Task;
        }

        public async UniTask<bool> Validate(IUserInfo userInfo)
        {
            var state = await GetCredentialStateAsync(userInfo.UserId);
            return state == CredentialState.Authorized;
        }

        public async UniTask<Credential> SignIn()
        {
            var rawNonce = GenerateRandomString(32);
            var nonce = GenerateSHA256NonceFromRawNonce(rawNonce);

            SignInResult result;
            try
            {
                var loginArgs =
                    new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName, nonce);
                result = await LoginWithAppleIdAsync(Manager, loginArgs);
            }
            catch (AppleErrorException e)
            {
                switch (e.Code)
                {
                    case AuthorizationErrorCode.Canceled:
                        throw new SignInFailedException(SignInFailReason.PlatformCredentialCanceled, Id);
                    default:
                        throw new SignInFailedException(SignInFailReason.PlatformCredentialFailed, Id);
                }
            }

            return OAuthProvider.GetCredential(
                Id,
                result.IdToken,
                rawNonce,
                result.AuthCode
            );
        }

        private async UniTask<SignInResult> LoginWithAppleIdAsync(
            IAppleAuthManager manager, AppleAuthLoginArgs args)
        {
            var source = new UniTaskCompletionSource<IAppleIDCredential>();
            manager.LoginWithAppleId(args, result => { source.TrySetResult(result as IAppleIDCredential); },
                err => { source.TrySetException(new AppleErrorException(err)); });
            var credential = await source.Task;

            return new SignInResult(credential);
        }

        private static string GenerateSHA256NonceFromRawNonce(string rawNonce)
        {
            var sha = new SHA256Managed();
            var utf8RawNonce = Encoding.UTF8.GetBytes(rawNonce);
            var hash = sha.ComputeHash(utf8RawNonce);

            var result = string.Empty;
            for (var i = 0; i < hash.Length; i++)
            {
                result += hash[i].ToString("x2");
            }

            return result;
        }

        private static string GenerateRandomString(int length)
        {
            if (length <= 0)
            {
                throw new Exception("Expected nonce to have positive length");
            }

            const string charset = "0123456789ABCDEFGHIJKLMNOPQRSTUVXYZabcdefghijklmnopqrstuvwxyz-._";
            var cryptographicallySecureRandomNumberGenerator = new RNGCryptoServiceProvider();
            var result = string.Empty;
            var remainingLength = length;

            var randomNumberHolder = new byte[1];
            while (remainingLength > 0)
            {
                var randomNumbers = new List<int>(16);
                for (var randomNumberCount = 0; randomNumberCount < 16; randomNumberCount++)
                {
                    cryptographicallySecureRandomNumberGenerator.GetBytes(randomNumberHolder);
                    randomNumbers.Add(randomNumberHolder[0]);
                }

                for (var randomNumberIndex = 0; randomNumberIndex < randomNumbers.Count; randomNumberIndex++)
                {
                    if (remainingLength == 0)
                    {
                        break;
                    }

                    var randomNumber = randomNumbers[randomNumberIndex];
                    if (randomNumber < charset.Length)
                    {
                        result += charset[randomNumber];
                        remainingLength--;
                    }
                }
            }

            return result;
        }

        private readonly struct SignInResult
        {
            public bool IsValid()
            {
                return !string.IsNullOrEmpty(User) &&
                       !string.IsNullOrEmpty(IdToken) &&
                       !string.IsNullOrEmpty(AuthCode);
            }

            public readonly string User;
            public readonly string IdToken;
            public readonly string AuthCode;

            private SignInResult(string user, string idToken, string authCode)
            {
                User = user;
                IdToken = idToken;
                AuthCode = authCode;
            }

            public SignInResult(IAppleIDCredential credential)
            {
                User = credential.User;
                IdToken = Encoding.UTF8.GetString(credential.IdentityToken, 0,
                    credential.IdentityToken.Length);
                AuthCode = Encoding.UTF8.GetString(credential.AuthorizationCode, 0,
                    credential.AuthorizationCode.Length);
            }
        }

        public class AppleErrorException : Exception
        {
            public AuthorizationErrorCode Code { get; private set; }
            public override string Message { get; }

            public AppleErrorException(IAppleError error)
            {
                Code = error.GetAuthorizationErrorCode();
                Message = $"[{Code.ToString()}] {error.LocalizedFailureReason} {error.LocalizedDescription}";
            }
        }
    }
}