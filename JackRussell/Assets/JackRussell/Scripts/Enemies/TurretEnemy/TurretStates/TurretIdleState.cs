using JackRussell.States;
using UnityEngine;

namespace JackRussell.Enemies
{
    /// <summary>
    /// Turret is idle and inactive, waiting to detect a player.
    /// </summary>
    public class TurretIdleState : TurretStateBase
    {
        public TurretIdleState(TurretEnemy turret, StateMachine stateMachine) : base(turret, stateMachine) { }
        
        public override string Name => "Idle";
        
        public override void LogicUpdate()
        {
            // Check if player is detected
            if (IsPlayerInRange())
            {
                ChangeState(new TurretDetectingState(_turret, _stateMachine));
            }
        }
    }
}