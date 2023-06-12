using Cysharp.Threading.Tasks;
using Firebase.Auth;

namespace AuthGate.Firebase
{
    public interface IFirebaseProvider : ICredentialProvider
    {
        UniTask<bool> ValidateAsync(IUserInfo userInfo);
        UniTask<Credential> SignInAsync();
    }
}