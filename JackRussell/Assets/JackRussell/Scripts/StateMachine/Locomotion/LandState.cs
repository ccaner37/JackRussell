using JackRussell;
using JackRussell.States;
using UnityEngine;

namespace JackRussell.States.Locomotion
{
    /// <summary>
    /// Land state: short landing window after hitting ground, then resumes locomotion.
    /// </summary>
    public class LandState : PlayerStateBase
    {
        private float _timer;
        private readonly float _landDuration = 0.05f;

        public LandState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override string Name => nameof(LandState);

        public override void Enter()
        {
            _timer = 0.4f; // adjust to animation length
            // trigger landing-move animation based on speed/sprint, if speed > 3
            Vector3 horizontalVel = new Vector3(_player.Rigidbody.linearVelocity.x, 0f, _player.Rigidbody.linearVelocity.z);
            if (horizontalVel.magnitude > 6f)
            {
                bool isSprinting = _player.Animator.GetBool(Animator.StringToHash("IsSprinting"));
                bool highSpeed = horizontalVel.magnitude > _player.WalkSpeed * 1.2F;
                if (isSprinting || highSpeed)
                {
                    _player.Animator.SetTrigger(Animator.StringToHash("LandMoveHighTrigger"));
                }
                else
                {
                    _player.Animator.SetTrigger(Animator.StringToHash("LandMoveLowTrigger"));
                }
            }

            // Optionally zero vertical velocity to avoid small bounces
            var v = _player.Rigidbody.linearVelocity;
            v.y = 0f;
            _player.SetVelocityImmediate(v);
        }

        public override void LogicUpdate()
        {
            // nothing special in LogicUpdate for now
        }

        public override void PhysicsUpdate()
        {
            // Count down then transition to move or idle depending on input
            _timer -= Time.fixedDeltaTime;
            if (_timer <= 0f)
            {
                if (_player.MoveDirection.sqrMagnitude > 0.001f)
                    ChangeState(new MoveState(_player, _stateMachine));
                else
                    ChangeState(new IdleState(_player, _stateMachine));
            }
        }
    }
}
