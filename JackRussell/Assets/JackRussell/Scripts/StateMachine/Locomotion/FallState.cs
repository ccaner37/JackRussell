using UnityEngine;
using UnityEngine.InputSystem;

namespace JackRussell.States.Locomotion
{
    /// <summary>
    /// Fall state: applied when vertical velocity is downwards. Waits for landing.
    /// </summary>
    public class FallState : PlayerStateBase
    {
        private const float k_AirControlFactor = 0.5f;

        public FallState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override string Name => nameof(FallState);
        
        public override LocomotionType LocomotionType => LocomotionType.Fall;

        public override void Enter()
        {
            // Subscribe to sprint and jump presses
            _player.Actions.Player.Sprint.performed += OnSprintPressed;
            _player.Actions.Player.Jump.performed += OnJumpPressed;
            // Subscribe to dash press
            _player.Actions.Player.Dash.performed += OnDashPressed;
        }

        public override void LogicUpdate()
        {
            // When grounded, transition to LandState
            if (_player.IsGrounded)
            {
                ChangeState(new LandState(_player, _stateMachine));
                return;
            }

            // If crouch requested, transition to FastFallState
            if (_player.CrouchRequested)
            {
                ChangeState(new FastFallState(_player, _stateMachine));
                return;
            }

            // If attack requested, action state machine handles it (action SM runs in Player)
        }

        public override void Exit(IState nextState = null)
        {
            // Unsubscribe
            _player.Actions.Player.Sprint.performed -= OnSprintPressed;
            _player.Actions.Player.Jump.performed -= OnJumpPressed;
            _player.Actions.Player.Dash.performed -= OnDashPressed;
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

        private void OnDashPressed(InputAction.CallbackContext context)
        {
            if (_player.CanDash())
            {
                Vector3 dashDir = _player.GetDashDirection();
                ChangeState(new DashState(_player, _stateMachine, dashDir, this));
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

            // Air control (weaker than jump state's control)
            Vector3 desired = _player.MoveDirection;
            float targetSpeed = _player.SprintRequested ? _player.RunSpeed : _player.WalkSpeed;
            Vector3 desiredVel = desired * targetSpeed;

            Vector3 horizontalVel = new Vector3(_player.Rigidbody.linearVelocity.x, 0f, _player.Rigidbody.linearVelocity.z);
            Vector3 velocityDiff = desiredVel - horizontalVel;

            _player.AddGroundForce(velocityDiff * (_player.AccelAir * k_AirControlFactor));

            // Allow existing horizontal momentum to persist; optionally clamp to a large air max
            _player.ClampHorizontalSpeed(Mathf.Max(targetSpeed, horizontalVel.magnitude));

            // General speed decay if speed > WalkSpeed
            Vector3 currentVel = new Vector3(_player.Rigidbody.linearVelocity.x, 0f, _player.Rigidbody.linearVelocity.z);
            float currentSpeed = currentVel.magnitude;
            if (currentSpeed > _player.WalkSpeed * 0.6f)
            {
                Vector3 targetVel = currentVel.normalized * Mathf.Lerp(currentSpeed, _player.WalkSpeed * 0.6f, Time.fixedDeltaTime * 6f);
                _player.Rigidbody.linearVelocity = new Vector3(targetVel.x, _player.Rigidbody.linearVelocity.y, targetVel.z);
            }

            // Rotate in air with reduced responsiveness
            _player.RotateTowardsDirection(desired, Time.fixedDeltaTime, isAir: true);

            // Apply extra gravity if user configured gravity multiplier (handled by Player or states if necessary)
        }
    }
}
