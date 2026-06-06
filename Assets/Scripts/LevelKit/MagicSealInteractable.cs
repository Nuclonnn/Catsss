using Catsss.Interaction;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.LevelKit
{
    /// <summary>Источник активации MagicSeal через E и существующую систему Interaction/World Hints.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MagicSeal))]
    public sealed class MagicSealInteractable : NetworkBehaviour, IInteractable, IInteractableViewHost
    {
        [Header("Interaction")]
        [SerializeField] private MagicSeal seal;
        [SerializeField] private Transform promptAnchor;
        [SerializeField] private InteractionPromptSettings promptSettings = new();
        [SerializeField] private InteractableHighlightStub highlightStub;

        public Transform PromptAnchor => promptAnchor != null ? promptAnchor : transform;
        public InteractionPromptSettings PromptSettings => promptSettings;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        public bool CanInteract(NetworkPlayerController interactor)
        {
            if (seal == null || !IsSpawned)
            {
                return false;
            }

            return seal.State != MagicSealState.Disabled && seal.State != MagicSealState.Locked;
        }

        public void RequestInteract(NetworkPlayerController interactor)
        {
            if (interactor == null || !interactor.IsOwner || interactor.NetworkObject == null)
            {
                return;
            }

            if (NetworkObject == null || !NetworkObject.IsSpawned)
            {
                Debug.LogWarning("[MagicSealInteractable] MagicSeal NetworkObject ещё не spawned.", this);
                return;
            }

            RequestInteractServerRpc(interactor.NetworkObject.NetworkObjectId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestInteractServerRpc(ulong interactorNetworkObjectId)
        {
            if (!TryResolveInteractor(interactorNetworkObjectId, out NetworkPlayerController interactor))
            {
                return;
            }

            if (seal == null)
            {
                seal = GetComponent<MagicSeal>();
            }

            seal?.PressPulseServer(interactor);
        }

        private static bool TryResolveInteractor(
            ulong interactorNetworkObjectId,
            out NetworkPlayerController interactor)
        {
            interactor = null;
            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager == null
                || !networkManager.SpawnManager.SpawnedObjects.TryGetValue(
                    interactorNetworkObjectId,
                    out NetworkObject playerObject))
            {
                return false;
            }

            return playerObject.TryGetComponent(out interactor);
        }

        private void ResolveReferences()
        {
            if (seal == null)
            {
                seal = GetComponent<MagicSeal>();
            }

            if (highlightStub == null)
            {
                highlightStub = GetComponentInChildren<InteractableHighlightStub>(true);
            }
        }

        InteractableHighlightStub IInteractableViewHost.HighlightStub => highlightStub;
    }
}
