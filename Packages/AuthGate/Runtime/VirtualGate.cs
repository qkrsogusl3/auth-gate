using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AuthGate.Editor
{
    [Serializable]
    public class VirtualUser
    {
        public string userId;
        public string email;
        public bool isAnonymous;

        public static implicit operator UserInfo(VirtualUser user) => new UserInfo(
            user.userId,
            user.email,
            user.isAnonymous
        );
    }

    [CreateAssetMenu(menuName = "AuthGate/" + nameof(VirtualGate), fileName = nameof(VirtualGate))]
    public class VirtualGate : ScriptableObject, IGate
    {
#if UNITY_EDITOR
        public static IGate LoadFromAssetDatabase(string assetPath)
        {
            if (!assetPath.StartsWith("Assets/"))
            {
                assetPath = $"Assets/{assetPath}";
            }

            if (!Path.HasExtension("asset"))
            {
                assetPath = Path.ChangeExtension(assetPath, "asset");
            }

            Debug.Log($"load gate: {assetPath}");
            return AssetDatabase.LoadAssetAtPath<VirtualGate>(assetPath);
        }
#endif
        [SerializeField] private VirtualUser user;
        [SerializeField] private bool useCachedLogin = false;

        [SerializeField] private List<string> providers = new List<string>()
        {
            "google.com",
            "apple.com"
        };

        [NonSerialized] private VirtualUser _currentUser;
        [NonSerialized] private readonly HashSet<string> _connectedProviders = new HashSet<string>();

        public UserInfo CurrentUser => _currentUser ?? new UserInfo();

        public UniTask<UserInfo> InitializeAsync()
        {
            if (useCachedLogin)
            {
                _currentUser = CreateUser();
            }

            return UniTask.FromResult(CurrentUser);
        }

        public UniTask<UserInfo> SignInAnonymous()
        {
            ExceptionUtil.ThrowIfCreatedUser(CurrentUser);
            _currentUser = CreateUser();
            _currentUser.isAnonymous = true;
            return UniTask.FromResult(CurrentUser);
        }

        public UniTask<UserInfo> SignInAsync(string providerId)
        {
            ExceptionUtil.ThrowIfCreatedUser(CurrentUser);
            ExceptionUtil.ThrowIfNotSupportedProvider(providers.Contains(providerId), providerId);

            _currentUser = CreateUser();
            _connectedProviders.Add(providerId);

            return UniTask.FromResult(CurrentUser);
        }

        private VirtualUser CreateUser()
        {
            return new VirtualUser()
            {
                userId = user.userId,
                email = user.email,
                isAnonymous = user.isAnonymous
            };
        }

        public UniTask<string> LinkAsync(string providerId)
        {
            _connectedProviders.Add(providerId);
            return UniTask.FromResult(providerId);
        }


        public UniTask UnlinkAsync(string providerId)
        {
            _connectedProviders.Remove(providerId);
            return UniTask.CompletedTask;
        }

        public UniTask SignOutAsync()
        {
            ExceptionUtil.ThrowIfInvalidUser(CurrentUser);
            _currentUser = null;
            _connectedProviders.Clear();
            return UniTask.CompletedTask;
        }

        public UniTask DeleteAsync()
        {
            ExceptionUtil.ThrowIfInvalidUser(CurrentUser);
            _currentUser = null;
            _connectedProviders.Clear();
            return UniTask.CompletedTask;
        }

        public bool IsSupportCredential(string providerId)
        {
            return providers.Contains(providerId);
        }

        public bool IsConnectedProvider(string providerId)
        {
            return _connectedProviders.Contains(providerId);
        }

        public string[] GetConnectedProviders()
        {
            return _connectedProviders.ToArray();
        }
    }
}