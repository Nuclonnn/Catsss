namespace Catsss.Core.Timing
{
    /// <summary>
    /// Секундомер: накапливает время, пока <see cref="Timer.IsRunning"/>.
    /// </summary>
    public sealed class StopwatchTimer : Timer
    {
        private float _elapsed;

        public StopwatchTimer()
        {
            IsRunning = false;
        }

        public float Elapsed => _elapsed;

        public override void Tick(float deltaTime)
        {
            if (!IsRunning)
            {
                return;
            }

            _elapsed += deltaTime;
        }

        /// <summary>Сброс накопленного времени; флаг работы не трогаем.</summary>
        public void ResetElapsed()
        {
            _elapsed = 0f;
        }

        /// <summary>Стоп и сброс.</summary>
        public void Reset()
        {
            Stop();
            _elapsed = 0f;
        }
    }
}
