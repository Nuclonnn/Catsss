using Catsss.Core.FSM;

namespace Catsss.Player.States
{
    public abstract class PlayerState : IState
    {
        protected readonly PlayerController Controller;

        protected PlayerState(PlayerController controller)
        {
            Controller = controller;
        }

        public virtual void OnEnter()
        {
        }

        public virtual void OnExit()
        {
        }

        public virtual void OnUpdate()
        {
        }

        public virtual void OnFixedUpdate()
        {
        }
    }

    public sealed class LocomotionState : PlayerState
    {
        public LocomotionState(PlayerController controller) : base(controller)
        {
        }

        public override void OnFixedUpdate()
        {
            Controller.ApplyHorizontalMovement();
            Controller.ApplyVerticalMovement();
        }
    }

    public sealed class JumpState : PlayerState
    {
        public JumpState(PlayerController controller) : base(controller)
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

    public sealed class DashState : PlayerState
    {
        public DashState(PlayerController controller) : base(controller)
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
