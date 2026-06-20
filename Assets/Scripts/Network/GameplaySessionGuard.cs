using Catsss.Core.Services;
using Catsss.Menu;
using Catsss.Menu.Flow;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Network
{
    /// <summary>
    /// DontDestroyOnLoad: во время InGame следит за NGO-сессией и мягко возвращает в меню.
    /// Клиент отключился — хост остаётся; хост пропал — клиент с баннером HostDisconnected.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplaySessionGuard : MonoBehaviour
    {
        private const float MonitoringGraceSeconds = 2f;
        private const float DisconnectPollConfirmSeconds = 0.75f;

        private IAppFlowCommands _flow;
        private bool _monitoring;
        private bool _networkEventsSubscribed;
        private bool _connectionManagerSubscribed;
        private bool _disconnectHandled;
        private float _monitoringStartedAt;
        private float _disconnectedPollSince = -1f;

        public void Initialize(IAppFlowCommands flow)
        {
            _flow = flow;
        }

        public void SetMonitoring(bool enabled)
        {
            if (_monitoring == enabled)
            {
                return;
            }

            _monitoring = enabled;

            if (_monitoring)
            {
                _disconnectHandled = false;
                _disconnectedPollSince = -1f;
                _monitoringStartedAt = Time.unscaledTime;
                SessionEndSignal.Clear();
                TrySubscribeConnectionManager();
                TrySubscribeNetworkEvents();
            }
            else
            {
                UnsubscribeConnectionManager();
                UnsubscribeNetworkEvents();
                SessionEndSignal.Clear();
                _disconnectedPollSince = -1f;
            }
        }

        private void Update()
        {
            if (!_monitoring || _flow == null || _flow.IsReturningFlow || _disconnectHandled)
            {
                return;
            }

            if (!_connectionManagerSubscribed)
            {
                TrySubscribeConnectionManager();
            }

            if (!_networkEventsSubscribed)
            {
                TrySubscribeNetworkEvents();
            }

            if (!IsMonitoringGraceElapsed())
            {
                return;
            }

            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager == null || networkManager.IsHost)
            {
                _disconnectedPollSince = -1f;
                return;
            }

            if (networkManager.IsClient && networkManager.IsConnectedClient)
            {
                _disconnectedPollSince = -1f;
                return;
            }

            if (!networkManager.IsClient)
            {
                return;
            }

            if (_disconnectedPollSince < 0f)
            {
                _disconnectedPollSince = Time.unscaledTime;
                return;
            }

            if (Time.unscaledTime - _disconnectedPollSince >= DisconnectPollConfirmSeconds)
            {
                HandleLocalClientConnectionLost("Connection lost (poll).");
            }
        }

        private bool IsMonitoringGraceElapsed()
        {
            return Time.unscaledTime - _monitoringStartedAt >= MonitoringGraceSeconds;
        }

        private bool CanHandleDisconnectEvent()
        {
            return _monitoring
                && _flow != null
                && !_flow.IsReturningFlow
                && !_disconnectHandled
                && IsMonitoringGraceElapsed();
        }

        private void TrySubscribeConnectionManager()
        {
            if (_connectionManagerSubscribed)
            {
                return;
            }

            if (!ServiceLocator.TryGet(out ConnectionManager connectionManager))
            {
                connectionManager = FindAnyObjectByType<ConnectionManager>();
            }

            if (connectionManager == null)
            {
                return;
            }

            connectionManager.ConnectionFailed += HandleConnectionFailed;
            _connectionManagerSubscribed = true;
        }

        private void UnsubscribeConnectionManager()
        {
            if (!_connectionManagerSubscribed)
            {
                return;
            }

            if (ServiceLocator.TryGet(out ConnectionManager connectionManager))
            {
                connectionManager.ConnectionFailed -= HandleConnectionFailed;
            }
            else
            {
                ConnectionManager found = FindAnyObjectByType<ConnectionManager>();

                if (found != null)
                {
                    found.ConnectionFailed -= HandleConnectionFailed;
                }
            }

            _connectionManagerSubscribed = false;
        }

        private void TrySubscribeNetworkEvents()
        {
            if (_networkEventsSubscribed)
            {
                return;
            }

            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager == null)
            {
                return;
            }

            networkManager.OnClientDisconnectCallback += HandleClientDisconnect;
            networkManager.OnTransportFailure += HandleTransportFailure;
            _networkEventsSubscribed = true;
        }

        private void UnsubscribeNetworkEvents()
        {
            if (!_networkEventsSubscribed)
            {
                return;
            }

            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager != null)
            {
                networkManager.OnClientDisconnectCallback -= HandleClientDisconnect;
                networkManager.OnTransportFailure -= HandleTransportFailure;
            }

            _networkEventsSubscribed = false;
        }

        private void HandleConnectionFailed(string message)
        {
            if (!CanHandleDisconnectEvent())
            {
                return;
            }

            if (IsHostSessionHealthy())
            {
                Debug.LogWarning(
                    $"[GameplaySessionGuard] Ignoring ConnectionFailed while host session is healthy: {message}");
                return;
            }

            Debug.LogWarning($"[GameplaySessionGuard] Transport/connect failure during session: {message}");
            HandleLocalClientConnectionLost(message);
        }

        private void HandleTransportFailure()
        {
            if (!CanHandleDisconnectEvent())
            {
                return;
            }

            if (IsHostSessionHealthy())
            {
                Debug.LogWarning("[GameplaySessionGuard] Ignoring OnTransportFailure while host session is healthy.");
                return;
            }

            Debug.LogWarning("[GameplaySessionGuard] OnTransportFailure during session.");
            HandleLocalClientConnectionLost("Transport failure.");
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            if (!CanHandleDisconnectEvent())
            {
                return;
            }

            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager == null)
            {
                return;
            }

            if (networkManager.IsServer && clientId != networkManager.LocalClientId)
            {
                Debug.Log(
                    $"[GameplaySessionGuard] Remote client {clientId} left. Host session continues " +
                    $"({networkManager.ConnectedClientsList.Count} connected).");
                return;
            }

            if (!networkManager.IsHost && clientId == networkManager.LocalClientId)
            {
                HandleLocalClientConnectionLost("Local client disconnected.");
            }
        }

        private void HandleLocalClientConnectionLost(string logContext)
        {
            if (_disconnectHandled || _flow == null || _flow.IsReturningFlow)
            {
                return;
            }

            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager != null && networkManager.IsHost)
            {
                if (IsHostSessionHealthy())
                {
                    Debug.LogWarning(
                        $"[GameplaySessionGuard] Ignoring disconnect signal while host session is healthy: {logContext}");
                    return;
                }

                Debug.LogWarning(
                    $"[GameplaySessionGuard] Host transport issue: {logContext}. Ending session for host.");
                _disconnectHandled = true;
                _flow.RequestReturnToMainMenu(MenuReturnReason.SessionEnded, stopNetwork: true);
                return;
            }

            MenuReturnReason reason = SessionEndSignal.TryConsumeExpectedRemoteEnd(out MenuReturnReason expected)
                ? expected
                : MenuReturnReason.HostDisconnected;

            Debug.LogWarning($"[GameplaySessionGuard] Client lost session ({logContext}) → {reason}.");
            _disconnectHandled = true;
            _flow.RequestReturnToMainMenu(reason, stopNetwork: true);
        }

        private static bool IsHostSessionHealthy()
        {
            NetworkManager networkManager = NetworkManager.Singleton;

            return networkManager != null
                && networkManager.IsHost
                && networkManager.IsListening;
        }
    }
}
