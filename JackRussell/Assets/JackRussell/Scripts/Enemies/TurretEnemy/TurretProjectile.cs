using UnityEngine;

namespace JackRussell.Enemies
{
    /// <summary>
    /// Laser projectile fired by turret enemies.
    /// Travels in a straight line and damages the player on contact.
    /// </summary>
    public class TurretProjectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private float _speed = 30f;
        [SerializeField] private float _damage = 20f;
        [SerializeField] private float _lifetime = 5f;
        [SerializeField] private LayerMask _collisionLayerMask;
        [SerializeField] private GameObject _impactEffectPrefab;
        [SerializeField] private float _impactEffectDuration = 1f;
        
        [Header("Visual")]
        [SerializeField] private TrailRenderer _trailRenderer;
        [SerializeField] private ParticleSystem _particleSystem;
        
        private Vector3 _direction;
        private float _spawnTime;
        private bool _hasHit = false;
        
        public float Damage => _damage;
        
        /// <summary>
        /// Initialize the projectile with direction and damage
        /// </summary>
        /// <param name="direction">Direction to travel</param>
        /// <param name="damage">Damage to deal (overrides base damage)</param>
        public void Initialize(Vector3 direction, float damage = -1f)
        {
            _direction = direction.normalized;
            _direction.y *= 0.5f;
            if (damage > 0f)
            {
                _damage = damage;
            }
            _spawnTime = Time.time;
            _hasHit = false;
            
            // Enable visual effects
            if (_trailRenderer != null)
            {
                _trailRenderer.enabled = true;
                _trailRenderer.Clear();
            }
            
            if (_particleSystem != null)
            {
                _particleSystem.Play();
            }
        }
        
        private void Update()
        {
            if (_hasHit) return;
            
            // Move the projectile
            transform.position += _direction * _speed * Time.deltaTime;
            
            // Check lifetime
            if (Time.time - _spawnTime > _lifetime)
            {
                DestroyProjectile();
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (_hasHit) return;
            
            // Check if we hit the player
            if (other.TryGetComponent<Player>(out Player player))
            {
                HitPlayer(player);
            }
            // Check if we hit other objects
            else if (((1 << other.gameObject.layer) & _collisionLayerMask) != 0)
            {
                HitObject(other);
            }
        }
        
        private void HitPlayer(Player player)
        {
            if (_hasHit) return;
            
            _hasHit = true;
            
            // Apply damage to player's pressure
            player.SetPressure(player.Pressure - _damage);
            
            // Create impact effect
            CreateImpactEffect();
            
            // Destroy projectile
            DestroyProjectile();
        }
        
        private void HitObject(Collider other)
        {
            if (_hasHit) return;
            
            _hasHit = true;
            
            // Create impact effect
            CreateImpactEffect();
            
            // Destroy projectile
            DestroyProjectile();
        }
        
        private void CreateImpactEffect()
        {
            if (_impactEffectPrefab != null)
            {
                GameObject effect = Instantiate(_impactEffectPrefab, transform.position, transform.rotation);
                if (_impactEffectDuration > 0f)
                {
                    Destroy(effect, _impactEffectDuration);
                }
            }
        }
        
        private void DestroyProjectile()
        {
            // Disable visual effects before destroying
            if (_trailRenderer != null)
            {
                _trailRenderer.enabled = false;
            }
            
            if (_particleSystem != null)
            {
                _particleSystem.Stop();
            }
            
            // Destroy the projectile
            Destroy(gameObject);
        }
        
        private void OnDisable()
        {
            // Clean up when disabled
            _hasHit = true;
            
            if (_trailRenderer != null)
            {
                _trailRenderer.enabled = false;
            }
            
            if (_particleSystem != null)
            {
                _particleSystem.Stop();
            }
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure we have valid layer mask
            if (_collisionLayerMask == 0)
            {
                // Default to everything except projectiles and enemies
                _collisionLayerMask = Physics.DefaultRaycastLayers & ~((1 << gameObject.layer) | (1 << 9)); // Assuming layer 9 is projectiles
            }
        }
        
        private void Reset()
        {
            // Auto-find components
            _trailRenderer = GetComponent<TrailRenderer>();
            _particleSystem = GetComponent<ParticleSystem>();
        }
#endif
    }
}