using UnityEngine;

namespace Catsss.Core.Events
{
    [CreateAssetMenu(menuName = "Catsss/Events/Empty Event Channel")]
    public sealed class EmptyEventChannel : EventChannel<EmptyEvent>
    {
        public void Invoke()
        {
            Invoke(new EmptyEvent());
        }
    }
}
