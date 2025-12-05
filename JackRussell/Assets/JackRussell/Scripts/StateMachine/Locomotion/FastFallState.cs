using UnityEngine;

namespace JackRussell.States.Locomotion
{
    /// <summary>
    /// Fast fall state: applied when crouch is pressed during fall. Sets constant high downward speed.
    /// Transitions to LandState when grounded.
    /// </summary>
    public class FastFallState : PlayerStateBase
    {
        private const float k_AirControlFactor = 0.5f;
        private const float k_FastFallSpeed = 30f; // constant downward speed for fast fall

        public FastFallState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override string Name => nameof(FastFallState);

        public override LocomotionType LocomotionType => LocomotionType.FastFall;

        public override void Enter()
        {
            // Set constant downward velocity for fast fall
            _player.SetVelocityImmediate(new Vector3(0f, -k_FastFallSpeed, 0f));

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

            // Fast fall state persists until grounded

            // If attack requested, action state machine handles it (action SM runs in Player)

            HandleFastFall();
        }

        public override void PhysicsUpdate()
        {
        }

        private void HandleFastFall()
        {
            // If an exclusive movement override is active, let it control movement
            // if (_player.HasMovementOverride() && _player.IsOverrideExclusive())
            // {
            //     _player.SetVelocityImmediate(_player.GetOverrideVelocity());
            //     return;
            // }

            // // Get current velocity
            // Vector3 currentVel = _player.KinematicController.Velocity;

            // // Air control (weaker than jump state's control)
            // Vector3 desired = _player.MoveDirection;
            // float targetSpeed = _player.SprintRequested ? _player.RunSpeed : _player.WalkSpeed;
            // Vector3 desiredVel = desired * targetSpeed;

            // Vector3 horizontalVel = new Vector3(currentVel.x, 0f, currentVel.z);
            // Vector3 velocityDiff = desiredVel - horizontalVel;

            // // Apply air acceleration directly
            // Vector3 newHorizontalVel = horizontalVel + velocityDiff * (_player.AccelAir * k_AirControlFactor) * Time.deltaTime;

            // // Clamp horizontal speed
            // newHorizontalVel = Vector3.ClampMagnitude(newHorizontalVel, Mathf.Max(targetSpeed, newHorizontalVel.magnitude));

            // // Maintain constant fast fall speed
            // float newY = -k_FastFallSpeed;

            // // Set new velocity
            // Vector3 newVel = new Vector3(newHorizontalVel.x, newY, newHorizontalVel.z);
            // _player.SetVelocityImmediate(newVel);

            // // Rotate in air with reduced responsiveness
            // _player.RotateTowardsDirection(desired, Time.deltaTime, isAir: true);
        }
    }
}