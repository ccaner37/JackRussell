using JackRussell;
using UnityEngine;

namespace JackRussell.States.Action
{
    /// <summary>
    /// Default no-op action state. Keeps action state machine idle.
    /// </summary>
    public class ActionNoneState : PlayerActionStateBase
    {
        public ActionNoneState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override string Name => nameof(ActionNoneState);

        public override void Enter()
        {
            // Clear any action-specific animator flags if necessary
        }

        public override void Exit()
        {
        }

        public override void LogicUpdate()
        {
            if (_player.InhaleRequested)
            {
                ChangeState(new ActionInhaleState(_player, _stateMachine));
                return;
            }

            // Consume attack input here or let other systems create transitions.
                // For now, do nothing; other action states will be implemented later.
                if (_player.ConsumeAttackRequest())
                {
                    // If player is airborne, start homing attack (if a target exists)
                    if (!_player.IsGrounded)
                    {
                        ChangeState(new HomingAttackState(_player, _stateMachine));
                        return;
                    }

                    // Ground attack fallback: keep as no-op for now (could start a ground attack state)
                }
        }

        public override void PhysicsUpdate()
        {
            // No physics behavior for the idle action state.
        }
    }
}
