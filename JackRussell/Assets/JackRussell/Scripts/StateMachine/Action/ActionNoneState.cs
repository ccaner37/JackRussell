using JackRussell;
using JackRussell.Enemies;
using UnityEngine;
using UnityEngine.InputSystem;

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
            
            // Subscribe to attack input
            _player.Actions.Player.Attack.performed += OnAttackPressed;
        }

        public override void Exit(IState nextState = null)
        {
            // Unsubscribe from attack input
            _player.Actions.Player.Attack.performed -= OnAttackPressed;
        }

        public override void LogicUpdate()
        {
            if (_player.InhaleRequested)
            {
                ChangeState(new ActionInhaleState(_player, _stateMachine));
                return;
            }

            // Attack input is now handled by event subscription in Enter/Exit methods
        }

        public override void PhysicsUpdate()
        {
            // No physics behavior for the idle action state.
        }

        private void OnAttackPressed(InputAction.CallbackContext context)
        {
            // Check for parry opportunity first
            if (TryParryAttack())
            {
                return;
            }

            // If player is airborne, start homing attack (if a target exists)
            if (_player.CanHomingAttack())
            {
                ChangeState(new HomingAttackState(_player, _stateMachine));
                return;
            }

            // Ground attack fallback: keep as no-op for now (could start a ground attack state)
        }

        private bool TryParryAttack()
        {
            // Find nearest parryable enemy using shared utility
            var parryable = ParryUtility.FindNearestParryableEnemy(_player);
            
            if (parryable != null && parryable.IsInParryWindow)
            {
                // Found a parryable enemy in parry window, initiate parry attack
                ChangeState(new ParryAttackState(_player, _stateMachine));
                return true;
            }
            
            return false;
        }
    }
}
