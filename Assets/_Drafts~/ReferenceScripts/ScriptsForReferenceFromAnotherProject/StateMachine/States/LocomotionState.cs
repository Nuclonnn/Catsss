using UnityEngine;

public class LocomotionState : BaseState
{
    public LocomotionState(PlayerController playerController, Animator animator) : base(playerController, animator){}

    public override void OnEnter(){
        _animator.CrossFade(LocomotionHash, crossFadeDuration);
    }

    public override void OnFixedUpdate()
    {
        _playerController.HandleMovement();
        _playerController.ApplyVerticalMovement();
    }
}
