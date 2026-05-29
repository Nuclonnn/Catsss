using Catsss.Player.Aim;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Player
{
    /// <summary>
    /// Устарел после Stage 4.1 — отключи компонент на префабе.
    /// Используй <see cref="PlayerAimDebugLog"/> + <see cref="PlayerAimController"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerProjectileInputSmokeTest : NetworkBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerAimController aimController;

        private void Reset()
        {
            inputReader = GetComponent<PlayerInputReader>();
            aimController = GetComponent<PlayerAimController>();
        }

        private void Update()
        {
            if (!IsOwner || aimController != null)
            {
                return;
            }

            if (inputReader.ConsumeAimToggledThisFrame())
            {
                Debug.LogWarning("[Stage4.0] SmokeTest без PlayerAimController — добавь Aim или отключи этот компонент.", this);
            }
        }
    }
}
