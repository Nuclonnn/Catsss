using System.Collections;
using Catsss.Gameplay.Mouse;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Dev
{
    /// <summary>
    /// Dev-only сценарии мыши. Вешать на sandbox-сцену; не использовать на финальных уровнях.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MouseDebugBootstrap : MonoBehaviour
    {
        [SerializeField] private MouseBrain mouse;

        [Header("Presence")]
        [SerializeField] private bool startSpectralVisibleForDebug;

        [Header("Route")]
        [SerializeField] private bool playDebugRouteOnSpawn;
        [SerializeField] private MouseRoute debugRouteOnSpawn;

        [Header("Dome")]
        [SerializeField] private bool enterDomeOnSpawnForDebug;

        private bool _applied;

        private void Reset()
        {
            if (mouse == null)
            {
                mouse = FindAnyObjectByType<MouseBrain>();
            }
        }

        private void OnEnable()
        {
            _applied = false;
            StartCoroutine(ApplyWhenReady());
        }

        private IEnumerator ApplyWhenReady()
        {
            while (!_applied)
            {
                if (TryApplyDebugServer())
                {
                    _applied = true;
                    yield break;
                }

                yield return null;
            }
        }

        private bool TryApplyDebugServer()
        {
            if (!IsServerAuthority() || mouse == null)
            {
                return false;
            }

            NetworkObject networkObject = mouse.NetworkObject;

            if (networkObject == null || !networkObject.IsSpawned)
            {
                return false;
            }

            if (startSpectralVisibleForDebug)
            {
                mouse.SetPresenceModeServer(MousePresenceMode.Spectral);
            }

            if (playDebugRouteOnSpawn && debugRouteOnSpawn != null)
            {
                mouse.PlayRouteServer(debugRouteOnSpawn, teleportToRouteStart: true);
            }
            else if (enterDomeOnSpawnForDebug)
            {
                mouse.EnterDomeServer();
            }

            return true;
        }

        private static bool IsServerAuthority()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            return networkManager != null && networkManager.IsServer;
        }
    }
}
