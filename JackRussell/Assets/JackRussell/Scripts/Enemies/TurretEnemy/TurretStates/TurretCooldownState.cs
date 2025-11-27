using JackRussell.States;
using UnityEngine;

namespace JackRussell.Enemies
{
    /// <summary>
    /// Turret is in cooldown after firing, cannot act for duration.
    /// </summary>
    public class TurretCooldownState : TurretStateBase
    {
        private float _timer;
        
        public TurretCooldownState(TurretEnemy turret, StateMachine stateMachine) : base(turret, stateMachine) { }
        
        public override string Name => "Cooldown";
        
        public override void Enter()
        {
            _timer = _turret.CooldownTime;
            
            // Play cooldown sound/effect
            _turret.PlayCooldownSound();
            
            // Enable cooldown visual effects
            _turret.EnableCooldownEffects();
        }
        
        public override void Exit(IState nextState = null)
        {
            // Disable cooldown effects
            _turret.DisableCooldownEffects();
        }
        
        public override void LogicUpdate()
        {
            _timer -= Time.deltaTime;
            
            // Check if cooldown is complete
            if (_timer <= 0f)
            {
                // Check if player is still in range
                if (IsPlayerInRange())
                {
                    ChangeState(new TurretTargetingState(_turret, _stateMachine));
                }
                else
                {
                    ChangeState(new TurretIdleState(_turret, _stateMachine));
                }
            }
        }
    }
}