using System.Collections.Generic;
using UnityEngine;

namespace Catsss.Configs
{
    [CreateAssetMenu(menuName = "Catsss/Configs/Charge Database")]
    public sealed class ChargeDatabase : ScriptableObject
    {
        [SerializeField] private ChargeType[] chargeTypes;
        private Dictionary<int, ChargeType> _byId;

        public ChargeType GetById(int id)
        {
            BuildCacheIfNeeded();
            return _byId.TryGetValue(id, out ChargeType chargeType) ? chargeType : null;
        }

        private void BuildCacheIfNeeded()
        {
            if (_byId != null)
            {
                return;
            }

            _byId = new Dictionary<int, ChargeType>();

            if (chargeTypes == null)
            {
                return;
            }

            foreach (ChargeType chargeType in chargeTypes)
            {
                if (chargeType != null)
                {
                    _byId[chargeType.Id] = chargeType;
                }
            }
        }
    }
}
