using System;
using UnityEngine;

namespace Catsss.LevelKit
{
    [Serializable]
    public struct AntiMagicZoneSignalBinding
    {
        public AntiMagicZoneSignalAction zoneAction;
        public AntiMagicZoneSignalAction projectileBlockAction;
    }
}
