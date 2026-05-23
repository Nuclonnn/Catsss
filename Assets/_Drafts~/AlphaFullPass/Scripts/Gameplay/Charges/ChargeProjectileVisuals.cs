using UnityEngine;

namespace Catsss.Gameplay.Charges
{
    public sealed class ChargeProjectileVisuals : MonoBehaviour
    {
        [SerializeField] private HomingChargeProjectile projectile;
        [SerializeField] private GameObject fizzleVfxPrefab;

        private void OnEnable()
        {
            if (projectile != null)
            {
                projectile.Fizzled += PlayFizzle;
            }
        }

        private void OnDisable()
        {
            if (projectile != null)
            {
                projectile.Fizzled -= PlayFizzle;
            }
        }

        private void PlayFizzle(Vector3 position)
        {
            if (fizzleVfxPrefab != null)
            {
                Instantiate(fizzleVfxPrefab, position, Quaternion.identity);
            }
        }
    }
}
