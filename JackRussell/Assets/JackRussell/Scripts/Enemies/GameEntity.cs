using UnityEngine;

namespace JackRussell.Enemies
{
    /// <summary>
    /// Base class for all game entities that share common mechanics like health/damage.
    /// This provides a foundation for both Player and Enemy classes to share common functionality.
    /// </summary>
    public abstract class GameEntity : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] protected float _maxHealth = 100f;
        [SerializeField] protected float _currentHealth;
        
        public virtual float MaxHealth => _maxHealth;
        public virtual float CurrentHealth 
        { 
            get => _currentHealth; 
            protected set => _currentHealth = Mathf.Clamp(value, 0f, _maxHealth); 
        }
        
        public abstract bool IsActive { get; }
        
        protected virtual void Awake()
        {
            _currentHealth = _maxHealth;
        }
        
        /// <summary>
        /// Apply damage to this entity
        /// </summary>
        /// <param name="amount">Amount of damage to take</param>
        public virtual void TakeDamage(float amount)
        {
            if (!IsActive) return;
            
            CurrentHealth -= amount;
            
            if (CurrentHealth <= 0f)
            {
                OnDeath();
            }
        }
        
        /// <summary>
        /// Heal this entity
        /// </summary>
        /// <param name="amount">Amount of health to restore</param>
        public virtual void Heal(float amount)
        {
            if (!IsActive) return;
            
            CurrentHealth += amount;
        }
        
        /// <summary>
        /// Called when health reaches zero
        /// </summary>
        public abstract void OnDeath();
    }
}