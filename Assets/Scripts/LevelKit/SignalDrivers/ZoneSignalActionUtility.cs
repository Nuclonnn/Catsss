namespace Catsss.LevelKit
{
    /// <summary>Enable / Disable / Toggle для zone signal drivers.</summary>
    internal static class ZoneSignalActionUtility
    {
        public static bool Apply(bool current, AeroZoneSignalAction action)
        {
            return action switch
            {
                AeroZoneSignalAction.Enable => true,
                AeroZoneSignalAction.Disable => false,
                AeroZoneSignalAction.Toggle => !current,
                _ => current,
            };
        }

        public static bool Apply(bool current, AntiMagicZoneSignalAction action)
        {
            return action switch
            {
                AntiMagicZoneSignalAction.Enable => true,
                AntiMagicZoneSignalAction.Disable => false,
                AntiMagicZoneSignalAction.Toggle => !current,
                _ => current,
            };
        }
    }
}
