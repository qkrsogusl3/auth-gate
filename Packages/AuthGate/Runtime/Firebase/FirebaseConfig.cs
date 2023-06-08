using System.Collections.Generic;

namespace AuthGate.Firebase
{
    public enum LoginCredentialValidateMode
    {
        Latest,
        All
    }

    public partial class FirebaseConfig
    {
        public LoginCredentialValidateMode CredentialValidationOnLogin = LoginCredentialValidateMode.Latest;

        internal readonly Dictionary<string, IFirebaseProvider> Providers =
            new Dictionary<string, IFirebaseProvider>();

        public void AddCredentialProvider(IFirebaseProvider provider)
        {
            Providers.Add(provider.ProviderId, provider);
        }
    }
}