using Catsss.Core.Events;
using Catsss.Core.Services;
using Catsss.Gameplay.Mouse;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;

namespace Catsss.Flow
{
    public sealed class GameFlowManager : NetworkBehaviour
    {
        [SerializeField] private IntEventChannel pylonActivatedChannel;
        [SerializeField] private EmptyEventChannel mouseCaughtChannel;
        [SerializeField] private MouseBrain mouseBrain;
        [SerializeField] private SplineContainer chaseSpline;
        [SerializeField, Min(1)] private int pylonsRequiredToChase = 2;
        [SerializeField] private UnityEvent chaseStarted;
        [SerializeField] private UnityEvent victory;

        private int _activatedPylons;
        private bool _chaseStarted;
        private bool _victoryTriggered;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnEnable()
        {
            if (pylonActivatedChannel != null)
            {
                pylonActivatedChannel.Raised += HandlePylonActivated;
            }

            if (mouseCaughtChannel != null)
            {
                mouseCaughtChannel.Raised += HandleMouseCaught;
            }
        }

        private void OnDisable()
        {
            if (pylonActivatedChannel != null)
            {
                pylonActivatedChannel.Raised -= HandlePylonActivated;
            }

            if (mouseCaughtChannel != null)
            {
                mouseCaughtChannel.Raised -= HandleMouseCaught;
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<GameFlowManager>();
        }

        private void HandlePylonActivated(int pylonId)
        {
            if (!IsServer || _chaseStarted)
            {
                return;
            }

            _activatedPylons++;
            UnlockAbilityForPylon(pylonId);

            if (_activatedPylons >= pylonsRequiredToChase)
            {
                StartChase();
            }
        }

        private void HandleMouseCaught(EmptyEvent value)
        {
            if (!IsServer || _victoryTriggered)
            {
                return;
            }

            _victoryTriggered = true;
            ShowVictoryClientRpc();
        }

        private void UnlockAbilityForPylon(int pylonId)
        {
            string abilityName = pylonId == 1 ? PlayerAbility.Dash : PlayerAbility.DoubleJump;
            PlayerAbilityUnlocker[] unlockers = FindObjectsByType<PlayerAbilityUnlocker>(FindObjectsSortMode.None);

            foreach (PlayerAbilityUnlocker unlocker in unlockers)
            {
                unlocker.UnlockForEveryone(abilityName);
            }
        }

        private void StartChase()
        {
            _chaseStarted = true;

            if (mouseBrain != null && chaseSpline != null)
            {
                mouseBrain.StartChase(chaseSpline);
            }

            StartChaseClientRpc();
        }

        [ClientRpc]
        private void StartChaseClientRpc()
        {
            chaseStarted?.Invoke();
        }

        [ClientRpc]
        private void ShowVictoryClientRpc()
        {
            victory?.Invoke();
        }
    }
}
