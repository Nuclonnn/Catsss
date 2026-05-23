using UnityEngine;
public class PlayerAnimationController
    {
        readonly Animator animator;
        float fallThreshold;

        static readonly int Speed = Animator.StringToHash("Speed");
        static readonly int JumpTrigger = Animator.StringToHash("Jump");
        static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        static readonly int IsFalling = Animator.StringToHash("IsFalling");
        static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");

        public PlayerAnimationController(Animator animator, float fallThreshold)
        {
            this.animator = animator;
            this.fallThreshold = fallThreshold;
        }

        public void SetFallThreshold(float value)
        {
            fallThreshold = value;
        }

        public void TriggerJump()
        {
            animator.SetTrigger(JumpTrigger);
        }

        public void Update(float speed, bool isGrounded, float verticalVelocity)
        {
            animator.SetFloat(Speed, speed);
            animator.SetBool(IsGrounded, isGrounded);
            animator.SetFloat(VerticalVelocity, verticalVelocity);

            bool isFalling = !isGrounded && verticalVelocity < fallThreshold;
            animator.SetBool(IsFalling, isFalling);
        }
    }
