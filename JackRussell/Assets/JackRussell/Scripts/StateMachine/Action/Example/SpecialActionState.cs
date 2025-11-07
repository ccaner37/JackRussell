using UnityEngine;
using JackRussell.States;

namespace JackRussell.States.Action
{
    /// <summary>
    /// Example: Custom action state that blocks only movement and jump, but allows dash.
    /// </summary>
    public class SpecialActionState : PlayerActionStateBase
    {
        public SpecialActionState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override string Name => nameof(SpecialActionState);

        /// <summary>
        /// Block only movement and jump, but allow dash for flexibility.
        /// </summary>
        public override LocomotionType BlocksLocomotion => LocomotionType.Move | LocomotionType.Jump;

        public override void Enter()
        {
            // Special action logic here
            Debug.Log("Special action started - movement and jump are blocked, but dash is allowed");
        }

        public override void LogicUpdate()
        {
            // Example: Use player's permission system to check locomotion
            if (_player.IsLocomotionAllowed(LocomotionType.Dash))
            {
                Debug.Log("Dash is still allowed during this action!");
            }
            
            if (_player.IsLocomotionAllowed(LocomotionType.Move))
            {
                Debug.LogError("This should never print - movement is blocked!");
            }
        }
    }
}