namespace Catsss.Player.States
{
    public sealed class NetworkPlayerDashState : NetworkPlayerState
    {
        public NetworkPlayerDashState(NetworkPlayerController controller) : base(controller)
        {
        }

        public override void OnEnter()
        {
            Controller.StartDash();
        }

        public override void OnExit()
        {
            Controller.StopDash();
        }

        public override void OnFixedUpdate()
        {
            Controller.ApplyDashMovement();
        }
    }
}
