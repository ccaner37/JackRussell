using System;

namespace JackRussell.States
{
    /// <summary>
    /// Simple, reusable state machine that runs IState lifecycle hooks.
    /// Use Initialize(startState) to set initial state, then call LogicUpdate/PhysicsUpdate each frame from the owner.
    /// </summary>
    public class StateMachine
    {
        private IState _current;
        private float _timeInState;

        /// <summary>
        /// Current active state (may be null until initialized).
        /// </summary>
        public IState Current => _current;

        /// <summary>
        /// Time (seconds) since the current state was entered.
        /// </summary>
        public float TimeInState => _timeInState;

        /// <summary>
        /// Initializes the state machine with a starting state. Performs the Enter call.
        /// </summary>
        public void Initialize(IState startState)
        {
            if (startState == null) throw new ArgumentNullException(nameof(startState));
            _current = startState;
            _timeInState = 0f;
            _current.Enter();
        }

        /// <summary>
        /// Requests a state change. If the new state is the same instance as the current one, nothing happens.
        /// </summary>
        public void ChangeState(IState newState)
        {
            if (newState == null) throw new ArgumentNullException(nameof(newState));
            if (ReferenceEquals(newState, _current)) return;

            _current?.Exit(newState);
            _current = newState;
            _timeInState = 0f;
            _current.Enter();
        }

        /// <summary>
        /// Forcefully sets the current state without calling exit/enter hooks.
        /// Use sparingly for special cases.
        /// </summary>
        public void ForceSetState(IState newState)
        {
            _current = newState;
            _timeInState = 0f;
        }

        /// <summary>
        /// Should be called from the owner's Update() once per frame.
        /// </summary>
        public void LogicUpdate(float deltaTime)
        {
            if (_current == null) return;
            _current.LogicUpdate();
            _timeInState += deltaTime;
        }

        /// <summary>
        /// Should be called from the owner's FixedUpdate() once per fixed frame.
        /// </summary>
        public void PhysicsUpdate()
        {
            if (_current == null) return;
            _current.PhysicsUpdate();
        }
    }
}
