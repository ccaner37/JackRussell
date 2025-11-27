using JackRussell;
using UnityEngine;

namespace JackRussell.States.Locomotion
{
    /// <summary>
    /// Crouch state: reduces movement speed and changes animator flag.
    /// Exits when crouch input is released or when the player jumps.
    /// </summary>
    public class CrouchState : PlayerStateBase
    {
        private const float k_CrouchSpeedMultiplier = 0.5f;

        public CrouchState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override string Name => nameof(CrouchState);

        public override LocomotionType LocomotionType => LocomotionType.Crouch;

        public override void Enter()
        {
            _player.Animator.SetBool(Animator.StringToHash("IsCrouching"), true);
        }

        public override void Exit(IState nextState = null)
        {
            _player.Animator.SetBool(Animator.StringToHash("IsCrouching"), false);
        }

        public override void LogicUpdate()
        {
            // If jump requested while crouching, do a normal jump (or stand-jump)
            if (_player.ConsumeJumpRequest() && _player.IsGrounded)
            {
                Vector3 v = _player.Rigidbody.linearVelocity;
                v.y = _player.JumpVelocity;
                _player.SetVelocityImmediate(v);
                ChangeState(new JumpState(_player, _stateMachine));
                return;
            }

            // If crouch input released, go to Move or Idle
            // We don't have a dedicated crouch input exposed; treat SprintRequested as placeholder for toggling in future.
            if (!_player.SprintRequested)
            {
                if (_player.MoveDirection.sqrMagnitude > 0.001f)
                    ChangeState(new MoveState(_player, _stateMachine));
                else
                    ChangeState(new IdleState(_player, _stateMachine));
            }
        }

        public override void PhysicsUpdate()
        {
            // Apply reduced movement while crouching
            Vector3 desired = _player.MoveDirection;
            float targetSpeed = _player.SprintRequested ? _player.RunSpeed * k_CrouchSpeedMultiplier : _player.WalkSpeed * k_CrouchSpeedMultiplier;
            Vector3 desiredVel = desired * targetSpeed;

            Vector3 horizontalVel = new Vector3(_player.Rigidbody.linearVelocity.x, 0f, _player.Rigidbody.linearVelocity.z);
            Vector3 velocityDiff = desiredVel - horizontalVel;

            _player.AddGroundForce(velocityDiff * _player.AccelGround);
            _player.ClampHorizontalSpeed(targetSpeed);
        }
    }
}
