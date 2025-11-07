using JackRussell;
using UnityEngine;
using DG.Tweening;

namespace JackRussell.States.Locomotion
{
    /// <summary>
    /// Dash state: short-duration directional burst of speed with teleport-like feel, works in air and ground.
    /// Features initial delay, animator crossfade, and high-speed movement.
    /// Consumes a charge and returns to the triggering state after duration.
    /// </summary>
    public class DashState : PlayerStateBase
    {
        private Vector3 _dashDirection;
        private PlayerStateBase _returnState;
        private float _timer;
        private bool _hasStartedMovement;
        private const float DASH_DELAY = 0.05f;
        private const string DASH_ANIMATION = "3321_0_dio3_combo_in";

        public DashState(Player player, StateMachine stateMachine, Vector3 dashDirection, PlayerStateBase returnState)
            : base(player, stateMachine)
        {
            _dashDirection = dashDirection.normalized;
            _returnState = returnState;
        }

        public override string Name => nameof(DashState);

        public override LocomotionType LocomotionType => LocomotionType.Dash;

        public override void Enter()
        {
            _player.OnDashEnter();

            // Consume charge
            _player.ConsumeCharge();

            // Crossfade to dash animation
            _player.Animator.CrossFade(DASH_ANIMATION, 0.05f);

            // Reset flags
            _hasStartedMovement = false;
            _timer = DASH_DELAY + _player.DashDuration;

            _player.PlaySound(Audio.SoundType.Dash);
        }

        public override void Exit()
        {
            // Clear dash velocity to prevent slipping
            _player.SetVelocityImmediate(Vector3.zero);

            _player.EnableFootIK();
        }

        public override void LogicUpdate()
        {
            // Decrement timer
            _timer -= Time.deltaTime;

            // Start movement after delay
            if (!_hasStartedMovement && _timer <= _player.DashDuration)
            {
                _hasStartedMovement = true;
                // Teleport-like movement: instant velocity set for high-speed feel
                _player.SetVelocityImmediate(_dashDirection * _player.DashSpeed);
            }

            if (_timer <= 0f)
            {
                // Return to triggering state
                ChangeState(_returnState);
            }
        }

        public override void PhysicsUpdate()
        {
            // Respect exclusive movement overrides
            if (_player.HasMovementOverride() && _player.IsOverrideExclusive())
            {
                _player.SetVelocityImmediate(_player.GetOverrideVelocity());
                return;
            }

            // Only apply physics after movement has started
            if (_hasStartedMovement)
            {
                // Maintain consistent dash velocity
                _player.SetVelocityImmediate(_dashDirection * _player.DashSpeed);

                // Apply ground projection only if grounded
                if (_player.IsGrounded)
                {
                    Vector3 projectedVel = Vector3.ProjectOnPlane(_player.Rigidbody.linearVelocity, _player.GroundNormal);
                    _player.Rigidbody.linearVelocity = projectedVel;
                }
            }
        }
    }
}