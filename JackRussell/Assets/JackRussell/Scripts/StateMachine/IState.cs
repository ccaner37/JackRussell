namespace JackRussell.States
{
    /// <summary>
    /// Lightweight state interface used by the generic StateMachine.
    /// States implement lifecycle hooks that are invoked from the Player context.
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Human-readable name for debugging. Concrete states should return their enum name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Called once when the state becomes active.
        /// </summary>
        void Enter();

        /// <summary>
        /// Called once when the state is exited.
        /// </summary>
        void Exit();

        /// <summary>
        /// Called every frame from Update() for non-physics logic / input handling.
        /// </summary>
        void LogicUpdate();

        /// <summary>
        /// Called every fixed frame from FixedUpdate() for physics interactions.
        /// </summary>
        void PhysicsUpdate();
    }
}
