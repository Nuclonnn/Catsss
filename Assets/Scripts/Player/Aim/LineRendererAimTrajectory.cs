using Catsss.Gameplay.Charges.Projectile;

using Catsss.Configs;

using UnityEngine;
using UnityEngine.Rendering;



namespace Catsss.Player.Aim

{

    /// <summary>Дуга прицеливания через <see cref="LineRenderer"/>.</summary>

    [DisallowMultipleComponent]

    public sealed class LineRendererAimTrajectory : MonoBehaviour, IAimTrajectoryRenderer

    {

        [SerializeField] private GameConfig gameConfig;

        [SerializeField] private LineRenderer lineRenderer;



        [Header("LineRenderer")]

        [Tooltip("Первая точка = ThrowOrigin, далее симуляция с точки спавна снаряда.")]

        [SerializeField] private bool includeThrowOriginSegment = true;



        [SerializeField] private bool applyRecommendedLineSettings = true;



        [SerializeField, Min(0.01f)] private float startWidth = 0.12f;



        [SerializeField, Min(0.01f)] private float endWidth = 0.04f;



        private void Reset()

        {

            lineRenderer = GetComponent<LineRenderer>();



            if (lineRenderer == null)

            {

                lineRenderer = GetComponentInChildren<LineRenderer>(true);

            }

        }



        private void Awake()

        {

            if (applyRecommendedLineSettings)

            {

                ApplyRecommendedLineSettings();

            }



            Hide();

        }



        public void Show()

        {

            if (lineRenderer != null)

            {

                lineRenderer.enabled = true;

            }

        }



        public void Hide()

        {

            if (lineRenderer == null)

            {

                return;

            }



            lineRenderer.enabled = false;

            lineRenderer.positionCount = 0;

        }



        public void UpdateTrajectory(Vector3 lineStart, Vector3 flightOrigin, Vector3 direction, Transform target)

        {

            if (lineRenderer == null)

            {

                return;

            }



            ProjectileSettings settings = ResolveProjectileSettings();



            Vector3? targetPosition = target != null ? target.position : null;



            System.Collections.Generic.IReadOnlyList<Vector3> simulated = ProjectileTrajectorySimulator.Simulate(

                flightOrigin,

                direction,

                targetPosition,

                settings);



            int simulatedCount = simulated.Count;



            if (simulatedCount <= 0)

            {

                lineRenderer.positionCount = 0;

                return;

            }



            bool prependOrigin = includeThrowOriginSegment

                && (lineStart - simulated[0]).sqrMagnitude > 0.0001f;



            int totalCount = simulatedCount + (prependOrigin ? 1 : 0);

            lineRenderer.positionCount = totalCount;



            int writeIndex = 0;



            if (prependOrigin)

            {

                lineRenderer.SetPosition(writeIndex++, lineStart);

            }



            for (int i = 0; i < simulatedCount; i++)

            {

                lineRenderer.SetPosition(writeIndex++, simulated[i]);

            }

        }



        private void ApplyRecommendedLineSettings()

        {

            if (lineRenderer == null)

            {

                return;

            }



            lineRenderer.useWorldSpace = true;

            lineRenderer.shadowCastingMode = ShadowCastingMode.Off;

            lineRenderer.receiveShadows = false;

            lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            lineRenderer.alignment = LineAlignment.View;

            lineRenderer.textureMode = LineTextureMode.Stretch;

            lineRenderer.numCornerVertices = 6;

            lineRenderer.numCapVertices = 4;

            lineRenderer.widthMultiplier = 1f;

            lineRenderer.widthCurve = BuildWidthCurve(startWidth, endWidth);



            Gradient gradient = new();

            GradientColorKey[] colorKeys =

            {

                new(Color.white, 0f),

                new(new Color(0.75f, 0.9f, 1f), 1f),

            };

            GradientAlphaKey[] alphaKeys =

            {

                new(1f, 0f),

                new(0.35f, 1f),

            };

            gradient.SetKeys(colorKeys, alphaKeys);

            lineRenderer.colorGradient = gradient;

        }



        private static AnimationCurve BuildWidthCurve(float start, float end)

        {

            Keyframe startKeyframe = new(0f, start);

            Keyframe endKeyframe = new(1f, end);

            AnimationCurve curve = new(startKeyframe, endKeyframe);

            curve.SmoothTangents(0, 0f);
            curve.SmoothTangents(1, 0f);

            return curve;

        }



        private ProjectileSettings ResolveProjectileSettings()

        {

            if (gameConfig != null && gameConfig.Projectile != null)

            {

                return gameConfig.Projectile;

            }



            return new ProjectileSettings();

        }

    }

}


