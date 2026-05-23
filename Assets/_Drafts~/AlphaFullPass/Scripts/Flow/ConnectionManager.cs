using System;
using Catsss.Core.Services;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Catsss.Flow
{
    [RequireComponent(typeof(NetworkManager))]
    public sealed class ConnectionManager : MonoBehaviour
    {
        [SerializeField] private string localAddress = "127.0.0.1";
        [SerializeField] private ushort port = 7777;

        private NetworkManager _networkManager;

        public event Action<string> HostStarted;
        public event Action ClientStarted;
        public event Action<string> ConnectionFailed;

        private void Awake()
        {
            _networkManager = GetComponent<NetworkManager>();
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<ConnectionManager>();
        }

        public void StartHost()
        {
            try
            {
                ConfigureTransport(localAddress);
                bool started = _networkManager.StartHost();

                if (started)
                {
                    HostStarted?.Invoke($"{localAddress}:{port}");
                }
                else
                {
                    ConnectionFailed?.Invoke("NetworkManager failed to start host.");
                }
            }
            catch (Exception exception)
            {
                ConnectionFailed?.Invoke(exception.Message);
                Debug.LogException(exception, this);
            }
        }

        public void StartClient()
        {
            try
            {
                ConfigureTransport(localAddress);
                bool started = _networkManager.StartClient();

                if (started)
                {
                    ClientStarted?.Invoke();
                }
                else
                {
                    ConnectionFailed?.Invoke("NetworkManager failed to start client.");
                }
            }
            catch (Exception exception)
            {
                ConnectionFailed?.Invoke(exception.Message);
                Debug.LogException(exception, this);
            }
        }

        public void SetAddress(string address)
        {
            localAddress = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address;
        }

        public void StartHostWithRelay()
        {
            Debug.LogWarning("Relay is temporarily disabled. Starting a local host instead.", this);
            StartHost();
        }

        public void StartClientWithRelay(string joinCodeOrAddress)
        {
            if (!string.IsNullOrWhiteSpace(joinCodeOrAddress))
            {
                SetAddress(joinCodeOrAddress);
            }

            Debug.LogWarning("Relay is temporarily disabled. Treating the entered value as a direct address.", this);
            StartClient();
        }

        private void ConfigureTransport(string address)
        {
            UnityTransport transport = _networkManager.GetComponent<UnityTransport>();
            if (transport == null)
            {
                throw new InvalidOperationException("UnityTransport is required on NetworkManager for local connections.");
            }

            transport.SetConnectionData(address, port);
        }
    }
}
