using UnityEngine;

namespace Catsss.Core.Timing
{
    /// <summary>
    /// Пока <paramref name="renewCondition"/> истинно, окно остаётся полным; иначе убывает.
    /// Паттерн coyote time после схода с земли.
    /// </summary>
    public sealed class GracePeriodTimer
    {
        private float _remaining;

        public float Remaining => _remaining;

        public bool HasGrace => _remaining > 0f;

        public void Tick(float deltaTime, bool renewCondition, float fullDuration)
        {
            float full = Mathf.Max(0f, fullDuration);

            if (renewCondition)
            {
                _remaining = full;
            }
            else
            {
                _remaining = Mathf.Max(0f, _remaining - deltaTime);
            }
        }

        public void Clear()
        {
            _remaining = 0f;
        }
    }
}
