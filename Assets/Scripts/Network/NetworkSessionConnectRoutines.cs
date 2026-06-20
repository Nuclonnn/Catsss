using System;
using System.Collections;
using Catsss.Menu;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Network
{
    /// <summary>
    /// Общие coroutine для NGO host/client connect.
    /// Используются <see cref="Menu.Flow.ApplicationFlowController"/> и <see cref="GameplayNetworkSessionStarter"/>.
    /// </summary>
    public static class NetworkSessionConnectRoutines
    {
        public enum ClientConnectOutcome
        {
            Connected,
            InvalidInput,
            ImmediateFailure,
            FailedDuringWait,
            TimedOut,
        }

        public readonly struct ClientConnectParameters
        {
            public ClientConnectParameters(
                string remoteHost,
                ushort port,
                float timeoutSeconds,
                float holdOverlaySecondsAfterConnect,
                string logContext)
            {
                RemoteHost = remoteHost ?? string.Empty;
                Port = port;
                TimeoutSeconds = Mathf.Max(1f, timeoutSeconds);
                HoldOverlaySecondsAfterConnect = Mathf.Max(0f, holdOverlaySecondsAfterConnect);
                LogContext = string.IsNullOrWhiteSpace(logContext) ? nameof(NetworkSessionConnectRoutines) : logContext;
            }

            public string RemoteHost { get; }

            public ushort Port { get; }

            public float TimeoutSeconds { get; }

            public float HoldOverlaySecondsAfterConnect { get; }

            public string LogContext { get; }
        }

        public readonly struct ClientConnectResult
        {
            public ClientConnectResult(ClientConnectOutcome outcome, ClientConnectInputError inputError, string failureMessage)
            {
                Outcome = outcome;
                InputError = inputError;
                FailureMessage = failureMessage ?? string.Empty;
            }

            public ClientConnectOutcome Outcome { get; }

            public ClientConnectInputError InputError { get; }

            public string FailureMessage { get; }

            public bool IsConnected => Outcome == ClientConnectOutcome.Connected;
        }

        /// <summary>Сервер: configure + StartHost + опциональная пауза overlay.</summary>
        public static IEnumerator StartHost(
            ConnectionManager manager,
            ushort port,
            float holdOverlaySecondsAfterConnect)
        {
            manager.ConfigureForHost(port);
            manager.StartHost();
            yield return HoldOverlayRoutine(holdOverlaySecondsAfterConnect);
        }

        /// <summary>Клиент: validate → StartClient → wait connected или timeout/failure.</summary>
        public static IEnumerator ConnectClient(
            ConnectionManager manager,
            ClientConnectParameters parameters,
            Action<ClientConnectResult> onFinished)
        {
            if (manager == null)
            {
                onFinished?.Invoke(new ClientConnectResult(
                    ClientConnectOutcome.ImmediateFailure,
                    ClientConnectInputError.InvalidHost,
                    "ConnectionManager is null."));
                yield break;
            }

            if (!ClientConnectInputValidator.TryValidate(
                    parameters.RemoteHost,
                    parameters.Port,
                    out ClientConnectInputError inputError))
            {
                Debug.LogWarning($"[{parameters.LogContext}] Rejected client input: {inputError}");
                onFinished?.Invoke(new ClientConnectResult(ClientConnectOutcome.InvalidInput, inputError, null));
                yield break;
            }

            bool connectionFailed;
            string failureLogMessage = null;

            void OnConnectionFailed(string message)
            {
                connectionFailed = true;
                failureLogMessage = message;
            }

            connectionFailed = false;
            manager.ConnectionFailed += OnConnectionFailed;

            manager.ConfigureForClient(parameters.RemoteHost, parameters.Port);
            manager.StartClient();

            yield return null;

            if (connectionFailed)
            {
                manager.ConnectionFailed -= OnConnectionFailed;
                Debug.LogWarning(
                    $"[{parameters.LogContext}] Client connect failed immediately: {failureLogMessage}");
                onFinished?.Invoke(new ClientConnectResult(
                    ClientConnectOutcome.ImmediateFailure,
                    ClientConnectInputError.None,
                    failureLogMessage));
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < parameters.TimeoutSeconds)
            {
                if (connectionFailed)
                {
                    break;
                }

                NetworkManager networkManager = NetworkManager.Singleton;

                if (networkManager != null && networkManager.IsConnectedClient)
                {
                    manager.ConnectionFailed -= OnConnectionFailed;
                    yield return HoldOverlayRoutine(parameters.HoldOverlaySecondsAfterConnect);
                    onFinished?.Invoke(new ClientConnectResult(ClientConnectOutcome.Connected, ClientConnectInputError.None, null));
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            manager.ConnectionFailed -= OnConnectionFailed;

            ClientConnectOutcome outcome = connectionFailed
                ? ClientConnectOutcome.FailedDuringWait
                : ClientConnectOutcome.TimedOut;

            if (outcome == ClientConnectOutcome.TimedOut)
            {
                Debug.LogWarning(
                    $"[{parameters.LogContext}] Client connect timeout ({parameters.TimeoutSeconds:0.#}s) " +
                    $"to {parameters.RemoteHost}:{parameters.Port}.");
            }
            else
            {
                Debug.LogWarning(
                    $"[{parameters.LogContext}] Client connect failed: {failureLogMessage}");
            }

            onFinished?.Invoke(new ClientConnectResult(outcome, ClientConnectInputError.None, failureLogMessage));
        }

        private static IEnumerator HoldOverlayRoutine(float holdOverlaySecondsAfterConnect)
        {
            if (holdOverlaySecondsAfterConnect > 0f)
            {
                yield return new WaitForSecondsRealtime(holdOverlaySecondsAfterConnect);
            }
        }
    }
}
