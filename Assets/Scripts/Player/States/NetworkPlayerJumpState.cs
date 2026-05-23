namespace Catsss.Player.States
{
    public sealed class NetworkPlayerJumpState : NetworkPlayerState
    {
        public NetworkPlayerJumpState(NetworkPlayerController controller) : base(controller)
        {
        }

        public override void OnEnter()
        {
            Controller.StartJump();
        }

        public override void OnFixedUpdate()
        {
            Controller.ApplyHorizontalMovement();
            Controller.ApplyVerticalMovement();
        }
    }
}
