using UnityEngine;

public class StopwatchTimer : Timer
{

    public StopwatchTimer() : base(0) { }

    public override void Tick(float deltaTime)
    {
        if (IsRunning)
        {
            Time += deltaTime;
        }
    }

    public void ResetTimer() => Time = 0;

    public float GetTime() => Time;
}

