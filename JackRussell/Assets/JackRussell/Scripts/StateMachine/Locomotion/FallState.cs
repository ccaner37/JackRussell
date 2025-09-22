using UnityEngine;

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

        public override void Enter()
        {
            // Could set fall animator parameter if desired
        }

        public override void LogicUpdate()
        {
            // When grounded, transition to LandState
            if (_player.IsGrounded)
            {
                ChangeState(new LandState(_player, _stateMachine));
                return;
            }

            // If attack requested, action state machine handles it (action SM runs in Player)
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

            // Apply extra gravity if user configured gravity multiplier (handled by Player or states if necessary)
        }
    }
}
