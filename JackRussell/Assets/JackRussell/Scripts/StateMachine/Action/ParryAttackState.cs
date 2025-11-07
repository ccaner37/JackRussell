using UnityEngine;
using JackRussell.CameraController;
using JackRussell.Enemies;
using DG.Tweening;
using System.Collections;
using JackRussell.Audio;

namespace JackRussell.States.Action
{
    /// <summary>
    /// Player action state for performing parry attacks.
    /// Teleports player to parryable enemy and instantly kills them.
    /// Triggered when player attacks during enemy's parry window.
    /// </summary>
    public class ParryAttackState : PlayerActionStateBase
    {
        private IParryable _target;
        private bool _hasTeleported;
        private Vector3 _startPosition;
        private Vector3 _targetPosition;
        
        public ParryAttackState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }
        
        public override string Name => nameof(ParryAttackState);
        
        /// <summary>
        /// ParryAttack blocks all locomotion to prevent interruption during the teleportation and attack sequence.
        /// </summary>
        public override LocomotionType BlocksLocomotion => LocomotionType.All;
        
        public override void Enter()
        {
            // Find nearest parryable enemy
            _target = ParryUtility.FindNearestParryableEnemy(_player);

            if (_target == null || !_target.IsInParryWindow)
            {
                // No valid target, exit immediately
                ChangeState(new ActionNoneState(_player, _stateMachine));
                return;
            }

            _player.ResetLocomotionState();
            
            // Initialize teleportation
            _startPosition = _player.transform.position;
            _targetPosition = _target.ParryTargetTransform.position;
            //_timer = _teleportDuration;
            _hasTeleported = false;

            // Play parry start sound
            _player.PlaySound(SoundType.ParryAttackSuccess);

            _player.Animator.CrossFade("3001_1_stapla_06_Quickdraw_02_in", 0.35f);
            
            // Enable parry visual effects
            _player.EnableSmokeEffects();
            
            // Start teleportation sequence
            _player.StartCoroutine(ParrySequence());
        }
        
        public override void Exit()
        {
            // Disable parry effects
            _player.DisableSmokeEffects();
            
            // Clear any movement overrides
            _player.ClearMovementOverride();
            _player.ClearRotationOverride();
        }
        
        public override void LogicUpdate()
        {
            // _timer -= Time.deltaTime;
            
            // if (_timer <= 0f)
            // {
            //     // Transition to recovery state instead of directly to ActionNoneState
            //     ChangeState(new ParryExitState(_player, _stateMachine));
            //     return;
            // }
        }
        
        public override void PhysicsUpdate()
        {
            // Movement is handled by coroutine
        }
        
        private IEnumerator ParrySequence()
        {
            // Phase 1: Initial dash towards target
            Vector3 direction = (_targetPosition - _startPosition).normalized;
            
            //Vector3 dashVelocity = direction * _teleportSpeed;
            
            //_player.RequestMovementOverride(dashVelocity, _teleportDuration * 0.5f, true);
            _player.RotateTowardsDirection(direction, Time.fixedDeltaTime, isAir: true, instantaneous: true);

            yield return new WaitForSeconds(0.3f); // _teleportDuration * 0.5f
            
            _player.PlaySound(SoundType.Teleport);
            
            // Phase 2: Teleport to target
            if (!_hasTeleported && _target != null)
            {
                _hasTeleported = true;

                // Instant teleport to target
                //_player.transform.position = _targetPosition;
                _player.Rigidbody.position = Vector3.Lerp(_startPosition, _targetPosition, 0.8f);

                yield return new WaitForSeconds(0.35f);
                _player.Animator.Play("3001_1_stapla_06_Quickdraw_03_ht");

                yield return new WaitForSeconds(0.1f);

                // Trigger parry on enemy
                _target.OnParried(_player);
                
                _player.PlaySound(SoundType.HeavyPunch);
                
                // Play parry success effects
                PlayParrySuccessEffects();
                
                // Camera shake for impact
                var cameraController = Object.FindAnyObjectByType<CinemachineCameraController>();
                if (cameraController != null)
                {
                    cameraController.ShakeCamera(2f, 0.5f);
                }
                
                // Apply small bounce back
                // Vector3 bounceBack = -direction * _player.JumpVelocity * 0.5f;
                // _player.SetVelocityImmediate(new Vector3(bounceBack.x, _player.JumpVelocity * 0.3f, bounceBack.z));
            }

            yield return new WaitForSeconds(0.2f);
            ChangeState(new ParryExitState(_player, _stateMachine));
        }
        
        
        private void PlayParrySuccessEffects()
        {
            // Play hit particle at target position
            var hitParticle = _player.HomingHitParticle;
            if (hitParticle != null)
            {
                hitParticle.transform.position = _targetPosition;
                hitParticle.Play();
            }
            
            // Create visual effect at target position
            CreateParryVisualEffect();
        }
        
        private void CreateParryVisualEffect()
        {
            // Create a temporary visual effect at the parry target position
            // GameObject effectObj = new GameObject("ParryEffect");
            // effectObj.transform.position = _targetPosition;
            
            // // Add a simple particle system or visual indicator
            // var particleSystem = effectObj.AddComponent<ParticleSystem>();
            // var main = particleSystem.main;
            // main.startColor = Color.cyan;
            // main.startSize = 2f;
            // main.startLifetime = 0.5f;
            // main.duration = 0.5f;
            // main.loop = false;
            // main.playOnAwake = true;
            
            // // Auto-destroy after effect completes
            // _player.StartCoroutine(DestroyEffectAfterDelay(effectObj, 1f));
        }
        
        private IEnumerator DestroyEffectAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null)
            {
                Object.Destroy(obj);
            }
        }
    }
}