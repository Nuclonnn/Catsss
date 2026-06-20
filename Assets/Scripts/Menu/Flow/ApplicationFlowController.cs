using System.Collections;
using Catsss.Core.FSM;
using Catsss.Core.Services;
using Catsss.Menu.Flow.Services;
using Catsss.Network;
using Catsss.Settings;
using UnityEngine;

namespace Catsss.Menu.Flow
{
    /// <summary>
    /// DontDestroyOnLoad-дирижёр FSM. Coroutine-логика — в <see cref="Services.AppFlowSessionConnectService"/> и сервисах возврата.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class ApplicationFlowController : MonoBehaviour, IAppFlowCommands, IAppFlowRoutineHost
    {
        public const string DefaultMainMenuSceneName = "MainMenu";
        public const string DefaultGameplaySceneName = "Sandbox";

        [SerializeField]
        private string mainMenuSceneName = DefaultMainMenuSceneName;

        private static ApplicationFlowController _instance;

        private StateMachine _fsm;
        private AppFlowMainMenuState _mainMenuState;
        private AppFlowLoadingState _loadingState;
        private AppFlowInGameState _inGameState;
        private AppFlowReturningState _returningState;

        private NetworkSessionIntent.LaunchPayload _activeLaunch;
        private MenuLoadPresentation _loadPresentation = MenuLoadPresentation.Default;
        private Coroutine _sessionRoutine;
        private Coroutine _returnRoutine;
        private bool _sessionStartupHandled;
        private bool _returnStopNetwork = true;
        private GameplaySessionGuard _sessionGuard;

        public static ApplicationFlowController Instance => _instance;

        public bool IsSessionStartupHandled => _sessionStartupHandled;

        public IState CurrentFlowState => _fsm?.CurrentState;

        public bool IsInMainMenuFlow => ReferenceEquals(_fsm.CurrentState, _mainMenuState);

        public bool IsLoadingFlow => ReferenceEquals(_fsm.CurrentState, _loadingState);

        public bool IsInGameFlow => ReferenceEquals(_fsm.CurrentState, _inGameState);

        public bool IsReturningFlow => ReferenceEquals(_fsm.CurrentState, _returningState);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureExists();
        }

        public static ApplicationFlowController EnsureExists()
        {
            if (_instance != null)
            {
                return _instance;
            }

            var root = new GameObject(nameof(ApplicationFlowController));
            _instance = root.AddComponent<ApplicationFlowController>();
            return _instance;
        }

        public static bool TryGet(out IAppFlowCommands commands)
        {
            commands = _instance;
            return _instance != null;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            var settings = new UserSettingsService();
            settings.Load();

            if (!ServiceLocator.TryGet(out UserSettingsService _))
            {
                ServiceLocator.Register(settings);
            }

            InitializeStateMachine();
            EnsureSessionGuard();

            if (!ServiceLocator.TryGet(out IAppFlowCommands _))
            {
                ServiceLocator.Register<IAppFlowCommands>(this);
            }
        }

        private void EnsureSessionGuard()
        {
            _sessionGuard = GetComponent<GameplaySessionGuard>();

            if (_sessionGuard == null)
            {
                _sessionGuard = gameObject.AddComponent<GameplaySessionGuard>();
            }

            _sessionGuard.Initialize(this);
        }

        internal void EnableSessionGuard()
        {
            _sessionGuard?.SetMonitoring(true);
        }

        internal void DisableSessionGuard()
        {
            _sessionGuard?.SetMonitoring(false);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            if (ServiceLocator.TryGet(out IAppFlowCommands registered) && ReferenceEquals(registered, this))
            {
                ServiceLocator.Unregister<IAppFlowCommands>();
            }

            if (ServiceLocator.TryGet(out UserSettingsService settings))
            {
                ServiceLocator.Unregister<UserSettingsService>();
            }
        }

        private void Update()
        {
            _fsm?.Update();
        }

        private void FixedUpdate()
        {
            _fsm?.FixedUpdate();
        }

        private void InitializeStateMachine()
        {
            _mainMenuState = new AppFlowMainMenuState(this);
            _loadingState = new AppFlowLoadingState(this);
            _inGameState = new AppFlowInGameState(this);
            _returningState = new AppFlowReturningState(this);

            _fsm = new StateMachine();
            _fsm.SetState(_mainMenuState);
        }

        public void NotifyGameplaySessionActive()
        {
            if (IsReturningFlow || IsInGameFlow || IsLoadingFlow)
            {
                return;
            }

            EnterInGameState();
        }

        public void NotifyMainMenuSceneLoaded()
        {
            if (!IsInMainMenuFlow && !IsReturningFlow)
            {
                EnterMainMenuState();
            }
        }

        public void RequestHostSession(ushort port, string levelSceneName, MenuLoadPresentation presentation)
        {
            if (!IsInMainMenuFlow)
            {
                Debug.LogWarning("[ApplicationFlowController] Host request ignored — not in MainMenu flow.");
                return;
            }

            _activeLaunch = NetworkSessionIntent.LaunchPayload.ForHost(
                port,
                AppFlowSceneLoader.ResolveLevelSceneName(levelSceneName),
                presentation.OverlayHoldSecondsAfterConnect);
            _loadPresentation = presentation;
            ShowLoadingOverlay(_loadPresentation);
            _sessionStartupHandled = false;
            _fsm.SetState(_loadingState);
        }

        public void RequestClientSession(string hostAddress, ushort port, string levelSceneName, MenuLoadPresentation presentation)
        {
            if (!IsInMainMenuFlow)
            {
                Debug.LogWarning("[ApplicationFlowController] Client request ignored — not in MainMenu flow.");
                return;
            }

            _activeLaunch = NetworkSessionIntent.LaunchPayload.ForClient(
                hostAddress,
                port,
                AppFlowSceneLoader.ResolveLevelSceneName(levelSceneName),
                presentation.OverlayHoldSecondsAfterConnect);
            _loadPresentation = presentation;
            ShowLoadingOverlay(_loadPresentation);
            _sessionStartupHandled = false;
            _fsm.SetState(_loadingState);
        }

        public void RequestReturnToMainMenu(MenuReturnReason reason, bool stopNetwork = true)
        {
            if (IsReturningFlow)
            {
                return;
            }

            if (reason != MenuReturnReason.None && reason != MenuReturnReason.UserLeftSession)
            {
                AppFlowReturnCoordinator.ApplyReturnFeedback(reason);
            }

            if (stopNetwork && (IsInGameFlow || IsLoadingFlow))
            {
                AppFlowReturnCoordinator.TryNotifyRemoteClientsBeforeLocalShutdown(reason);
            }

            _returnStopNetwork = stopNetwork;
            _fsm.SetState(_returningState);
        }

        internal void BeginSessionLoadRoutine()
        {
            CancelSessionLoadRoutine();
            _sessionRoutine = StartCoroutine(
                AppFlowSessionConnectService.RunSessionLoad(this, _activeLaunch, _loadPresentation));
        }

        internal void CancelSessionLoadRoutine()
        {
            if (_sessionRoutine != null)
            {
                StopCoroutine(_sessionRoutine);
                _sessionRoutine = null;
            }
        }

        internal void BeginReturnToMainMenuRoutine()
        {
            CancelReturnToMainMenuRoutine();
            _returnRoutine = StartCoroutine(
                AppFlowReturnToMenuService.RunReturnToMainMenu(this, mainMenuSceneName, _returnStopNetwork));
        }

        internal void CancelReturnToMainMenuRoutine()
        {
            if (_returnRoutine != null)
            {
                StopCoroutine(_returnRoutine);
                _returnRoutine = null;
            }
        }

        void IAppFlowRoutineHost.EnterInGameState() => EnterInGameState();

        void IAppFlowRoutineHost.EnterReturningState() => EnterReturningState();

        void IAppFlowRoutineHost.EnterMainMenuState() => EnterMainMenuState();

        void IAppFlowRoutineHost.SetSessionStartupHandled(bool handled)
        {
            _sessionStartupHandled = handled;
        }

        private void EnterInGameState()
        {
            _fsm.SetState(_inGameState);
        }

        private void EnterReturningState()
        {
            _fsm.SetState(_returningState);
        }

        private void EnterMainMenuState()
        {
            _fsm.SetState(_mainMenuState);
        }

        private static void ShowLoadingOverlay(in MenuLoadPresentation presentation)
        {
            MenuLoadingOverlay overlay = MenuLoadingOverlay.Create(
                presentation.LoadingSplashSpriteOptional,
                presentation.LoadingBackdropColorWithoutSprite);
            overlay.Show();
        }
    }
}
