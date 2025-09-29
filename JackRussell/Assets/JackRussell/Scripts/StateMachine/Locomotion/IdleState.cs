using JackRussell;
using UnityEngine;
using UnityEngine.InputSystem;

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

        public override void Enter()
        {
            // Subscribe to jump press
            _player.Actions.Player.Jump.performed += OnJumpPressed;
        }

        public override void Exit()
        {
            // Unsubscribe
            _player.Actions.Player.Jump.performed -= OnJumpPressed;
        }

        private void OnJumpPressed(InputAction.CallbackContext context)
        {
            if (_player.IsGrounded)
            {
                // simple jump handling here; a dedicated JumpState can replace this later
                Vector3 v = _player.Rigidbody.linearVelocity;
                v.y = _player.JumpVelocity;
                _player.SetVelocityImmediate(v);
                _player.Animator.SetTrigger(Animator.StringToHash("JumpTrigger"));
                // switch to JumpState so proper jump state logic runs
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
        }

        public override void PhysicsUpdate()
        {
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

            // Basic ground movement physics: gentle acceleration toward desired velocity
            Vector3 desired = _player.MoveDirection;
            if (desired.sqrMagnitude < k_InputDeadzone)
            {
                // decelerate to stop
                Vector3 horizontal = new Vector3(_player.Rigidbody.linearVelocity.x, 0f, _player.Rigidbody.linearVelocity.z);
                if (horizontal.sqrMagnitude > 0.0001f)
                {
                    Vector3 decel = -horizontal.normalized * Mathf.Min(horizontal.magnitude, _player.Deceleration * Time.fixedDeltaTime);
                    _player.AddGroundForce(decel / Time.fixedDeltaTime);
                }
                return;
            }

            float targetSpeed = _player.SprintRequested ? _player.RunSpeed : _player.WalkSpeed;
            Vector3 desiredVel = desired * targetSpeed;

            Vector3 horizontalVel = new Vector3(_player.Rigidbody.linearVelocity.x, 0f, _player.Rigidbody.linearVelocity.z);
            Vector3 velocityDiff = desiredVel - horizontalVel;

            // apply ground acceleration
            _player.AddGroundForce(velocityDiff * _player.AccelGround);

            // clamp to the appropriate top speed
            float clampTo = _player.SprintRequested ? _player.RunSpeed : _player.WalkSpeed;
            _player.ClampHorizontalSpeed(clampTo);
        }
    }
}
