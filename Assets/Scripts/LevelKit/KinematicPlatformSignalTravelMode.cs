namespace Catsss.LevelKit
{
    /// <summary>Как Signal-платформа проходит waypoints после команды от кнопки.</summary>
    public enum KinematicPlatformSignalTravelMode : byte
    {
        /// <summary>До конца траектории в текущем направлении и остановка.</summary>
        OneWay = 0,
        /// <summary>Разворот на концах пути, пока не придёт Stop или новая команда.</summary>
        Yoyo = 1,
        /// <summary>Замыкание path в кольцо, пока не придёт Stop или новая команда.</summary>
        Loop = 2,
    }
}
