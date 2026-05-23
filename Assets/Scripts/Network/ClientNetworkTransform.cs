using Unity.Netcode.Components;

namespace Catsss.Network
{
    public sealed class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
