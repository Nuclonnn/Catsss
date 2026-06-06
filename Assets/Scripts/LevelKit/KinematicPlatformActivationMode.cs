namespace Catsss.LevelKit
{
    /// <summary>Как платформа начинает и поддерживает движение.</summary>
    public enum KinematicPlatformActivationMode : byte
    {
        /// <summary>Постоянный цикл по waypoints без внешних сигналов.</summary>
        Cycle = 0,
        /// <summary>Управление через EmptyEventChannel от MagicSeal или других систем.</summary>
        SignalDriven = 1,
        /// <summary>Едет, пока на trigger-зоне есть подходящий игрок/заряд.</summary>
        Resonance = 2,
    }
}
