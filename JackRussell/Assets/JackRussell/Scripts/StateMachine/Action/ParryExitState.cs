using UnityEngine;
using JackRussell.CameraController;
using DG.Tweening;
using System.Collections;

namespace JackRussell.States.Action
{
    /// <summary>
    /// Player action state for recovering after a successful parry attack.
    /// Handles recovery animation and returns to ActionNoneState.
    /// </summary>
    public class ParryExitState : PlayerActionStateBase
    {
        private readonly float _recoveryDuration = 0.2f;
        private float _timer;
        private bool _recoveryComplete;
        
        public ParryExitState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }
        
        public override string Name => nameof(ParryExitState);
        
        /// <summary>
        /// ParryExit blocks all locomotion to prevent interruption during the recovery sequence.
        /// </summary>
        public override LocomotionType BlocksLocomotion => LocomotionType.All;
        
        public override void Enter()
        {
            _timer = _recoveryDuration;
            _recoveryComplete = false;
            
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
            
            // Clear any movement overrides
            _player.ClearMovementOverride();
            _player.ClearRotationOverride();

            if (_player.IsGrounded)
            {
                _player.Animator.CrossFade("Grounded", 0.1f);
            }
            else
            {
                _player.Animator.CrossFade("Ungrounded", 0.1f);
            }
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