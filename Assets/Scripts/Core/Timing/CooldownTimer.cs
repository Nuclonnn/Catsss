using UnityEngine;

namespace Catsss.Core.Timing
{
    /// <summary>
    /// Обратный отсчёт: после <see cref="Restart"/> тикает до нуля и вызывает <see cref="Timer.Stop"/>.
    /// Подходит для кулдаунов, буфера прыжка, длительности дэша (тиковать с <see cref="Time.fixedDeltaTime"/> там, где нужна физика).
    /// </summary>
    public sealed class CooldownTimer : Timer
    {
        private float _initialDuration;
        private float _remaining;

        public CooldownTimer(float initialDuration = 0f)
        {
            _initialDuration = Mathf.Max(0f, initialDuration);
            _remaining = _initialDuration;
            IsRunning = false;
        }

        /// <summary>Исходная длительность последнего <see cref="Restart"/>.</summary>
        public float InitialDuration => _initialDuration;

        /// <summary>Оставшееся время до нуля.</summary>
        public float Remaining => _remaining;

        /// <summary>Доля оставшегося времени [0–1]; при нулевой длине возвращает 0.</summary>
        public float NormalizedRemaining =>
            _initialDuration > Mathf.Epsilon ? Mathf.Clamp01(_remaining / _initialDuration) : 0f;

        public override void Tick(float deltaTime)
        {
            if (!IsRunning || _remaining <= 0f)
            {
                return;
            }

            _remaining -= deltaTime;
            if (_remaining <= 0f)
            {
                _remaining = 0f;
                Stop();
            }
        }

        /// <summary>Задать новую длительность и включить отсчёт (если длительность &gt; 0).</summary>
        public void Restart(float duration)
        {
            _initialDuration = Mathf.Max(0f, duration);
            _remaining = _initialDuration;

            if (_remaining <= 0f)
            {
                if (IsRunning)
                {
                    Stop();
                }

                return;
            }

            if (!IsRunning)
            {
                IsRunning = true;
                RaiseStarted();
                return;
            }
        }

        /// <summary>Мгновенно обнуляет оставшееся время и останавливает таймер.</summary>
        public void Clear()
        {
            if (!IsRunning && _remaining <= 0f)
            {
                return;
            }

            _remaining = 0f;
            Stop();
        }
    }
}
