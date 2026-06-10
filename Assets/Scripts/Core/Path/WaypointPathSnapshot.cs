using System;
using System.Collections.Generic;
using UnityEngine;

namespace Catsss.Core.Path
{
    /// <summary>Снимок world-позиций дочерних точек Path (Point_0, Point_1...).</summary>
    public static class WaypointPathSnapshot
    {
        public static Vector3[] CollectFromPathRoot(Transform pathRoot)
        {
            if (pathRoot == null)
            {
                return Array.Empty<Vector3>();
            }

            int count = pathRoot.childCount;

            if (count == 0)
            {
                return Array.Empty<Vector3>();
            }

            Vector3[] points = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                Transform child = pathRoot.GetChild(i);
                points[i] = child != null ? child.position : pathRoot.position;
            }

            return points;
        }

        public static int FindClosestWaypointIndex(IReadOnlyList<Vector3> points, Vector3 worldPosition)
        {
            if (points == null || points.Count == 0)
            {
                return 0;
            }

            int bestIndex = 0;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < points.Count; i++)
            {
                float distance = Vector3.SqrMagnitude(points[i] - worldPosition);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }
    }
}
