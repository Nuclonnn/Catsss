using UnityEngine;

public class CooldownTimer : Timer
{
    public CooldownTimer(float value): base(value){}
    
    public override void Tick(float deltaTime){
        if (IsRunning && Time>0){
            Time -= deltaTime;
        }

        if (IsRunning && Time<=0){
            Stop();
        }
    }

    public bool IsFinished => Time <= 0;

    public void ResetTimer() => Time = initialTime;

    public void ResetTimer(float newTime){
        initialTime = newTime;
        ResetTimer();
    }
}
