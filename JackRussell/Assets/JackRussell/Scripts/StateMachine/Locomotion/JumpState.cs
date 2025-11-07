using JackRussell;
using UnityEngine;
using UnityEngine.InputSystem;

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
        
        public override LocomotionType LocomotionType => LocomotionType.Jump;

        public override void Enter()
        {
            // Apply instant vertical velocity for a snappy jump
            Vector3 v = _player.Rigidbody.linearVelocity;
            v.y = _player.JumpVelocity;
            _player.SetVelocityImmediate(v);

            // Reset air sprint flag for new jump
            _player.ResetSprintInAir();

            // Subscribe to sprint and jump presses
            _player.Actions.Player.Sprint.performed += OnSprintPressed;
            _player.Actions.Player.Jump.performed += OnJumpPressed;

            // Trigger animator
            _player.Animator.SetTrigger(Animator.StringToHash("JumpTrigger"));

            _player.OnJumpEnter();
        }

        public override void LogicUpdate()
        {
            // If crouch requested, transition to FastFallState for immediate fast descent
            if (_player.CrouchRequested)
            {
                ChangeState(new FastFallState(_player, _stateMachine));
                return;
            }

            // If we start falling, transition to FallState
            if (_player.Rigidbody.linearVelocity.y < 0f)
            {
                ChangeState(new FallState(_player, _stateMachine));
                return;
            }

            // Allow action states to trigger overrides (handled elsewhere)
        }

        public override void Exit()
        {
            // Unsubscribe
            _player.Actions.Player.Sprint.performed -= OnSprintPressed;
            _player.Actions.Player.Jump.performed -= OnJumpPressed;
        }

        private void OnSprintPressed(InputAction.CallbackContext context)
        {
            if (!_player.HasSprintedInAir)
            {
                ChangeState(new SprintState(_player, _stateMachine));
            }
        }

        private void OnJumpPressed(InputAction.CallbackContext context)
        {
            if (!_player.HasDoubleJumped)
            {
                _player.MarkDoubleJumped();
                Vector3 v = _player.Rigidbody.linearVelocity;
                v.y = _player.JumpVelocity;
                _player.SetVelocityImmediate(v);
                _player.OnJumpEnter(); // play jump sound
            }
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

            // General speed decay if speed > WalkSpeed
            Vector3 currentVel = new Vector3(_player.Rigidbody.linearVelocity.x, 0f, _player.Rigidbody.linearVelocity.z);
            float currentSpeed = currentVel.magnitude;
            if (currentSpeed > _player.WalkSpeed * 0.8f)
            {
                Vector3 targetVel = currentVel.normalized * Mathf.Lerp(currentSpeed, _player.WalkSpeed * 0.8f, Time.fixedDeltaTime * 4f);
                _player.Rigidbody.linearVelocity = new Vector3(targetVel.x, _player.Rigidbody.linearVelocity.y, targetVel.z);
            }

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
