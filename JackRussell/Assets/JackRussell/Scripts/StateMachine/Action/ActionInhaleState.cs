namespace JackRussell.States.Action
{
    public class ActionInhaleState : PlayerActionStateBase
    {
        public ActionInhaleState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override string Name => nameof(ActionInhaleState);

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
        }

        public override void PhysicsUpdate()
        {
        }
    }
}
