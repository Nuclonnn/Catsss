namespace Catsss.LevelKit
{
    /// <summary>Как печать реагирует на успешную активацию.</summary>
    public enum MagicSealActivationPolicy : byte
    {
        /// <summary>Активна, пока источник удерживает печать; E даёт короткий импульс.</summary>
        Momentary = 0,
        /// <summary>Каждая успешная активация переключает Idle/Pressed.</summary>
        Toggle = 1,
        /// <summary>Первый успешный press вызывает сигнал и переводит печать в Locked.</summary>
        OneShot = 2,
    }
}
