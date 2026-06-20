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

        private IAppFlowCommands _flow;

        private bool _monitoring;

        private bool _networkEventsSubscribed;

        private bool _connectionManagerSubscribed;

        private bool _disconnectHandled;



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

                SessionEndSignal.Clear();

                TrySubscribeConnectionManager();

                TrySubscribeNetworkEvents();

            }

            else

            {

                UnsubscribeConnectionManager();

                UnsubscribeNetworkEvents();

                SessionEndSignal.Clear();

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



            NetworkManager networkManager = NetworkManager.Singleton;



            if (networkManager == null)

            {

                return;

            }



            // Fallback: NGO callback иногда приходит позже transport timeout.

            if (!networkManager.IsHost && networkManager.IsClient && !networkManager.IsConnectedClient)

            {

                HandleLocalClientConnectionLost("Connection lost (poll).");

            }

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

            if (!_monitoring || _flow == null || _flow.IsReturningFlow || _disconnectHandled)

            {

                return;

            }



            Debug.LogWarning($"[GameplaySessionGuard] Transport/connect failure during session: {message}");

            HandleLocalClientConnectionLost(message);

        }



        private void HandleTransportFailure()

        {

            if (!_monitoring || _flow == null || _flow.IsReturningFlow || _disconnectHandled)

            {

                return;

            }



            Debug.LogWarning("[GameplaySessionGuard] OnTransportFailure during session.");

            HandleLocalClientConnectionLost("Transport failure.");

        }



        private void HandleClientDisconnect(ulong clientId)

        {

            if (!_monitoring || _flow == null || _flow.IsReturningFlow || _disconnectHandled)

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

    }

}


