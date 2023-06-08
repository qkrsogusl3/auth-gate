using System;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Google;

namespace AuthGate.Firebase.Google
{
    public class GoogleProvider : IFirebaseProvider
    {
        public const string Id = "google.com";

        public bool IsSupported
        {
            get
            {
                // only mobile
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
                return true;
#endif
                return false;
            }
        }

        public string ProviderId => Id;
        private GoogleSignIn Google => GoogleSignIn.DefaultInstance;

        private GoogleProvider()
        {
        }

        public GoogleProvider(string webClientId)
        {
            if (string.IsNullOrEmpty(webClientId))
            {
                throw new Exception("require google webClientId");
            }

            GoogleSignIn.Configuration = new GoogleSignInConfiguration()
            {
                UseGameSignIn = false,
                RequestIdToken = true,
                RequestEmail = true,
                WebClientId = webClientId
            };
        }

        public async UniTask<bool> Validate(IUserInfo userInfo)
        {
            try
            {
                var user = await Google.SignInSilently();
                return user != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async UniTask<Credential> SignIn()
        {
            GoogleSignInUser signIn;
            try
            {
                signIn = await Google.SignIn();
            }
            catch (Exception)
            {
                throw new SignInFailedException(SignInFailReason.PlatformCredentialFailed, Id);
            }

            return GoogleAuthProvider.GetCredential(signIn.IdToken, null);
        }
    }
}