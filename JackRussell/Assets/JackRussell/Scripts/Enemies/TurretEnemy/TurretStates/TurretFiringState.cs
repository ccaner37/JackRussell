using JackRussell.States;
using UnityEngine;

namespace JackRussell.Enemies
{
    /// <summary>
    /// Turret fires laser projectile at the player.
    /// </summary>
    public class TurretFiringState : TurretStateBase
    {
        private bool _hasFired = false;
        
        public TurretFiringState(TurretEnemy turret, StateMachine stateMachine) : base(turret, stateMachine) { }
        
        public override string Name => "Firing";
        
        public override void Enter()
        {
            _hasFired = false;
            
            // Play firing sound
            _turret.PlayFiringSound();
            
            // Enable firing visual effects
            _turret.EnableFiringEffects();
        }
        
        public override void Exit()
        {
            // Disable firing effects
            _turret.DisableFiringEffects();
        }
        
        public override void LogicUpdate()
        {
            if (!_hasFired)
            {
                // Fire the laser
                _turret.FireLaser();
                _hasFired = true;
            }
            
            // Immediately transition to cooldown after firing
            ChangeState(new TurretCooldownState(_turret, _stateMachine));
        }
    }
}