using UnityEngine;

namespace JackRussell.States.Action
{
    public class ActionInhaleState : PlayerActionStateBase
    {
        public ActionInhaleState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override string Name => nameof(ActionInhaleState);

        private float _inhaleSpeed = 2f;

        public override void Enter()
        {
            _player.StartLoopedSound(Audio.SoundType.InhaleLoop);
        }

        public override void Exit()
        {
            _player.StopLoopedSound(Audio.SoundType.InhaleLoop);
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
