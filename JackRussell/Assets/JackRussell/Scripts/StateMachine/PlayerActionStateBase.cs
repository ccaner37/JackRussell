using JackRussell;
using System;

namespace JackRussell.States
{
    /// <summary>
    /// Base class for action/combat states (parallel to locomotion).
    /// Action states can manipulate the Player via its public API and request locomotion overrides.
    /// </summary>
    public abstract class PlayerActionStateBase : IState, IBlocksLocomotion
    {
        protected readonly Player _player;
        protected readonly StateMachine _stateMachine;

        protected PlayerActionStateBase(Player player, StateMachine stateMachine)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        }

        public abstract string Name { get; }

        /// <summary>
        /// Default: action states don't block locomotion unless explicitly overridden.
        /// Override this property in derived action states to block specific locomotion types.
        /// </summary>
        public virtual LocomotionType BlocksLocomotion => LocomotionType.None;
        
        public virtual bool IsBlockingLocomotion => BlocksLocomotion != LocomotionType.None;

        public virtual void Enter() { }

        public virtual void Exit(IState nextState = null) { }

        public virtual void LogicUpdate() { }

        public virtual void PhysicsUpdate() { }

        /// <summary>
        /// Helper to request an action state change.
        /// </summary>
        protected void ChangeState(PlayerActionStateBase newState)
        {
            _stateMachine.ChangeState(newState);
        }
    }
}
