using Catsss.Configs;
using UnityEngine;

namespace Catsss.Player
{
    public sealed class PlayerVisuals : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerNetwork playerNetwork;
        [SerializeField] private PlayerChargeHolder chargeHolder;
        [SerializeField] private ChargeDatabase chargeDatabase;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform vfxParent;
        [SerializeField] private Light chargeLight;

        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int DashHash = Animator.StringToHash("Dash");
        private static readonly int HasChargeHash = Animator.StringToHash("HasCharge");

        private GameObject _currentChargeVfx;

        private void OnEnable()
        {
            if (playerController != null)
            {
                playerController.Jumped += PlayJump;
                playerController.Dashed += PlayDash;
            }

            if (playerNetwork != null)
            {
                playerNetwork.RemoteJumped += PlayJump;
                playerNetwork.RemoteDashed += PlayDash;
            }

            if (chargeHolder != null)
            {
                chargeHolder.ChargeChanged += HandleChargeChanged;
            }
        }

        private void OnDisable()
        {
            if (playerController != null)
            {
                playerController.Jumped -= PlayJump;
                playerController.Dashed -= PlayDash;
            }

            if (playerNetwork != null)
            {
                playerNetwork.RemoteJumped -= PlayJump;
                playerNetwork.RemoteDashed -= PlayDash;
            }

            if (chargeHolder != null)
            {
                chargeHolder.ChargeChanged -= HandleChargeChanged;
            }
        }

        private void PlayJump()
        {
            if (animator != null)
            {
                animator.SetTrigger(JumpHash);
            }
        }

        private void PlayDash()
        {
            if (animator != null)
            {
                animator.SetTrigger(DashHash);
            }
        }

        private void HandleChargeChanged(int chargeId)
        {
            if (_currentChargeVfx != null)
            {
                Destroy(_currentChargeVfx);
            }

            ChargeType chargeType = chargeDatabase != null ? chargeDatabase.GetById(chargeId) : null;

            if (animator != null)
            {
                animator.SetBool(HasChargeHash, chargeType != null);
            }

            if (chargeLight != null)
            {
                chargeLight.enabled = chargeType != null;
                if (chargeType != null)
                {
                    chargeLight.color = chargeType.LightColor;
                }
            }

            if (chargeType != null && chargeType.VfxPrefab != null)
            {
                Transform parent = vfxParent != null ? vfxParent : transform;
                _currentChargeVfx = Instantiate(chargeType.VfxPrefab, parent);
                _currentChargeVfx.transform.localPosition = Vector3.zero;
                _currentChargeVfx.transform.localRotation = Quaternion.identity;
            }
        }
    }
}
