using JackRussell.States;
using UnityEngine;
using System.Collections;

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
            
            // Start parry window coroutine
            _turret.StartCoroutine(ParryWindowSequence());
        }
        
        public override void Exit(IState nextState = null)
        {
            // Stop glow effect
            _turret.StopGlowEffect();
            
            // Disable charging effects
            _turret.DisableChargingEffects();
            
            // Close parry window
            _turret.OnParryWindowClose();
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
        
        private IEnumerator ParryWindowSequence()
        {
            // Wait until parry time before preparation ends
            float parryWindowStartTime = _turret.PreparationTime - _turret.ParryTime;
            yield return new WaitForSeconds(parryWindowStartTime);
            
            // Open parry window
            _turret.OnParryWindowOpen();
            
            // Play parry window sound
            _turret.PlayParryWindowSound();
            
            // Enable parry window visual effects
            _turret.EnableParryWindowEffects();
            
            // Wait until preparation ends
            yield return new WaitForSeconds(_turret.ParryTime);
            
            // Close parry window (will also be called in Exit())
        }
    }
}