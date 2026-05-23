using UnityEngine;

public class JumpState : BaseState
{
    public JumpState(PlayerController playerController, Animator animator) : base(playerController, animator){}

    public override void OnEnter(){
        _animator.CrossFade(JumpHash, crossFadeDuration);
        _playerController.StartJump();
    }

    public override void OnFixedUpdate()
    {
        _playerController.HandleMovement();
        _playerController.ApplyVerticalMovement();
    }
}
