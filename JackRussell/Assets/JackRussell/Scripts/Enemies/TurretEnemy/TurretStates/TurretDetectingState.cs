using JackRussell.States;
using UnityEngine;

namespace JackRussell.Enemies
{
    /// <summary>
    /// Turret has detected player and begins the targeting sequence.
    /// </summary>
    public class TurretDetectingState : TurretStateBase
    {
        private float _stateTimer;
        private readonly float _detectionDuration = 0.5f;
        
        public TurretDetectingState(TurretEnemy turret, StateMachine stateMachine) : base(turret, stateMachine) { }
        
        public override string Name => "Detecting";
        
        public override void Enter()
        {
            _stateTimer = 0f;
            
            // Play detection sound/effect
            _turret.PlayDetectionSound();
            
            // Enable any detection visual effects
            _turret.EnableDetectionEffects();
        }
        
        public override void Exit()
        {
            // Disable detection effects
            _turret.DisableDetectionEffects();
        }
        
        public override void LogicUpdate()
        {
            _stateTimer += Time.deltaTime;
            
            // Check if player is still in range
            if (!IsPlayerInRange())
            {
                ChangeState(new TurretIdleState(_turret, _stateMachine));
                return;
            }
            
            // After detection duration, begin targeting
            if (_stateTimer >= _detectionDuration)
            {
                ChangeState(new TurretTargetingState(_turret, _stateMachine));
            }
        }
    }
}