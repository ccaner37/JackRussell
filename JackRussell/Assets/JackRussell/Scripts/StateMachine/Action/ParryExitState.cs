using UnityEngine;
using JackRussell.CameraController;
using JackRussell.GamePostProcessing;
using DG.Tweening;
using System.Collections;
using VitalRouter;

namespace JackRussell.States.Action
{
    /// <summary>
    /// Player action state for recovering after a successful parry attack.
    /// Handles recovery animation and returns to ActionNoneState.
    /// </summary>
    public class ParryExitState : PlayerActionStateBase
    {
        private readonly float _recoveryDuration = 0.15f;
        private float _timer;
        private bool _recoveryComplete;
        private ICommandPublisher _commandPublisher;
        
        public ParryExitState(Player player, StateMachine stateMachine) : base(player, stateMachine)
        {
            _commandPublisher = player.CommandPublisher;
        }
        
        public override string Name => nameof(ParryExitState);
        
        /// <summary>
        /// ParryExit blocks all locomotion to prevent interruption during the recovery sequence.
        /// </summary>
        public override LocomotionType BlocksLocomotion => LocomotionType.All;
        
        public override void Enter()
        {
            _timer = _recoveryDuration;
            _recoveryComplete = false;
            
            // Restore camera target offset to default values with cinematic timing
            _commandPublisher.PublishAsync(CameraStateUpdateCommand.WithTargetOffset(null, 0.6f));
            
            // Apply recovery animation or effects
            _player.EnableSmokeEffects();
            
            // Apply small upward velocity for recovery
            // Vector3 recoveryVelocity = new Vector3(0f, _player.JumpVelocity * 0.3f, 0f);
            // _player.SetVelocityImmediate(recoveryVelocity);
            
            // Play recovery sound
            //_player.PlaySound(Audio.SoundType.Jump);
        }
        
        public override void Exit()
        {
            // Disable recovery effects
            _player.DisableSmokeEffects();
            
            // Restore default post-processing values
            _player.PostProcessingController?.RestoreDefaultValues();
            
            // Clear any movement overrides
            _player.ClearMovementOverride();
            _player.ClearRotationOverride();

            if (_player.IsGrounded)
            {
                _player.Animator.CrossFade("Grounded", 0.15f);
            }
            else
            {
                _player.Animator.CrossFade("Ungrounded", 0.15f);
            }

            _player.PunchEffect.SetActive(false);
        }
        
        public override void LogicUpdate()
        {
            _timer -= Time.deltaTime;
            
            if (_timer <= 0f && !_recoveryComplete)
            {
                _recoveryComplete = true;
                
                // Transition back to ActionNoneState
                ChangeState(new ActionNoneState(_player, _stateMachine));
            }
        }
        
        public override void PhysicsUpdate()
        {
            // Apply gentle deceleration during recovery
            // Vector3 currentVel = _player.Rigidbody.linearVelocity;
            // Vector3 deceleratedVel = new Vector3(
            //     currentVel.x * 0.9f,
            //     currentVel.y,
            //     currentVel.z * 0.9f
            // );
            // _player.SetVelocityImmediate(deceleratedVel);
        }
    }
}