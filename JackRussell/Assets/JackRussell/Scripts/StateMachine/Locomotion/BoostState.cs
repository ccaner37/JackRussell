using JackRussell;
using UnityEngine;

namespace JackRussell.States.Locomotion
{
    /// <summary>
    /// Boost: longer speed surge while sprinting. Uses an exclusive override so the player keeps boosted velocity,
    /// but allows limited steering (implemented by re-applying override velocity each frame based on current input).
    /// </summary>
    public class BoostState : PlayerStateBase
    {
        private float _timer;
        private Vector3 _boostDirection;

        public BoostState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override string Name => nameof(BoostState);

        public override void Enter()
        {
            _timer = _player.BoostDuration;

            // Determine boost direction: current input or forward
            _boostDirection = _player.MoveDirection.sqrMagnitude > 0.01f ? _player.MoveDirection.normalized : _player.transform.forward;

            // Request an exclusive override for the boost duration
            Vector3 boostVel = _boostDirection * _player.BoostSpeed;
            _player.RequestMovementOverride(boostVel, _timer, exclusive: true);

            // Animator flag
            _player.Animator.SetBool(Animator.StringToHash("IsBoosting"), true);

            // Immediate velocity change (keep vertical velocity)
            _player.SetVelocityImmediate(new Vector3(boostVel.x, _player.Rigidbody.linearVelocity.y, boostVel.z));
        }

        public override void Exit()
        {
            _player.ClearMovementOverride();
            _player.Animator.SetBool(Animator.StringToHash("IsBoosting"), false);
        }

        public override void LogicUpdate()
        {
            // Allow jump during boost
            if (_player.ConsumeJumpRequest() && _player.IsGrounded)
            {
                Vector3 v = _player.Rigidbody.linearVelocity;
                v.y = _player.JumpVelocity;
                _player.SetVelocityImmediate(v);
                ChangeState(new JumpState(_player, _stateMachine));
                return;
            }
        }

        public override void PhysicsUpdate()
        {
            _timer -= Time.fixedDeltaTime;
            if (_timer <= 0f)
            {
                // End boost - resume appropriate grounded/air state
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

            // Recompute a slight steering so player can influence boost direction lightly
            Vector3 steer = _player.MoveDirection.sqrMagnitude > 0.01f ? Vector3.Lerp(_boostDirection, _player.MoveDirection.normalized, 0.2f) : _boostDirection;
            Vector3 boostVel = steer.normalized * _player.BoostSpeed;
            _player.RequestMovementOverride(boostVel, _timer, exclusive: true);
            // Maintain horizontal component
            Vector3 cur = _player.Rigidbody.linearVelocity;
            _player.SetVelocityImmediate(new Vector3(boostVel.x, cur.y, boostVel.z));
        }
    }
}
