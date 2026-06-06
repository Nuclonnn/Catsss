using Unity.Netcode.Components;

namespace Catsss.Network
{
    /// <summary>Server-authoritative синхронизация transform для платформ и прочих level-kit объектов.</summary>
    public sealed class ServerNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return true;
        }
    }
}
