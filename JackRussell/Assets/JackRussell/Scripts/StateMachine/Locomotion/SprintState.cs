using JackRussell;
using UnityEngine;

namespace JackRussell.States.Locomotion
{
    public class SprintState : PlayerStateBase
    {
        public SprintState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override string Name => nameof(SprintState);

        public override void Enter()
        {
            // Set animator sprint flag if desired
            _player.Animator.SetBool(Animator.StringToHash("IsSprinting"), true);
        }

        public override void Exit()
        {
            _player.Animator.SetBool(Animator.StringToHash("IsSprinting"), false);
        }

        public override void LogicUpdate()
        {
            // If sprint is released or no move input, go back to Move or Idle
            if (!_player.SprintRequested)
            {
                if (_player.MoveDirection.sqrMagnitude > 0.001f)
                    ChangeState(new MoveState(_player, _stateMachine));
                else
                    ChangeState(new IdleState(_player, _stateMachine));
                return;
            }

            // Jump
            if (_player.ConsumeJumpRequest() && _player.IsGrounded)
            {
                ChangeState(new JumpState(_player, _stateMachine));
                return;
            }

            // Attack => boost
            if (_player.ConsumeAttackRequest())
            {
                ChangeState(new BoostState(_player, _stateMachine));
                return;
            }
        }

        public override void PhysicsUpdate()
        {
            // Sprint applies stronger acceleration and higher max speed
            if (_player.HasMovementOverride() && _player.IsOverrideExclusive())
            {
                _player.SetVelocityImmediate(_player.GetOverrideVelocity());
                return;
            }

            Vector3 desired = _player.MoveDirection;
            float targetSpeed = _player.RunSpeed;
            Vector3 desiredVel = desired * targetSpeed;

            Vector3 horizontalVel = new Vector3(_player.Rigidbody.velocity.x, 0f, _player.Rigidbody.velocity.z);
            Vector3 velocityDiff = desiredVel - horizontalVel;

            _player.AddGroundForce(velocityDiff * _player.AccelGround);
            _player.ClampHorizontalSpeed(_player.RunSpeed);

            // Rotate toward movement direction
            _player.RotateTowardsDirection(_player.MoveDirection, Time.fixedDeltaTime, isAir: false);
        }
    }
}
