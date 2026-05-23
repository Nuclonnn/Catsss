using System;
using UnityEngine;

namespace Catsss.Configs
{
    [Serializable]
    public sealed class PhysicsSettings
    {
        [Min(0f)] public float customGravityMultiplier = 2.5f;
        [Min(0f)] public float fallRespawnY = -10f;
    }
}
