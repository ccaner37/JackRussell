using UnityEngine;
using JackRussell.States.Action;

namespace JackRussell.Enemies
{
    /// <summary>
    /// Base class for all enemy entities.
    /// Inherits from GameEntity and implements HomingTarget to be targetable by player homing attacks.
    /// </summary>
    public abstract class Enemy : GameEntity, IHomingTarget
    {
        [Header("Enemy Settings")]
        [SerializeField] protected bool _isActive = true;
        [SerializeField] protected bool _destroyOnDeath = true;
        [SerializeField] protected GameObject _deathEffectPrefab;
        [SerializeField] protected float _deathEffectDuration = 2f;
        
        protected bool IsEnemyActive => _isActive;
        
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
            TakeDamage(CurrentHealth);
            
            // Play hit effects
            PlayHomingHitEffects();
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
            _isActive = false;
            
            // Play death effects
            PlayDeathEffects();
            
            // Handle destruction
            if (_destroyOnDeath)
            {
                Destroy(gameObject);
            }
            else
            {
                // Just disable instead of destroy
                gameObject.SetActive(false);
            }
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