using UnityEngine;
using JackRussell.States;
using JackRussell.Audio;
using VContainer;
using System.Collections;

namespace JackRussell.Enemies
{
    /// <summary>
    /// Turret enemy that detects player, tracks them, and fires laser projectiles.
    /// Uses a state machine to manage behavior phases: Idle -> Detecting -> Targeting -> Preparing -> Firing -> Cooldown.
    /// Can be destroyed by player homing attacks and parry attacks.
    /// </summary>
    public class TurretEnemy : Enemy, IParryable
    {
        [Header("Turret Components")]
        [SerializeField] private Transform _headTransform;
        [SerializeField] private Transform _firePoint;
        [SerializeField] private TurretGlowEffect _glowEffect;
        [SerializeField] private GameObject _laserProjectilePrefab;
        [SerializeField] private AudioSource _audioSource;
        
        [Header("Detection Settings")]
        [SerializeField] private float _detectionRadius = 20f;
        [SerializeField] private LayerMask _playerLayerMask = -1;
        [SerializeField] private bool _showDetectionGizmo = false;
        
        [Header("Combat Settings")]
        [SerializeField] private float _rotationSpeed = 90f; // Degrees per second
        [SerializeField] private float _preparationTime = 2f;
        [SerializeField] private float _cooldownTime = 3f;
        [SerializeField] private float _laserDamage = 20f;
        [SerializeField] private float _laserSpeed = 30f;
        [SerializeField] private float _maxVerticalAngle = 45f;
        [SerializeField] private float _maxHorizontalAngle = 180f;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject _detectionEffectPrefab;
        [SerializeField] private GameObject _chargingEffectPrefab;
        [SerializeField] private GameObject _firingEffectPrefab;
        [SerializeField] private GameObject _deathEffectPrefab;
        
        [Header("Audio")]
        [SerializeField] private SoundType _detectionSound = SoundType.None;
        [SerializeField] private SoundType _targetingSound = SoundType.None;
        [SerializeField] private SoundType _chargingSound = SoundType.None;
        [SerializeField] private SoundType _firingSound = SoundType.None;
        [SerializeField] private SoundType _deathSound = SoundType.None;
        
        [Header("Parry Settings")]
        [SerializeField] private float _parryTime = 0.65f; // Time before preparation ends when parry window opens
        [SerializeField] private GameObject _parryWindowEffectPrefab;
        
        private StateMachine _stateMachine;
        private Player _targetedPlayer;
        private GameObject _detectionEffectInstance;
        private GameObject _chargingEffectInstance;
        private GameObject _firingEffectInstance;
        
        [Inject] private readonly AudioManager _audioManager;
        
        // Public getters for state access
        public Player TargetedPlayer => _targetedPlayer;
        public Transform HeadTransform => _headTransform;
        public Transform FirePoint => _firePoint;
        public float RotationSpeed => _rotationSpeed;
        public float PreparationTime => _preparationTime;
        public float ParryTime => _parryTime;
        public float CooldownTime => _cooldownTime;
        public float LaserDamage => _laserDamage;
        public float LaserSpeed => _laserSpeed;
        public GameObject LaserProjectilePrefab => _laserProjectilePrefab;
        
        public override Transform TargetTransform => _headTransform;
        
        public override bool IsActive => IsEnemyActive;
        
        // IParryable implementation
        public bool IsInParryWindow { get; private set; }
        
        public override Transform ParryTargetTransform => transform;
        
        public void OnParryWindowOpen()
        {
            IsInParryWindow = true;
        }
        
        public void OnParryWindowClose()
        {
            IsInParryWindow = false;
        }
        
        private void Awake()
        {
            // Initialize state machine
            _stateMachine = new StateMachine();
            
            // Initialize states
            var idleState = new TurretIdleState(this, _stateMachine);
            var detectingState = new TurretDetectingState(this, _stateMachine);
            var targetingState = new TurretTargetingState(this, _stateMachine);
            var preparingState = new TurretPreparingState(this, _stateMachine);
            var firingState = new TurretFiringState(this, _stateMachine);
            var cooldownState = new TurretCooldownState(this, _stateMachine);
            
            // Start in idle state
            _stateMachine.Initialize(idleState);
        }
        
        private void Update()
        {
            _stateMachine.LogicUpdate(Time.deltaTime);
        }
        
        private void FixedUpdate()
        {
            _stateMachine.PhysicsUpdate();
        }
        
        /// <summary>
        /// Detect if player is within range and set as target
        /// </summary>
        /// <returns>True if player detected</returns>
        public bool DetectPlayer()
        {
            Collider[] players = Physics.OverlapSphere(transform.position, _detectionRadius, _playerLayerMask);
            if (players.Length > 0)
            {
                Player newTarget = null;
                float closestDistance = float.MaxValue;
                
                // Find closest player
                foreach (var collider in players)
                {
                    if (collider.TryGetComponent<Player>(out Player player))
                    {
                        float distance = Vector3.Distance(transform.position, player.transform.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            newTarget = player;
                        }
                    }
                }
                
                _targetedPlayer = newTarget;
                return _targetedPlayer != null;
            }
            
            _targetedPlayer = null;
            return false;
        }
        
        /// <summary>
        /// Rotate turret head towards the targeted player
        /// </summary>
        public void RotateHeadTowardsPlayer()
        {
            if (_targetedPlayer == null || _headTransform == null) return;
            
            Vector3 directionToPlayer = _targetedPlayer.transform.position - _headTransform.position;
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
            
            // Apply rotation constraints
            Vector3 euler = targetRotation.eulerAngles;
            
            // Get current rotation relative to base
            Quaternion baseRotation = transform.rotation;
            Quaternion relativeRotation = Quaternion.Inverse(baseRotation) * targetRotation;
            Vector3 relativeEuler = relativeRotation.eulerAngles;
            
            // Clamp vertical angle
            float verticalAngle = NormalizeAngle(relativeEuler.x);
            verticalAngle = Mathf.Clamp(verticalAngle, -_maxVerticalAngle, _maxVerticalAngle);
            
            // Clamp horizontal angle
            float horizontalAngle = NormalizeAngle(relativeEuler.y);
            horizontalAngle = Mathf.Clamp(horizontalAngle, -_maxHorizontalAngle, _maxHorizontalAngle);
            
            // Apply constrained rotation
            Quaternion constrainedRotation = baseRotation * Quaternion.Euler(verticalAngle, horizontalAngle, 0f);
            
            // Smooth rotation
            _headTransform.rotation = Quaternion.RotateTowards(
                _headTransform.rotation, 
                constrainedRotation, 
                _rotationSpeed * Time.deltaTime
            );
        }
        
        /// <summary>
        /// Fire a laser projectile at the current target
        /// </summary>
        public void FireLaser()
        {
            if (_laserProjectilePrefab == null || _firePoint == null || _targetedPlayer == null) return;
            
            // Calculate direction to player
            Vector3 direction = (_targetedPlayer.transform.position - _firePoint.position).normalized;
            
            // Create projectile
            GameObject projectileObj = Instantiate(_laserProjectilePrefab, _firePoint.position, _firePoint.rotation);
            
            if (projectileObj.TryGetComponent<TurretProjectile>(out TurretProjectile projectile))
            {
                projectile.Initialize(direction, _laserDamage);
            }
            else
            {
                // Fallback if no TurretProjectile component
                projectileObj.transform.forward = direction;
            }
        }
        
        // Audio methods for states
        public void PlayDetectionSound() => PlaySound(_detectionSound);
        public void PlayTargetingSound() => PlaySound(_targetingSound);
        public void PlayChargingSound() => PlaySound(_chargingSound);
        public void PlayFiringSound() => PlaySound(_firingSound);
        public void PlayCooldownSound() => PlaySound(_deathSound);
        
        public void PlayParryWindowSound()
        {
            _audioManager.PlaySound(SoundType.ParryIndicator, _audioSource);
        }
        
        public void EnableParryWindowEffects()
        {
            if (_parryWindowEffectPrefab != null)
            {
                GameObject effect = Instantiate(_parryWindowEffectPrefab, transform.position, transform.rotation);
                if (_deathEffectDuration > 0f)
                {
                    Destroy(effect, 0.25f); // Match parry window duration
                }
            }
        }
        
        private void PlaySound(SoundType soundType)
        {
            if (soundType != SoundType.None && _audioManager != null && _audioSource != null)
            {
                _audioManager.PlaySound(soundType, _audioSource);
            }
        }
        
        // Visual effect methods for states
        public void EnableDetectionEffects()
        {
            if (_detectionEffectPrefab != null)
            {
                _detectionEffectInstance = Instantiate(_detectionEffectPrefab, transform.position, transform.rotation, transform);
            }
        }
        
        public void DisableDetectionEffects()
        {
            if (_detectionEffectInstance != null)
            {
                Destroy(_detectionEffectInstance);
                _detectionEffectInstance = null;
            }
        }
        
        public void EnableChargingEffects()
        {
            if (_chargingEffectPrefab != null)
            {
                _chargingEffectInstance = Instantiate(_chargingEffectPrefab, transform.position, transform.rotation, transform);
            }
        }
        
        public void DisableChargingEffects()
        {
            if (_chargingEffectInstance != null)
            {
                Destroy(_chargingEffectInstance);
                _chargingEffectInstance = null;
            }
        }
        
        public void EnableFiringEffects()
        {
            if (_firingEffectPrefab != null)
            {
                _firingEffectInstance = Instantiate(_firingEffectPrefab, _firePoint.position, _firePoint.rotation, _firePoint);
            }
        }
        
        public void DisableFiringEffects()
        {
            if (_firingEffectInstance != null)
            {
                Destroy(_firingEffectInstance);
                _firingEffectInstance = null;
            }
        }
        
        public void EnableCooldownEffects()
        {
            // Could add specific cooldown visual effects here
        }
        
        public void DisableCooldownEffects()
        {
            // Could disable cooldown visual effects here
        }
        
        // Glow effect methods
        public void StartGlowEffect()
        {
            if (_glowEffect != null)
            {
                _glowEffect.StartGlow(_preparationTime, _parryTime);
            }
        }
        
        public void StopGlowEffect()
        {
            if (_glowEffect != null)
            {
                _glowEffect.StopGlow();
            }
        }

        // Override homing hit behavior
        public override void OnHomingHit(Player player)
        {
            // Play hit effects
            base.OnHomingHit(player);

            // // Create explosion effect
            // if (_deathEffectPrefab != null)
            // {
            //     GameObject effect = Instantiate(_deathEffectPrefab, transform.position, transform.rotation);
            //     Destroy(effect, 2f);
            // }

            // Play death sound
            //PlaySound(_deathSound);
        }

        public override void OnDeath()
        {
            base.OnDeath();
            _stateMachine.ChangeState(new TurretCooldownState(this, _stateMachine));
        }
        
        private float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!_showDetectionGizmo) return;
            
            // Draw detection radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);
            
            // Draw line to target
            if (_targetedPlayer != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(_headTransform.position, _targetedPlayer.transform.position);
            }
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure we have valid references
            if (_headTransform == null)
                _headTransform = transform;
            
            if (_firePoint == null)
                _firePoint = transform;
            
            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();
            
            if (_glowEffect == null)
                _glowEffect = GetComponentInChildren<TurretGlowEffect>();
        }
        
        private void Reset()
        {
            // Auto-find components when added
            _headTransform = transform;
            _firePoint = transform;
            _audioSource = GetComponent<AudioSource>();
            _glowEffect = GetComponentInChildren<TurretGlowEffect>();
        }
#endif
    }
}