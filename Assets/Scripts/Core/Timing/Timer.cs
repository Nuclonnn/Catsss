using System;

namespace Catsss.Core.Timing
{
    /// <summary>
    /// Базовый таймер: тик только пока <see cref="IsRunning"/> (старт/стоп как в референсных утилитах).
    /// </summary>
    public abstract class Timer
    {
        public bool IsRunning { get; protected set; }

        public event Action TimerStarted;
        public event Action TimerStopped;

        protected void RaiseStarted() => TimerStarted?.Invoke();
        protected void RaiseStopped() => TimerStopped?.Invoke();

        public void Pause() => IsRunning = false;

        public void Resume() => IsRunning = true;

        public abstract void Tick(float deltaTime);

        public virtual void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            RaiseStopped();
        }
    }
}
