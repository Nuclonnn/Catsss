namespace Catsss.Gameplay.Mouse
{
    /// <summary>Что делать с мышью после прохождения маршрута.</summary>
    public enum MouseRouteEndMode : byte
    {
        StopAtEnd = 0,
        ReturnHidden = 1,
        EnterDome = 2,
    }
}
