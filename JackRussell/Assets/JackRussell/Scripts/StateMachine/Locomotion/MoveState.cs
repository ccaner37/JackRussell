using JackRussell;
using UnityEngine;
using UnityEngine.InputSystem;

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
                ChangeState(new JumpState(_player, _stateMachine));
            }
        }

        public override void LogicUpdate()
        {
            // Rotate player toward move direction
            _player.RotateTowardsDirection(_player.MoveDirection, Time.deltaTime, isAir: false);

            // Dash/Boost transition: use attack input (dash if not sprinting, boost if sprinting)
            if (_player.ConsumeAttackRequest())
            {
                if (_player.SprintRequested)
                    ChangeState(new BoostState(_player, _stateMachine));
                else
                    ChangeState(new DashState(_player, _stateMachine));

                return;
            }

            // If no input, go to Idle
            if (_player.MoveDirection.sqrMagnitude < k_InputDeadzone)
            {
                ChangeState(new IdleState(_player, _stateMachine));
                return;
            }

            // Sprint transition handled implicitly by SprintRequested flag in physics (or use a SprintState if preferred)
            if (_player.SprintRequested)
            {
                ChangeState(new SprintState(_player, _stateMachine));
                return;
            }
        }

        public override void PhysicsUpdate()
        {
            // If an exclusive movement override is active, let it control velocity
            if (_player.HasMovementOverride() && _player.IsOverrideExclusive())
            {
                _player.SetVelocityImmediate(_player.GetOverrideVelocity());
                return;
            }

            // Ground movement acceleration
            Vector3 desired = _player.MoveDirection;
            float targetSpeed = _player.SprintRequested ? _player.RunSpeed : _player.WalkSpeed;

            Vector3 currentVel = new Vector3(_player.Rigidbody.linearVelocity.x, 0f, _player.Rigidbody.linearVelocity.z);
            float currentSpeed = currentVel.magnitude;

            Vector3 force;
            if (currentSpeed < targetSpeed)
            {
                force = desired.normalized * _player.AccelGround - currentVel * _player.Damping;
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

        }
    }
}
