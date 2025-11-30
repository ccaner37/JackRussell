using UnityEngine;
using JackRussell.States.Action;
using System.Collections;
using DG.Tweening;
using VContainer;
using VitalRouter;
using JackRussell;

namespace JackRussell.Enemies
{
    /// <summary>
    /// Base class for all enemy entities.
    /// Inherits from GameEntity and implements HomingTarget to be targetable by player homing attacks.
    /// </summary>
    public abstract class Enemy : GameEntity, IHomingTarget, IParryable
    {
        [Header("Enemy Settings")]
        [SerializeField] protected bool _isActive = true;
        [SerializeField] protected bool _destroyOnDeath = true;
        [SerializeField] protected GameObject _deathEffectPrefab;
        [SerializeField] protected float _deathEffectDuration = 2f;

        [Inject] private readonly ICommandPublisher _commandPublisher;
        
        [Header("Hit Effects")]
        [SerializeField] private ParticleSystem _hitEffect;
        [SerializeField] private MeshRenderer[] _hitEffectRenderers;
        
        protected bool IsEnemyActive => _isActive;
        
        private IEnumerator OnHitMaterialEffect()
        {
            if (_hitEffectRenderers != null)
            {
                foreach (var renderer in _hitEffectRenderers)
                {
                    if (renderer != null && renderer.material != null)
                    {
                        renderer.material.DOFloat(1f, "_HitBlend", 0.1f);
                    }
                }

                yield return new WaitForSeconds(0.2f);

                foreach (var renderer in _hitEffectRenderers)
                {
                    if (renderer != null && renderer.material != null)
                    {
                        renderer.material.DOFloat(0f, "_HitBlend", 0.1f);
                    }
                }
            }
        }
        
        private IEnumerator TestingEnableBack()
        {
            yield return new WaitForSeconds(0.25f);
            _isActive = false;
            foreach (var renderer in _hitEffectRenderers)
            {
                if (renderer != null)
                    renderer.enabled = false;
            }
            yield return new WaitForSeconds(2f);
            foreach (var renderer in _hitEffectRenderers)
            {
                if (renderer != null)
                    renderer.enabled = true;
            }
            _isActive = true;
            CurrentHealth = _maxHealth;
        }
        
        // IParryable implementation
        public bool IsInParryWindow { get; protected set; }
        
        public virtual Transform ParryTargetTransform => transform;
        
        public virtual void OnParried(Player player)
        {
            // Default behavior: take lethal damage
            TakeDamage(CurrentHealth);
            
            // Play hit effects
            PlayHomingHitEffects();
            
            // Play optional hit effect
            if (_hitEffect != null)
            {
                _hitEffect.Play(true);
            }
            
            // Apply hit material effect
            StartCoroutine(OnHitMaterialEffect());
            
            // Start testing enable back coroutine with punch scale effect
            transform.DOPunchScale(Vector3.one, 0.25f, 10, 1);
        }
        
        public virtual void OnParryWindowOpen()
        {
            IsInParryWindow = true;
        }
        
        public virtual void OnParryWindowClose()
        {
            IsInParryWindow = false;
        }
        
        /// <summary>
        /// The transform that should be used as the homing attack target.
        /// Default to this transform, but can be overridden for specific targeting points.
        /// </summary>
        public virtual Transform TargetTransform => transform;
        
        /// <summary>
        /// Called when the player successfully hits this enemy with a homing attack.
        /// Default implementation applies lethal damage, but can be overridden for custom behaviors.
        /// </summary>
        /// <param name="player">Player that hit the enemy</param>
        public virtual void OnHomingHit(Player player)
        {
            // Default behavior: homing attack is lethal
            TakeDamage(50);
            
            // Play hit effects
            PlayHomingHitEffects();
            
            // Play optional hit effect
            if (_hitEffect != null)
            {
                _hitEffect.Play(true);
            }
            
            // Apply hit material effect
            StartCoroutine(OnHitMaterialEffect());

            // Start testing enable back coroutine with punch scale effect
            transform.DOPunchScale(Vector3.one, 0.25f, 10, 1);
        }
        
        /// <summary>
        /// Called when the hit stop animation ends (after homing attack impact).
        /// Can be overridden for custom behaviors.
        /// </summary>
        /// <param name="player">Player that hit the enemy</param>
        public virtual void OnHitStopEnd(Player player)
        {
            // Default implementation does nothing
            // Override for custom behaviors like launching the player or special effects
        }
        
        /// <summary>
        /// Called when health reaches zero.
        /// Handles death effects and cleanup.
        /// </summary>
        public override void OnDeath()
        {
            //_isActive = false;
            StartCoroutine(TestingEnableBack());

            // Play death effects
            PlayDeathEffects();

            // Publish particle collection command
            _commandPublisher.PublishAsync(new PressureCollectParticleCommand(transform.position));

            // Handle destruction
            // if (_destroyOnDeath)
            // {
            //     Destroy(gameObject);
            // }
            // else
            // {
            //     // Just disable instead of destroy
            //     gameObject.SetActive(false);
            // }
        }
        
        /// <summary>
        /// Play effects when hit by homing attack
        /// </summary>
        protected virtual void PlayHomingHitEffects()
        {
            // Override in derived classes for custom hit effects
        }
        
        /// <summary>
        /// Play effects when enemy dies
        /// </summary>
        protected virtual void PlayDeathEffects()
        {
            if (_deathEffectPrefab != null)
            {
                GameObject effect = Instantiate(_deathEffectPrefab, transform.position, transform.rotation);
                if (_deathEffectDuration > 0f)
                {
                    Destroy(effect, _deathEffectDuration);
                }
            }
        }
        
        /// <summary>
        /// Reset enemy to active state (for respawning or reusing)
        /// </summary>
        public virtual void ResetEnemy()
        {
            _isActive = true;
            CurrentHealth = _maxHealth;
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// Disable enemy without destroying (for temporary deactivation)
        /// </summary>
        public virtual void DisableEnemy()
        {
            _isActive = false;
            gameObject.SetActive(false);
        }
    }
}