using Catsss.Player;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Trials
{
    /// <summary>Триггер финиша: игрок с зарядом нужного trialId завершает испытание (только сервер).</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class TrialFinishZone : NetworkBehaviour
    {
        [SerializeField] private TrialDefinition trial;

        private TrialSessionRegistry Registry =>
            Catsss.Core.Services.ServiceLocator.TryGet(out TrialSessionRegistry registry) ? registry : null;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer)
            {
                return;
            }

            PlayerChargeController chargeController = other.GetComponentInParent<PlayerChargeController>();

            if (chargeController == null)
            {
                return;
            }

            if (trial == null || !trial.IsValid)
            {
                Debug.LogWarning("[TrialFinishZone] Не назначен или невалиден Trial Definition.", this);
                return;
            }

            if (chargeController.ChargeId == 0 || chargeController.ActiveTrialId != trial.TrialId)
            {
                return;
            }

            TrialSessionRegistry registry = Registry;

            if (registry == null)
            {
                Debug.LogWarning("[TrialFinishZone] TrialSessionRegistry не найден.", this);
                return;
            }

            registry.CompleteTrial(trial, chargeController);
        }
    }
}
