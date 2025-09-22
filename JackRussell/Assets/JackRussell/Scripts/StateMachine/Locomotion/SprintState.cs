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
            // Check pressure
            if (_player.Pressure < 5f)
            {
                // not enough pressure, go back to move or idle
                if (_player.MoveDirection.sqrMagnitude > 0.001f)
                    ChangeState(new MoveState(_player, _stateMachine));
                else
                    ChangeState(new IdleState(_player, _stateMachine));
                return;
            }

            // Consume pressure
            _player.SetPressure(_player.Pressure - 5f);

            // Set animator sprint flag if desired
            _player.OnSprintEnter();
            _player.Animator.SetBool(Animator.StringToHash("IsSprinting"), true);
            _player.PlaySound(Audio.SoundType.SprintStart);
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

            Vector3 currentVel = new Vector3(_player.Rigidbody.linearVelocity.x, 0f, _player.Rigidbody.linearVelocity.z);
            float currentSpeed = currentVel.magnitude;

            Vector3 force;
            if (currentSpeed < targetSpeed)
            {
                force = desired.normalized * (_player.AccelGround * 10) - currentVel * _player.Damping;
            }
            else
            {
                force = -currentVel * _player.Damping;
            }

            _player.AddGroundForce(force);

            // Project velocity onto ground plane to keep movement along the surface
            if (_player.IsGrounded)
            {
                Vector3 projectedVel = Vector3.ProjectOnPlane(_player.Rigidbody.linearVelocity, _player.GroundNormal);
                _player.Rigidbody.linearVelocity = projectedVel;
            }

            // Rotate toward movement direction
            _player.RotateTowardsDirection(desired, Time.fixedDeltaTime, isAir: false);
        }
    }
}
