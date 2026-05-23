namespace Catsss.Player.States
{
    public sealed class NetworkPlayerLocomotionState : NetworkPlayerState
    {
        public NetworkPlayerLocomotionState(NetworkPlayerController controller) : base(controller)
        {
        }

        public override void OnFixedUpdate()
        {
            Controller.ApplyHorizontalMovement();
            Controller.ApplyVerticalMovement();
        }
    }
}
