using JackRussell;
using JackRussell.States;
using UnityEngine;

namespace JackRussell.States.Locomotion
{
    /// <summary>
    /// Dash: short, high-speed horizontal surge. Uses an exclusive movement override so the action fully controls horizontal movement.
    /// </summary>
    public class DashState : PlayerStateBase
    {
        private float _timer;
        private Vector3 _dashVelocity;

        public DashState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override string Name => nameof(DashState);

        public override void Enter()
        {
            _timer = _player.DashDuration;

            // Determine dash direction: current input direction or forward facing
            Vector3 dir = _player.MoveDirection.sqrMagnitude > 0.01f ? _player.MoveDirection.normalized : _player.transform.forward;
            _dashVelocity = dir * _player.DashSpeed;

            // Request exclusive override on the player so locomotion stops controlling movement
            _player.RequestMovementOverride(_dashVelocity, _timer, exclusive: true);

            // Animator flag
            _player.Animator.SetBool(Animator.StringToHash("IsDashing"), true);

            // Immediately apply velocity for snappy start
            _player.SetVelocityImmediate(new Vector3(_dashVelocity.x, _player.Rigidbody.velocity.y, _dashVelocity.z));
        }

        public override void Exit()
        {
            // Clear override in case it's still set
            _player.ClearMovementOverride();
            _player.Animator.SetBool(Animator.StringToHash("IsDashing"), false);
        }

        public override void LogicUpdate()
        {
            // nothing special in logic; could check for jump input and allow jump during dash (if desired)
            if (_player.ConsumeJumpRequest() && _player.IsGrounded)
            {
                // Allow jumping out of a dash: set vertical velocity and transition to JumpState
                Vector3 v = _player.Rigidbody.velocity;
                v.y = _player.JumpVelocity;
                _player.SetVelocityImmediate(v);
                ChangeState(new JumpState(_player, _stateMachine));
                return;
            }
        }

        public override void PhysicsUpdate()
        {
            // Count down the dash timer
            _timer -= Time.fixedDeltaTime;
            if (_timer <= 0f)
            {
                // End dash: transition to Move or Idle depending on input / grounded
                if (_player.IsGrounded)
                {
                    if (_player.MoveDirection.sqrMagnitude > 0.01f)
                        ChangeState(new MoveState(_player, _stateMachine));
                    else
                        ChangeState(new IdleState(_player, _stateMachine));
                }
                else
                {
                    ChangeState(new FallState(_player, _stateMachine));
                }
                return;
            }

            // Ensure dash horizontal velocity is maintained
            Vector3 current = _player.Rigidbody.velocity;
            _player.SetVelocityImmediate(new Vector3(_dashVelocity.x, current.y, _dashVelocity.z));
        }
    }
}
