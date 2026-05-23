using Catsss.Trials;
using UnityEngine;

namespace Catsss.Core.Events
{
    [CreateAssetMenu(menuName = "Catsss/Events/Trial Progress Event Channel")]
    public sealed class TrialProgressEventChannel : EventChannel<TrialProgressEvent>
    {
    }
}
