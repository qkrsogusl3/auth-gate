using Cysharp.Threading.Tasks;

namespace AuthGate
{
    public interface IGate
    {
        UserInfo CurrentUser { get; }
        UniTask<UserInfo> InitializeAsync();

        UniTask<UserInfo> SignInAnonymous();
        UniTask<UserInfo> SignInAsync(string providerId);

        UniTask<string> LinkAsync(string providerId);
        UniTask UnlinkAsync(string providerId);

        UniTask SignOutAsync();

        UniTask DeleteAsync();

        bool IsSupportCredential(string providerId);
        bool IsConnectedProvider(string providerId);
        string[] GetConnectedProviders();
    }
}