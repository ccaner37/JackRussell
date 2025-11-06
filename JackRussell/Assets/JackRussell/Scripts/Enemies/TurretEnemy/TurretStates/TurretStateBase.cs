using UnityEngine;
using JackRussell.States;

namespace JackRussell.Enemies
{
    /// <summary>
    /// Base class for all turret enemy states.
    /// Follows the same pattern as PlayerStateBase but for enemy behaviors.
    /// </summary>
    public abstract class TurretStateBase : IState
    {
        protected readonly TurretEnemy _turret;
        protected readonly StateMachine _stateMachine;
        
        protected TurretStateBase(TurretEnemy turret, StateMachine stateMachine)
        {
            _turret = turret ?? throw new System.ArgumentNullException(nameof(turret));
            _stateMachine = stateMachine ?? throw new System.ArgumentNullException(nameof(stateMachine));
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
        /// Called from TurretEnemy.Update() for non-physics logic and input handling.
        /// </summary>
        public virtual void LogicUpdate() { }
        
        /// <summary>
        /// Called from TurretEnemy.FixedUpdate() for physics interactions.
        /// </summary>
        public virtual void PhysicsUpdate() { }
        
        /// <summary>
        /// Helper to request a state change.
        /// </summary>
        protected void ChangeState(TurretStateBase newState)
        {
            _stateMachine.ChangeState(newState);
        }
        
        /// <summary>
        /// Check if the player is still in detection range
        /// </summary>
        protected bool IsPlayerInRange()
        {
            return _turret.DetectPlayer();
        }
        
        /// <summary>
        /// Check if the turret head is aligned with the player within threshold
        /// </summary>
        protected bool IsAlignedWithPlayer(float thresholdAngle = 5f)
        {
            if (_turret.TargetedPlayer == null) return false;
            
            Vector3 toPlayer = _turret.TargetedPlayer.transform.position - _turret.HeadTransform.position;
            Vector3 headForward = _turret.HeadTransform.forward;
            
            return Vector3.Angle(headForward, toPlayer) < thresholdAngle;
        }
        
        /// <summary>
        /// Continue tracking the player (used by multiple states)
        /// </summary>
        protected void TrackPlayer()
        {
            _turret.RotateHeadTowardsPlayer();
        }
    }
}