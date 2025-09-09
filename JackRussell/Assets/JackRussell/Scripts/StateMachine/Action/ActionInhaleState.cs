using UnityEngine;

namespace JackRussell.States.Action
{
    public class ActionInhaleState : PlayerActionStateBase
    {
        public ActionInhaleState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override string Name => nameof(ActionInhaleState);

        private float _inhaleSpeed = 0.1f;

        public override void Enter()
        {
        }

        public override void Exit()
        {
        }

        public override void LogicUpdate()
        {
            if (!_player.InhaleRequested)
            {
                ChangeState(new ActionNoneState(_player, _stateMachine));
                return;
            }

            _player.SetPressure(_player.Pressure + _inhaleSpeed * Time.deltaTime);
        }

        public override void PhysicsUpdate()
        {
        }
    }
}
