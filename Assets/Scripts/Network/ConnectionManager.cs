using System;
using Catsss.Core.Services;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Catsss.Network
{
    [RequireComponent(typeof(NetworkManager))]
    [RequireComponent(typeof(UnityTransport))]
    public sealed class ConnectionManager : MonoBehaviour
    {
        /// <summary>Адрес, куда подключается <b>client</b> и куда клиент резолвит хост (LAN IP машины-хоста, 127.0.0.1 для одной ПК).</summary>
        [SerializeField] private string address = "127.0.0.1";

        [SerializeField] private ushort port = 7777;

        /// <summary>На чём слушает <b>host/server</b>. 0.0.0.0 — все интерфейсы; иначе внешние машины часто не подключатся (как одна только localhost).</summary>
        [SerializeField] private string serverListenAddress = "0.0.0.0";

        private enum TransportRole : byte
        {
            None = 0,
            Host = 1,
            Client = 2,
        }

        private NetworkManager _networkManager;
        private UnityTransport _transport;
        private TransportRole _transportRole;

        public event Action HostStarted;
        public event Action ClientStarted;
        public event Action ServerStopped;
        public event Action<string> ConnectionFailed;

        private void Awake()
        {
            _networkManager = GetComponent<NetworkManager>();
            _transport = GetComponent<UnityTransport>();
            _networkManager.OnTransportFailure += HandleTransportFailure;
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            if (_networkManager != null)
            {
                _networkManager.OnTransportFailure -= HandleTransportFailure;
            }

            if (ServiceLocator.TryGet(out ConnectionManager current) && current == this)
            {
                ServiceLocator.Unregister<ConnectionManager>();
            }
        }

        private void HandleTransportFailure()
        {
            string message = _transportRole switch
            {
                TransportRole.Client => "Client transport failed to start.",
                TransportRole.Host => $"Host transport failed. Port {port} may already be in use.",
                _ => "Network transport failed.",
            };

            Debug.LogWarning($"[ConnectionManager] {message}");
            ConnectionFailed?.Invoke(message);
        }

        public void StartHost()
        {
            _transportRole = TransportRole.Host;
            ConfigureTransport();

            if (_networkManager.StartHost())
            {
                HostStarted?.Invoke();
                return;
            }

            ConnectionFailed?.Invoke($"Не удалось запустить Host (порт {port}).");
        }

        public void StartClient()
        {
            if (!ClientConnectInputValidator.TryValidate(address, port, out ClientConnectInputError inputError))
            {
                Debug.LogWarning($"[ConnectionManager] Rejected client connect input: {inputError} ('{address}', port {port}).");
                ConnectionFailed?.Invoke($"Invalid client connect input: {inputError}.");
                return;
            }

            _transportRole = TransportRole.Client;
            ConfigureTransport();

            if (_networkManager.StartClient())
            {
                ClientStarted?.Invoke();
                return;
            }

            ConnectionFailed?.Invoke("Failed to start client.");
        }

        public void Stop()
        {
            if (_networkManager == null)
            {
                _transportRole = TransportRole.None;
                return;
            }

            if (_networkManager.IsListening || _networkManager.IsClient || _networkManager.IsHost)
            {
                _networkManager.Shutdown();
                ServerStopped?.Invoke();
            }

            _transportRole = TransportRole.None;
        }

        public ushort GameplayPort => port;

        public void SetAddress(string newAddress)
        {
            address = string.IsNullOrWhiteSpace(newAddress) ? "127.0.0.1" : newAddress;
        }

        /// <summary>
        /// Хост: задаёт общий игровой порт (слушание и транспорт). Адрес «client» берёт только значение SerializeField до старта — для хостинга вторично.
        /// </summary>
        public void ConfigureForHost(ushort gameplayPort)
        {
            port = gameplayPort;
        }

        /// <summary>
        /// Клиент: куда слать пакеты (IP другой машины, Hamachi‑IP и т.д.).
        /// </summary>
        public void ConfigureForClient(string remoteHostIpv4OrDns, ushort remotePort)
        {
            SetAddress(remoteHostIpv4OrDns);
            port = remotePort;
        }

        private void ConfigureTransport()
        {
            _transport.SetConnectionData(address, port, serverListenAddress);
        }
    }
}
