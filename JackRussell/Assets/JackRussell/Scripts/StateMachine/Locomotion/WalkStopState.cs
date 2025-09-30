using JackRussell;
using UnityEngine;

namespace JackRussell.States.Locomotion
{
    /// <summary>
    /// Walk stop state: plays walk stop animation before transitioning to idle.
    /// </summary>
    public class WalkStopState : PlayerStateBase
    {
        private float _timer;
        private readonly float _stopDuration = 0.01f; // adjust to animation length

        public WalkStopState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override string Name => nameof(WalkStopState);

        public override void Enter()
        {
            _timer = _stopDuration;
            // trigger walk stop animation
            _player.Animator.SetTrigger(Animator.StringToHash("WalkStopTrigger"));
        }

        public override void LogicUpdate()
        {
            // check for input to interrupt
            if (_player.MoveDirection.sqrMagnitude > 0.001f)
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
            // decelerate
            Vector3 horizontal = new Vector3(_player.Rigidbody.linearVelocity.x, 0f, _player.Rigidbody.linearVelocity.z);
            if (horizontal.sqrMagnitude > 0.0001f)
            {
                Vector3 decel = -horizontal.normalized * Mathf.Min(horizontal.magnitude, _player.Deceleration * Time.fixedDeltaTime);
                _player.AddGroundForce(decel / Time.fixedDeltaTime);
            }

            // count down then transition to idle
            _timer -= Time.fixedDeltaTime;
            if (_timer <= 0f)
            {
                ChangeState(new IdleState(_player, _stateMachine));
            }
        }
    }
}