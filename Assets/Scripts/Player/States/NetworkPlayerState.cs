using Catsss.Core.FSM;

namespace Catsss.Player.States
{
    public abstract class NetworkPlayerState : IState
    {
        protected readonly NetworkPlayerController Controller;

        protected NetworkPlayerState(NetworkPlayerController controller)
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
}
