using System;
using Catsss.Core.Events;
using UnityEngine;

namespace Catsss.Gameplay.Mouse
{
    /// <summary>Событие на конкретной точке MouseRoute: вызов EmptyEventChannel + опциональная пауза.</summary>
    [Serializable]
    public sealed class MouseRouteWaypointEvent
    {
        [Tooltip("Индекс Point_* (Point_0 = 0), на котором срабатывает событие.")]
        [SerializeField, Min(0)] private int waypointIndex;

        [SerializeField] private EmptyEventChannel channel;

        [Tooltip("Пауза движения мыши после вызова channel (сек). 0 = без паузы.")]
        [SerializeField, Min(0f)] private float waitSeconds;

        [Tooltip("Если true — событие срабатывает один раз за проигрывание маршрута.")]
        [SerializeField] private bool playOncePerRouteRun = true;

        public int WaypointIndex => waypointIndex;
        public EmptyEventChannel Channel => channel;
        public float WaitSeconds => waitSeconds;
        public bool PlayOncePerRouteRun => playOncePerRouteRun;
    }
}
