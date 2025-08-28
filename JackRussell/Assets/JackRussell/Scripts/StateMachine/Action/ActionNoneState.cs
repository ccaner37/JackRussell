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
            // Consume attack input here or let other systems create transitions.
            // For now, do nothing; other action states will be implemented later.
            if (_player.ConsumeAttackRequest())
            {
                // Example: start a simple attack state in the future.
                // Leaving as a placeholder to be expanded.
            }
        }

        public override void PhysicsUpdate()
        {
            // No physics behavior for the idle action state.
        }
    }
}
