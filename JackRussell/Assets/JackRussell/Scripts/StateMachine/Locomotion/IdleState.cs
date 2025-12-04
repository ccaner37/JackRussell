using JackRussell;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

namespace JackRussell.States.Locomotion
{
    /// <summary>
    /// Minimal Idle locomotion state.
    /// Handles basic ground movement and jump consumption.
    /// This is intentionally lightweight so it compiles and the state-machine is usable.
    /// Expand into separate Move/Sprint/Jump states later as needed.
    /// </summary>
    public class IdleState : PlayerStateBase
    {
        private const float k_InputDeadzone = 0.001f;

        public IdleState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override string Name => nameof(IdleState);

        public override LocomotionType LocomotionType => LocomotionType.None;

        public override void Enter()
        {
            // Start tentacle idle animation
            _player.TentacleController?.StartIdleAnimation();
            
            // Subscribe to jump press
            _player.Actions.Player.Jump.performed += OnJumpPressed;
        }

        public override void Exit(IState nextState = null)
        {
            // Stop tentacle idle animation
            _player.TentacleController?.StopIdleAnimation();

            // Unsubscribe
            _player.Actions.Player.Jump.performed -= OnJumpPressed;
        }

        private void OnJumpPressed(InputAction.CallbackContext context)
        {
            if (_player.IsGrounded)
            {
                ChangeState(new JumpState(_player, _stateMachine));
            }
        }

        public override void LogicUpdate()
        {
            // Transition to Move or Sprint when there is input
            if (_player.MoveDirection.sqrMagnitude > k_InputDeadzone)
            {
                if (_player.SprintRequested)
                    ChangeState(new SprintState(_player, _stateMachine));
                else
                    ChangeState(new MoveState(_player, _stateMachine));
                return;
            }

            HandleIdle();
        }

        public override void PhysicsUpdate()
        {
        }

        private void HandleIdle()
        {
            // Reset turn adjustments when idle
            _player.ApplyTurnAdjustments(0f, 0f, 0f);

            // If an action state has requested a movement override, apply it (exclusive)
            if (_player.HasMovementOverride())
            {
                if (_player.IsOverrideExclusive())
                {
                    _player.SetVelocityImmediate(_player.GetOverrideVelocity());
                    return;
                }
                // If it's a blend override, we will let normal locomotion run and the override can bias it.
            }
            // Pure Sonic-style deceleration when no input
            Vector3 desiredDirection = _player.MoveDirection;
            if (desiredDirection.sqrMagnitude < k_InputDeadzone)
            {
                // decelerate to stop
                Vector3 currentVelocity = _player.KinematicController.Velocity;
                Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

                if (horizontalVelocity.sqrMagnitude > 0.0001f)
                {
                    float deceleration = _player.Deceleration * Time.fixedDeltaTime;
                    Vector3 newHorizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, deceleration);

                    // Preserve vertical velocity
                    Vector3 newVelocity = new Vector3(newHorizontalVelocity.x, currentVelocity.y, newHorizontalVelocity.z);
                    _player.SetVelocityImmediate(newVelocity);
                }
                return;
            }
        }
    }
}
