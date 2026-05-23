using System.Collections.Generic;
using UnityEngine;

namespace Catsss.Configs.Charge
{
    /// <summary>Базовый каталог ScriptableObject-записей с уникальным byte Id (для NetworkVariable и RPC).</summary>
    public abstract class ByteIdCatalog<T> : ScriptableObject where T : ScriptableObject, IByteIdentifiable
    {
        [SerializeField] private List<T> entries = new();

        private Dictionary<byte, T> _lookup;

        public IReadOnlyList<T> Entries => entries;

        public bool TryGet(byte id, out T entry)
        {
            BuildLookupIfNeeded();

            if (id == 0)
            {
                entry = null;
                return false;
            }

            return _lookup.TryGetValue(id, out entry);
        }

        public T GetOrDefault(byte id)
        {
            return TryGet(id, out T entry) ? entry : null;
        }

        private void BuildLookupIfNeeded()
        {
            if (_lookup != null)
            {
                return;
            }

            _lookup = new Dictionary<byte, T>(entries.Count);

            foreach (T item in entries)
            {
                if (item == null || item.Id == 0)
                {
                    continue;
                }

                if (_lookup.ContainsKey(item.Id))
                {
                    Debug.LogError($"[{name}] Дублирующийся Id={item.Id} у {item.name}.", this);
                    continue;
                }

                _lookup.Add(item.Id, item);
            }
        }

        private void OnValidate()
        {
            _lookup = null;
        }
    }
}
