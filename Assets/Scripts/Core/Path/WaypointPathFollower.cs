using System;
using System.Collections.Generic;
using UnityEngine;

namespace Catsss.Core.Path
{
    /// <summary>One-way движение по waypoint'ам. Используется мышью; позже можно переиспользовать в KinematicPlatform.</summary>
    public sealed class WaypointPathFollower
    {
        private Vector3[] _points = Array.Empty<Vector3>();
        private int _fromIndex;
        private float _segmentProgress;
        private bool _isComplete;

        public bool IsComplete => _isComplete;
        public int FromIndex => _fromIndex;
        public int ToIndex => Mathf.Min(_fromIndex + 1, Mathf.Max(0, _points.Length - 1));
        public IReadOnlyList<Vector3> Points => _points;

        public void Begin(Vector3[] points, int startIndex = 0)
        {
            _points = points ?? Array.Empty<Vector3>();
            _fromIndex = _points.Length == 0 ? 0 : Mathf.Clamp(startIndex, 0, _points.Length - 1);
            _segmentProgress = 0f;
            _isComplete = _points.Length < 2 || _fromIndex >= _points.Length - 1;
        }

        /// <summary>
        /// Шаг симуляции. Возвращает true, если в этом кадре достигнута промежуточная точка.
        /// reachedWaypointIndex = индекс достигнутой точки или -1.
        /// </summary>
        public bool TryAdvance(float deltaTime, float unitsPerSecond, out Vector3 position, out int reachedWaypointIndex)
        {
            reachedWaypointIndex = -1;

            if (_points.Length == 0)
            {
                position = Vector3.zero;
                _isComplete = true;
                return false;
            }

            position = _points[_fromIndex];

            if (_isComplete)
            {
                return false;
            }

            int toIndex = _fromIndex + 1;

            if (toIndex >= _points.Length)
            {
                _isComplete = true;
                position = _points[^1];
                return false;
            }

            float segmentLength = Vector3.Distance(_points[_fromIndex], _points[toIndex]);

            if (segmentLength < 0.001f)
            {
                _fromIndex = toIndex;
                reachedWaypointIndex = toIndex;
                position = _points[_fromIndex];

                if (_fromIndex >= _points.Length - 1)
                {
                    _isComplete = true;
                }

                return true;
            }

            _segmentProgress += (unitsPerSecond * deltaTime) / segmentLength;

            while (_segmentProgress >= 1f)
            {
                _segmentProgress -= 1f;
                _fromIndex = toIndex;
                reachedWaypointIndex = toIndex;
                toIndex = _fromIndex + 1;

                if (toIndex >= _points.Length)
                {
                    _isComplete = true;
                    position = _points[^1];
                    return true;
                }

                segmentLength = Vector3.Distance(_points[_fromIndex], _points[toIndex]);

                if (segmentLength < 0.001f)
                {
                    continue;
                }

                break;
            }

            if (_isComplete)
            {
                return reachedWaypointIndex >= 0;
            }

            float eased = SmoothStep(Mathf.Clamp01(_segmentProgress));
            position = Vector3.LerpUnclamped(_points[_fromIndex], _points[toIndex], eased);
            return reachedWaypointIndex >= 0;
        }

        private static float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}
