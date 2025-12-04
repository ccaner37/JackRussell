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

        public override LocomotionType LocomotionType => LocomotionType.Move;

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

            // decelerate
            Vector3 horizontal = new Vector3(_player.KinematicController.Velocity.x, 0f, _player.KinematicController.Velocity.z);
            if (horizontal.sqrMagnitude > 0.0001f)
            {
                Vector3 decel = -horizontal.normalized * Mathf.Min(horizontal.magnitude, _player.Deceleration * Time.deltaTime);
                _player.AddGroundForce(decel / Time.deltaTime);
            }

            // count down then transition to idle
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                ChangeState(new IdleState(_player, _stateMachine));
            }
        }

        public override void PhysicsUpdate()
        {
        }
    }
}