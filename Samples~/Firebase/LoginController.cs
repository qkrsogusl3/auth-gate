#if UNITY_2021_1_OR_NEWER
using System;
using System.Collections.Generic;
using AuthGate.Firebase.Apple;
using AuthGate.Firebase.Google;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AuthGate.Samples.Firebase
{
    [RequireComponent(typeof(UIDocument))]
    public class LoginController : MonoBehaviour
    {
        private UIDocument _document;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            var uxml = Resources.Load<VisualTreeAsset>(nameof(LoginController));
            _document.visualTreeAsset = uxml;
            _document.panelSettings.screenMatchMode = PanelScreenMatchMode.Shrink;
        }

        private async UniTaskVoid LoginAsync()
        {
            // render
            isLoading = true;
            Render();

            // effect
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            var gate = Resources.Load<VirtualGate>(nameof(VirtualGate));
            var user = await GateManager.InitializeAsync(gate);

            // render
            isInitialized = true;
            isValidUser = user.IsValid();
            userId = user.UserId;
            email = user.Email;
            isLoading = false;
            Render();
        }

        #region State

        public bool isLoading;

        public string userId;
        public string email;

        public bool isInitialized;
        public bool isValidUser;

        #endregion


        private void OnEnable()
        {
            _document.rootVisualElement?.Bind(new SerializedObject(this));
            _document.rootVisualElement?.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _document.rootVisualElement?.RegisterCallback<DetachFromPanelEvent>(OnDetachPanel);
            Render();
        }

        private void OnDisable()
        {
            _document.rootVisualElement?.Unbind();
            _document.rootVisualElement?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _document.rootVisualElement?.UnregisterCallback<DetachFromPanelEvent>(OnDetachPanel);
        }

        private void OnValidate()
        {
            Render();
        }

        private void OnDetachPanel(DetachFromPanelEvent e)
        {
        }

        private void OnGeometryChanged(GeometryChangedEvent e)
        {
            if (_renderCount > 1) // start -> on enable(render) -> geometry changed(render skip)
            {
                Render();
            }
        }

        private int _renderCount = 0;

        private readonly NameQuery<VisualElement> _loading = "loading";
        private readonly NameQuery<TextField> _uidField = "uid-field";
        private readonly NameQuery<TextField> _emailField = "email-field";
        private readonly NameQuery<Button> _loginButton = "login-button";
        private readonly NameQuery<Button> _logoutButton = "logout-button";
        private readonly NameQuery<Button> _deleteButton = "delete-button";
        private readonly ClickContext _loginClickContext = new ClickContext();
        private readonly ClickContext _deleteClickContext = new ClickContext();

        private readonly NameQuery<Button> _googleButton = "google-button";
        private readonly NameQuery<Button> _appleButton = "apple-button";
        private readonly NameQuery<Button> _guestButton = "guest-button";
        private readonly ClickContext _googleClickContext = new ClickContext();
        private readonly ClickContext _appleClickContext = new ClickContext();
        private readonly ClickContext _guestClickContext = new ClickContext();

        private void Render()
        {
            if (_document == null) return;
            var root = _document.rootVisualElement;
            if (root == null) return;

            using (_loading.Scope(root, out var loading))
            {
                loading.visible = isLoading;
            }

            using (_uidField.Scope(root, out var uid))
            using (_emailField.Scope(root, out var email))
            {
                uid.value = userId;
                email.value = this.email;
            }

            using (_loginButton.Scope(root, out var login))
            using (_logoutButton.Scope(root, out var logout))
            using (_deleteButton.Scope(root, out var delete))
            using (_loginClickContext.Scope(login, out var loginClick))
            using (_deleteClickContext.Scope(delete, out var deleteClick))
            {
                login.visible = !isInitialized;
                logout.visible = isValidUser;
                delete.visible = isValidUser;

                loginClick.Add(() =>
                {
                    Debug.Log("clicked");
                    LoginAsync().Forget();
                });
                deleteClick.Add(() => DeleteAsync().Forget());
            }

            using (_googleButton.Scope(root, out var google))
            using (_appleButton.Scope(root, out var apple))
            using (_guestButton.Scope(root, out var guest))
            using (_googleClickContext.Scope(google, out var googleClick))
            using (_appleClickContext.Scope(apple, out var appleClick))
            using (_guestClickContext.Scope(guest, out var guestClick))
            {
                google.visible = GateManager.CanSignIn(GoogleProvider.Id);
                apple.visible = GateManager.CanSignIn(AppleProvider.Id);
                guest.visible = isInitialized && !GateManager.GetUser().IsValid();

                googleClick.Add(() => { SignInAsync(GoogleProvider.Id).Forget(); });
                appleClick.Add(() => { SignInAsync(AppleProvider.Id).Forget(); });
                guestClick.Add(() => { SignInAsync("guest").Forget(); });
            }

            _renderCount++;
        }

        private async UniTaskVoid DeleteAsync()
        {
            await GateManager.DeleteAsync();

            var user = GateManager.GetUser();
            userId = user.UserId;
            email = user.Email;
            isValidUser = user.IsValid();
            Render();
        }

        private async UniTaskVoid SignInAsync(string providerId)
        {
            isLoading = true;
            Render();
            var user = providerId == "guest"
                ? await GateManager.SignInAnonymousAsync()
                : await GateManager.SignInAsync(providerId);

            Debug.Log(user);
            userId = user.UserId;
            email = user.Email;
            isValidUser = user.IsValid();

            isLoading = false;
            Render();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                AssetDatabase.Refresh();
            }
        }
    }

    public class ClickContext : ClickContext.IListener
    {
        private Button _button;

        public Button Target
        {
            set
            {
                if (value == null)
                {
                    Clear();
                    return;
                }

                if (_button == null)
                {
                    _button = value;
                    _button.RegisterCallback<DetachFromPanelEvent>(OnDetach);
                    _isDirty = true;
                    return;
                }

                if (_button.GetHashCode() == value.GetHashCode()) return;

                _isDirty = true;
                Clear();
                _button = value;
            }
        }

        private bool _isDirty = false;
        private readonly List<Action> _actions = new List<Action>();

        private void OnDetach(DetachFromPanelEvent e)
        {
            Clear();
        }

        private void Clear()
        {
            if (_button != null)
            {
                foreach (var action in _actions)
                {
                    _button.clicked -= action;
                }
            }

            _actions.Clear();
            _button = null;
        }

        void IListener.Add(Action action)
        {
            if (!_isDirty) return;
            _button.clicked += action;
            _actions.Add(action);
        }

        public IDisposable Scope(Button target, out IListener listener)
        {
            Target = target;
            listener = this;
            return new AddScope(this);
        }

        public interface IListener
        {
            void Add(Action action);
        }

        private readonly struct AddScope : IDisposable
        {
            private readonly ClickContext _context;

            public AddScope(ClickContext context)
            {
                _context = context;
            }

            public void Dispose()
            {
                _context._isDirty = false;
            }
        }
    }

    public interface IQuery<out T> where T : VisualElement
    {
        T Q(VisualElement target);
    }

    public readonly struct NameQuery<T> : IQuery<T> where T : VisualElement
    {
        private readonly string _name;

        public NameQuery(string name)
        {
            _name = name;
        }

        public T Q(VisualElement target) => target.Q<T>(_name);

        public static implicit operator NameQuery<T>(string name) => new NameQuery<T>(name);
    }

    public readonly struct ClassQuery<T> : IQuery<T> where T : VisualElement
    {
        private readonly string _className;

        public ClassQuery(string className)
        {
            _className = className;
        }

        public T Q(VisualElement target) => target.Q<T>(className: _className);

        public static implicit operator ClassQuery<T>(string className) => new ClassQuery<T>(className);
    }

    public static class QueryExtensions
    {
        public static QueryScope<T> Scope<T>(this IQuery<T> query, VisualElement target, out T element)
            where T : VisualElement
        {
            var scope = new QueryScope<T>(query, target);
            element = scope.Element;
            return scope;
        }
    }

    public readonly struct QueryScope<T> : IDisposable where T : VisualElement
    {
        public readonly T Element;

        public QueryScope(in IQuery<T> query, VisualElement target)
        {
            Element = query.Q(target);
        }

        public void Dispose()
        {
        }
    }
}
#endif