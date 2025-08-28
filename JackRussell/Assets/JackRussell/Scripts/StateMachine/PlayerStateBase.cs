using JackRussell;
using System;

namespace JackRussell.States
{
    /// <summary>
    /// Base class for locomotion states. Holds a reference to the Player context and the owning StateMachine.
    /// Concrete states should be lightweight and only manipulate the Player via its public API.
    /// </summary>
    public abstract class PlayerStateBase : IState
    {
        protected readonly Player _player;
        protected readonly StateMachine _stateMachine;

        protected PlayerStateBase(Player player, StateMachine stateMachine)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        }

        public abstract string Name { get; }

        /// <summary>
        /// Called once when the state becomes active.
        /// Use this to initialize timers, set animator parameters, etc.
        /// </summary>
        public virtual void Enter() { }

        /// <summary>
        /// Called when the state is exited.
        /// Use this to clear parameters or stop coroutines.
        /// </summary>
        public virtual void Exit() { }

        /// <summary>
        /// Called from Player.Update() for non-physics logic and input handling.
        /// </summary>
        public virtual void LogicUpdate() { }

        /// <summary>
        /// Called from Player.FixedUpdate() for physics interactions.
        /// </summary>
        public virtual void PhysicsUpdate() { }

        /// <summary>
        /// Helper to request a locomotion state change.
        /// </summary>
        protected void ChangeState(PlayerStateBase newState)
        {
            _stateMachine.ChangeState(newState);
        }
    }
}
