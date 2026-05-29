using System;
using Catsss.Configs;
using UnityEngine;

namespace Catsss.Player.Aim
{
    /// <summary>
    /// Единая точка расчёта направления броска: луч из центра экрана → мир → вектор от <see cref="PlayerThrowOrigin"/>.
    /// Используется прицелом, превью дуги и ServerRpc (клиент шлёт уже это направление).
    /// </summary>
    public static class PlayerThrowDirectionResolver
    {
        /// <param name="ignoreRoot">Корень игрока — его collider'ы пропускаем (обычно <c>PlayerRoot.transform</c>).</param>
        public static Vector3 Resolve(
            Camera camera,
            Vector3 throwOriginWorld,
            ProjectileSettings settings,
            Transform ignoreRoot = null)
        {
            ProjectileSettings resolved = settings ?? new ProjectileSettings();

            if (camera == null)
            {
                return FallbackForward(ignoreRoot);
            }

            Ray ray = BuildViewportRay(
                camera,
                Mathf.Clamp01(resolved.aimViewportX),
                Mathf.Clamp01(resolved.aimViewportY));
            float maxDistance = Mathf.Max(1f, resolved.aimRayMaxDistance);
            float minDistance = Mathf.Max(0.01f, resolved.aimRayMinDistance);
            LayerMask mask = resolved.aimRayLayerMask;

            Vector3 aimPoint;
            bool hasAimPoint = TryResolveAimPointFromRaycast(
                ray,
                throwOriginWorld,
                maxDistance,
                minDistance,
                mask,
                ignoreRoot,
                resolved.aimSkyAimRayUpThreshold,
                out aimPoint);

            if (!hasAimPoint)
            {
                float fallbackDistance = Mathf.Clamp(
                    resolved.aimRayFallbackDistance,
                    minDistance + 0.1f,
                    maxDistance);
                aimPoint = ResolveAimPointOnFallback(ray, throwOriginWorld, fallbackDistance);
            }

            Vector3 direction = aimPoint - throwOriginWorld;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = ray.direction.sqrMagnitude > 0.0001f
                    ? ray.direction
                    : FallbackForward(ignoreRoot);
            }
            else
            {
                direction.Normalize();
            }

            if (resolved.drawAimDirectionDebug)
            {
                Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.cyan, 0f, false);
                Debug.DrawLine(throwOriginWorld, aimPoint, Color.yellow, 0f, false);
                Debug.DrawRay(throwOriginWorld, direction * 3f, Color.green, 0f, false);
            }

            return direction;
        }

        private static bool TryResolveAimPointFromRaycast(
            Ray ray,
            Vector3 throwOriginWorld,
            float maxDistance,
            float minDistance,
            LayerMask mask,
            Transform ignoreRoot,
            float skyAimRayUpThreshold,
            out Vector3 aimPoint)
        {
            aimPoint = default;
            bool skyAim = ray.direction.y >= skyAimRayUpThreshold;

            // value == 0 → ни один слой; не бросаем RaycastAll без маски.
            if (mask.value == 0)
            {
                return false;
            }

            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, mask, QueryTriggerInteraction.Ignore);

            if (hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];

                if (ShouldIgnoreHit(hit, ignoreRoot))
                {
                    continue;
                }

                if (skyAim && IsHorizontalGroundHit(hit))
                {
                    continue;
                }

                float distFromOrigin = Vector3.Distance(throwOriginWorld, hit.point);

                if (distFromOrigin < minDistance)
                {
                    continue;
                }

                Vector3 originToHit = hit.point - throwOriginWorld;

                if (originToHit.sqrMagnitude > 0.0001f
                    && Vector3.Dot(ray.direction, originToHit.normalized) <= 0.05f)
                {
                    continue;
                }

                aimPoint = hit.point;
                return true;
            }

            return false;
        }

        private static bool ShouldIgnoreHit(RaycastHit hit, Transform ignoreRoot)
        {
            if (ignoreRoot == null || hit.collider == null)
            {
                return false;
            }

            return hit.collider.transform.IsChildOf(ignoreRoot);
        }

        /// <summary>Промах / небо: точка вдоль луча экрана (сохраняет угол вверх).</summary>
        private static Vector3 ResolveAimPointOnFallback(Ray ray, Vector3 throwOriginWorld, float fallbackDistance)
        {
            Vector3 rayDirection = ray.direction.sqrMagnitude > 0.0001f
                ? ray.direction.normalized
                : Vector3.forward;

            return throwOriginWorld + rayDirection * fallbackDistance;
        }

        private static bool IsHorizontalGroundHit(RaycastHit hit)
        {
            return hit.normal.y > 0.85f;
        }

        /// <summary>Луч через viewport: near→far в screen space — совпадает с картинкой после Cinemachine.</summary>
        private static Ray BuildViewportRay(Camera camera, float viewportX, float viewportY)
        {
            Rect rect = camera.pixelRect;
            float screenX = rect.x + rect.width * viewportX;
            float screenY = rect.y + rect.height * viewportY;

            Vector3 nearPoint = camera.ScreenToWorldPoint(new Vector3(screenX, screenY, camera.nearClipPlane));
            Vector3 farPoint = camera.ScreenToWorldPoint(new Vector3(screenX, screenY, camera.farClipPlane));
            Vector3 direction = farPoint - nearPoint;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return camera.ViewportPointToRay(new Vector3(viewportX, viewportY, 0f));
            }

            direction.Normalize();
            return new Ray(nearPoint, direction);
        }

        private static Vector3 FallbackForward(Transform ignoreRoot)
        {
            return ignoreRoot != null ? ignoreRoot.forward : Vector3.forward;
        }
    }
}
