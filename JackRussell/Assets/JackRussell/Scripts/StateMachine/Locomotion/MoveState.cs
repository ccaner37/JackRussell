using JackRussell;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

namespace JackRussell.States.Locomotion
{
    /// <summary>
    /// Move state: handles normal ground movement and transitions to sprint/dash/jump.
    /// Lightweight and focused on locomotion physics only.
    /// </summary>
    public class MoveState : PlayerStateBase
    {
        private const float k_InputDeadzone = 0.001f;

        public MoveState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override string Name => nameof(MoveState);
        
        public override LocomotionType LocomotionType => LocomotionType.Move;

        public override void Enter()
        {
            // Subscribe to jump press
            _player.Actions.Player.Jump.performed += OnJumpPressed;
            // Subscribe to sprint press
            _player.Actions.Player.Sprint.performed += OnSprintPressed;
            // Subscribe to dash press
            _player.Actions.Player.Dash.performed += OnDashPressed;
        }

        public override void Exit(IState nextState = null)
        {
            // Unsubscribe
            _player.Actions.Player.Jump.performed -= OnJumpPressed;
            _player.Actions.Player.Sprint.performed -= OnSprintPressed;
            _player.Actions.Player.Dash.performed -= OnDashPressed;
        }

        private void OnJumpPressed(InputAction.CallbackContext context)
        {
            if (_player.IsGrounded)
            {
                ChangeState(new JumpState(_player, _stateMachine));
            }
        }

        private void OnSprintPressed(InputAction.CallbackContext context)
        {
            ChangeState(new SprintState(_player, _stateMachine));
        }

        private void OnDashPressed(InputAction.CallbackContext context)
        {
            if (_player.CanDash())
            {
                Vector3 dashDir = _player.GetDashDirection();
                ChangeState(new DashState(_player, _stateMachine, dashDir, this));
            }
        }

        public override void LogicUpdate()
        {
            // Kinematic controller handles automatic rotation toward movement direction

            // If no input, go to WalkStop if moving fast enough
            if (_player.MoveDirection.sqrMagnitude < k_InputDeadzone)
            {
                Vector3 horizontalVel = new Vector3(_player.KinematicController.Velocity.x, 0f, _player.KinematicController.Velocity.z);
                if (horizontalVel.magnitude > 8f)
                {
                    ChangeState(new WalkStopState(_player, _stateMachine));
                }
                else
                {
                    ChangeState(new IdleState(_player, _stateMachine));
                }
                return;
            }

            HandleMovement();

        }

        public override void PhysicsUpdate()
        {

        }

        private void HandleMovement()
        {
            // If an exclusive movement override is active, let it control velocity
            if (_player.HasMovementOverride() && _player.IsOverrideExclusive())
            {
                _player.SetVelocityImmediate(_player.GetOverrideVelocity());
                return;
            }

            // Pure Sonic-style ground movement using direct velocity calculation
            Vector3 desiredDirection = _player.MoveDirection;
            float targetSpeed = _player.SprintRequested ? _player.RunSpeed : _player.WalkSpeed;

            Vector3 currentVelocity = _player.KinematicController.Velocity;
            Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
            float currentSpeed = horizontalVelocity.magnitude;

            Vector3 newVelocity;

            if (desiredDirection.sqrMagnitude > 0.001f)
            {
                // Accelerating toward target speed
                if (currentSpeed < targetSpeed)
                {
                    // Accelerate toward desired direction
                    float acceleration = _player.AccelGround * Time.deltaTime;
                    Vector3 targetVelocity = desiredDirection.normalized * targetSpeed;

                    // Smooth acceleration curve (Sonic-style)
                    newVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration);
                }
                else
                {
                    // Decelerate if going too fast
                    float deceleration = _player.Deceleration * Time.deltaTime;
                    Vector3 targetVelocity = desiredDirection.normalized * targetSpeed;
                    newVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, deceleration);
                }
            }
            else
            {
                // No input - decelerate to stop
                float deceleration = _player.Deceleration * Time.deltaTime;
                newVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, deceleration);
            }

            // Preserve vertical velocity (gravity)
            newVelocity.y = currentVelocity.y;

            // Set the calculated velocity
            _player.SetVelocityImmediate(newVelocity);

            // Apply turn adjustments
            _player.ApplyTurnAdjustments(_player.GetIKWeight(), _player.MoveRollMaxDegrees, 1f);
        }
    }
}
