using JackRussell.States;
using UnityEngine;

namespace JackRussell.Enemies
{
    /// <summary>
    /// Turret rotates to face the player and prepares to attack.
    /// </summary>
    public class TurretTargetingState : TurretStateBase
    {
        private float _alignmentThreshold = 5f; // Degrees
        private float _maxTargetingTime = 3f; // Maximum time to spend targeting
        private float _targetingTimer;
        
        public TurretTargetingState(TurretEnemy turret, StateMachine stateMachine) : base(turret, stateMachine) { }
        
        public override string Name => "Targeting";
        
        public override void Enter()
        {
            _targetingTimer = 0f;
            
            // Play targeting sound/effect
            _turret.PlayTargetingSound();
        }
        
        public override void LogicUpdate()
        {
            _targetingTimer += Time.deltaTime;
            
            // Check if player is still in range
            if (!IsPlayerInRange())
            {
                ChangeState(new TurretIdleState(_turret, _stateMachine));
                return;
            }
            
            // Check if we've been targeting too long (failsafe)
            if (_targetingTimer >= _maxTargetingTime)
            {
                ChangeState(new TurretCooldownState(_turret, _stateMachine));
                return;
            }
            
            // Check if turret is aligned with player
            if (IsAlignedWithPlayer(_alignmentThreshold))
            {
                ChangeState(new TurretPreparingState(_turret, _stateMachine));
            }
        }
        
        public override void PhysicsUpdate()
        {
            // Continue tracking player during targeting
            TrackPlayer();
        }
    }
}