using Catsss.Core.FSM;

namespace Catsss.Menu.Flow
{
    public abstract class AppFlowStateBase : IState
    {
        protected AppFlowStateBase(ApplicationFlowController flow)
        {
            Flow = flow ?? throw new System.ArgumentNullException(nameof(flow));
        }

        protected ApplicationFlowController Flow { get; }

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
