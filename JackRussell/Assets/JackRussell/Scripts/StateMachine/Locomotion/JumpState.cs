using JackRussell;
using UnityEngine;

namespace JackRussell.States.Locomotion
{
    /// <summary>
    /// Jump state: sets initial vertical velocity and allows reduced air control.
    /// Transitions to FallState when vertical velocity goes negative.
    /// </summary>
    public class JumpState : PlayerStateBase
    {
        private const float k_AirControlFactor = 0.6f;

        public JumpState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override string Name => nameof(JumpState);

        public override void Enter()
        {
            // Apply instant vertical velocity for a snappy jump
            Vector3 v = _player.Rigidbody.linearVelocity;
            v.y = _player.JumpVelocity;
            _player.SetVelocityImmediate(v);

            // Trigger animator
            _player.Animator.SetTrigger(Animator.StringToHash("JumpTrigger"));

            _player.OnJumpEnter();
        }

        public override void LogicUpdate()
        {
            // If we start falling, transition to FallState
            if (_player.Rigidbody.linearVelocity.y < 0f)
            {
                ChangeState(new FallState(_player, _stateMachine));
                return;
            }

            // Allow action states to trigger overrides (handled elsewhere)
        }

        public override void PhysicsUpdate()
        {
            // If an exclusive movement override is active, let it control movement
            if (_player.HasMovementOverride() && _player.IsOverrideExclusive())
            {
                _player.SetVelocityImmediate(_player.GetOverrideVelocity());
                return;
            }

            // Air horizontal control (reduced)
            Vector3 desired = _player.MoveDirection;
            float targetSpeed = _player.SprintRequested ? _player.RunSpeed : _player.WalkSpeed;
            Vector3 desiredVel = desired * targetSpeed;

            Vector3 horizontalVel = new Vector3(_player.Rigidbody.linearVelocity.x, 0f, _player.Rigidbody.linearVelocity.z);
            Vector3 velocityDiff = desiredVel - horizontalVel;

            _player.AddGroundForce(velocityDiff * (_player.AccelAir * k_AirControlFactor));

            // optionally clamp to some reasonable air max (allow existing speed to persist)
            _player.ClampHorizontalSpeed(Mathf.Max(targetSpeed, horizontalVel.magnitude));

            // Rotate in air with reduced responsiveness
            _player.RotateTowardsDirection(desired, Time.fixedDeltaTime, isAir: true);

            // apply extra gravity multiplier if configured (makes falling snappier)
            if (_player.JumpVelocity != 0f && _player.AccelAir >= 0f)
            {
                // Use player's gravity multiplier (if >1 increases gravity)
                float gMult = 1f; // keep default unless you expose a separate fall multiplier
                Vector3 extraGravity = Physics.gravity * (gMult - 1f);
                if (extraGravity != Vector3.zero) _player.AddGroundForce(extraGravity);
            }
        }
    }
}
