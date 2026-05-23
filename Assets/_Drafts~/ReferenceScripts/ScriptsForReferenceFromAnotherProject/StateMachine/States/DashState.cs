using UnityEngine;

public class DashState : BaseState
{
    public DashState(PlayerController playerController, Animator animator) : base(playerController, animator) {}

    public override void OnEnter()
    {
        _animator.CrossFade(DashHash, crossFadeDuration);
        _playerController.StartDash();
    }

    public override void OnExit()
    {
        _playerController.StopDash();
    }

    public override void OnFixedUpdate()
    {
        _playerController.HandleDashMovement();
        _playerController.ApplyDashVerticalMovement();
        _playerController.DrainDashStamina();
    }
}
