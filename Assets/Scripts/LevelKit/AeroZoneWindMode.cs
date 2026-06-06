namespace Catsss.LevelKit
{
    /// <summary>
    /// TargetVelocity — скорость тянется к windDirection * strength.
    /// VerticalEquilibrium — горизонталь как target velocity; вертикаль — пружина к линии равновесия внутри box.
    /// </summary>
    public enum AeroZoneWindMode : byte
    {
        TargetVelocity = 0,
        VerticalEquilibrium = 1,
    }
}
