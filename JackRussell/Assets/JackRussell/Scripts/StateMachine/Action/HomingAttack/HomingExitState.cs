using UnityEngine;
using JackRussell;
using JackRussell.States.Action;

namespace JackRussell.States.Action
{
    /// <summary>
    /// State for handling the exit animations after a successful homing attack.
    /// Randomly selects and crossfades to one of the configured HomingExit animations with custom offset and duration, then transitions to the configured Jump_Down animation and back to ActionNoneState.
    /// </summary>
    public class HomingExitState : PlayerActionStateBase
    {
        private HomingExitAnimationConfig.AnimationData _selectedExitData;
        private bool _transitionedToJumpDown;

        public HomingExitState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }
        
        public override string Name => nameof(HomingExitState);
        
        /// <summary>
        /// HomingExit blocks all locomotion to prevent interruption during the exit animation sequence.
        /// </summary>
        public override LocomotionType BlocksLocomotion => LocomotionType.All;

        public override void Enter()
        {
            // Randomly select from configured exit animations, avoiding the same as last time
            var exitAnimations = _player.HomingExitConfig.exitAnimations;
            if (exitAnimations.Count == 0)
            {
                // Fallback if no animations configured
                ChangeState(new ActionNoneState(_player, _stateMachine));
                return;
            }

            int randomIndex;
            if (exitAnimations.Count == 1)
            {
                randomIndex = 0;
            }
            else
            {
                do
                {
                    randomIndex = Random.Range(0, exitAnimations.Count);
                } while (randomIndex == _player.LastHomingExitIndex);
            }
            _selectedExitData = exitAnimations[randomIndex];
            _player.LastHomingExitIndex = randomIndex;

            // Smooth crossfade with configured offset and duration
            _player.Animator.CrossFade(_selectedExitData.animationName, _selectedExitData.transitionDuration, 0, _selectedExitData.enterOffset);

            _transitionedToJumpDown = false;
        }

        public override void LogicUpdate()
        {
            var stateInfo = _player.Animator.GetCurrentAnimatorStateInfo(0);

            // Wait for the selected exit animation to reach its configured exit time
            if (stateInfo.IsName(_selectedExitData.animationName) && stateInfo.normalizedTime >= _selectedExitData.exitNormalizedTime)
            {
                if (!_transitionedToJumpDown)
                {
                    // Smooth crossfade to configured Jump_Down animation
                    var jumpData = _player.HomingExitConfig.jumpDownAnimation;
                    _player.Animator.CrossFade(jumpData.animationName, jumpData.transitionDuration);
                    _transitionedToJumpDown = true;
                }

                // Transition back to ActionNoneState after starting the jump down animation
                ChangeState(new ActionNoneState(_player, _stateMachine));
            }
        }

        public override void Exit()
        {
            if (_player.LocomotionStateName == "PathFollowState")
            {
                        _player.Animator.Play("thug_life");
            }
        }
    }
}