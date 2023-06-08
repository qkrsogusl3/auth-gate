using System;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;

namespace AuthGate
{
    public static class GateManager
    {
        private static IGate _gate;

        public static UniTask<UserInfo> InitializeAsync(IGate gate)
        {
            Assert.IsNotNull(gate);
            _gate = gate;
            return gate.InitializeAsync();
        }

        public static UserInfo GetUser() => _gate?.CurrentUser ?? new UserInfo();

        public static bool IsSupportCredential(string providerId) =>
            _gate?.IsSupportCredential(providerId) ?? false;

        public static bool IsConnectedProvider(string providerId) =>
            _gate?.IsConnectedProvider(providerId) ?? false;

        public static string[] GetConnectedProviders() =>
            _gate?.GetConnectedProviders() ?? Array.Empty<string>();

        public static UniTask<UserInfo> SignInAnonymousAsync()
        {
            ExceptionUtil.ThrowIfNotInitialized(_gate);
            return _gate.SignInAnonymous();
        }

        /// <summary>
        /// 단일 계정 연결.
        /// 기존에 SignIn되어 있는 공급자가 있다면 SignOut처리 됨
        /// </summary>
        /// <param name="providerId"></param>
        /// <returns></returns>
        public static async UniTask<UserInfo> SignInAsync(string providerId)
        {
            ExceptionUtil.ThrowIfNotInitialized(_gate);
            return await _gate.SignInAsync(providerId);
        }

        public static async UniTask<string> LinkAsync(string providerId)
        {
            ExceptionUtil.ThrowIfNotInitialized(_gate);
            return await _gate.LinkAsync(providerId);
        }

        public static UniTask UnlinkAsync(string providerId)
        {
            ExceptionUtil.ThrowIfNotInitialized(_gate);
            return _gate.UnlinkAsync(providerId);
        }

        public static UniTask DeleteAsync()
        {
            ExceptionUtil.ThrowIfNotInitialized(_gate);
            return _gate.DeleteAsync();
        }


        public static bool CanLink(string providerId)
        {
            if (_gate == null) return false;
            return _gate.CurrentUser.IsValid() && !_gate.IsConnectedProvider(providerId);
        }

        public static bool CanSignOut(string providerId)
        {
            if (_gate == null) return false;
            return _gate.CurrentUser.IsValid() && _gate.IsConnectedProvider(providerId);
        }

        public static bool CanSignIn(string providerId)
        {
            if (_gate == null) return false;
            return !_gate.CurrentUser.IsValid() && !_gate.IsConnectedProvider(providerId);
        }
    }
}