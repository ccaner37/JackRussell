using JackRussell.States;
using UnityEngine;

namespace JackRussell.Enemies
{
    /// <summary>
    /// Turret charges up its attack (glow effect) before firing.
    /// </summary>
    public class TurretPreparingState : TurretStateBase
    {
        private float _timer;
        
        public TurretPreparingState(TurretEnemy turret, StateMachine stateMachine) : base(turret, stateMachine) { }
        
        public override string Name => "Preparing";
        
        public override void Enter()
        {
            _timer = _turret.PreparationTime;
            
            // Start glow effect
            _turret.StartGlowEffect();
            
            // Play charging sound
            _turret.PlayChargingSound();
            
            // Enable charging visual effects
            _turret.EnableChargingEffects();
        }
        
        public override void Exit()
        {
            // Stop glow effect
            _turret.StopGlowEffect();
            
            // Disable charging effects
            _turret.DisableChargingEffects();
        }
        
        public override void LogicUpdate()
        {
            _timer -= Time.deltaTime;
            
            // Check if player is still in range
            if (!IsPlayerInRange())
            {
                ChangeState(new TurretIdleState(_turret, _stateMachine));
                return;
            }
            
            // Check if preparation is complete
            if (_timer <= 0f)
            {
                ChangeState(new TurretFiringState(_turret, _stateMachine));
            }
        }
        
        public override void PhysicsUpdate()
        {
            // Continue tracking player during preparation
            TrackPlayer();
        }
    }
}