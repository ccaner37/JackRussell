using UnityEngine;

namespace JackRussell.States.Locomotion
{
    /// <summary>
    /// Fast fall state: applied when crouch is held during fall. Increases downward speed.
    /// Transitions back to FallState when crouch is released or when grounded.
    /// </summary>
    public class FastFallState : PlayerStateBase
    {
        private const float k_AirControlFactor = 0.5f;
        private const float k_FastFallMultiplier = 20f; // multiplier for extra downward force
        private float _fastFallDelayTimer;

        public FastFallState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override string Name => nameof(FastFallState);

        public override void Enter()
        {
            // Cancel all velocity to immediately start fast falling straight down
            _player.SetVelocityImmediate(Vector3.zero);

            // Delay before applying fast fall force
            _fastFallDelayTimer = 0.1f;

            // Could set fast fall animator parameter if desired
            _player.Animator.CrossFade("fast_fall_enter", 0.08f);
        }

        public override void LogicUpdate()
        {
            // When grounded, transition to LandState
            if (_player.IsGrounded)
            {
                ChangeState(new LandState(_player, _stateMachine));
                return;
            }

            // Fast fall state persists until grounded (crouch input was the trigger to enter)

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

            // Rotate in air with reduced responsiveness
            _player.RotateTowardsDirection(desired, Time.fixedDeltaTime, isAir: true);

            // Update delay timer
            _fastFallDelayTimer -= Time.fixedDeltaTime;

            // Apply extra gravity for fast fall after delay
            if (_fastFallDelayTimer <= 0f)
            {
                Vector3 extraGravity = Physics.gravity * (k_FastFallMultiplier - 1f);
                if (extraGravity != Vector3.zero) _player.AddGroundForce(extraGravity);
            }
        }
    }
}