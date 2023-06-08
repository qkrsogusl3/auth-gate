using Cysharp.Threading.Tasks;
using Firebase.Auth;

namespace AuthGate.Firebase
{
    public interface IFirebaseProvider : ICredentialProvider
    {
        UniTask<bool> Validate(IUserInfo userInfo);
        UniTask<Credential> SignIn();
    }
}