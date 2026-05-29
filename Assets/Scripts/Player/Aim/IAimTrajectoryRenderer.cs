using UnityEngine;

namespace Catsss.Player.Aim
{
    /// <summary>Визуал дуги прицеливания (логика Aim не зависит от реализации).</summary>
    public interface IAimTrajectoryRenderer
    {
        void Show();

        void Hide();

        /// <param name="lineStart">Начало LineRenderer (обычно <see cref="PlayerThrowOrigin"/>).</param>
        /// <param name="flightOrigin">Старт симуляции полёта (lineStart + spawnForwardOffset).</param>
        void UpdateTrajectory(Vector3 lineStart, Vector3 flightOrigin, Vector3 direction, Transform target);
    }
}
