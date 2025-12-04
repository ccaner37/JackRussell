using UnityEngine;

namespace JackRussell
{
    /// <summary>
    /// Simplified kinematic character controller for Sonic-style gameplay.
    /// Always sticks to ground, moves based on yaw/pitch rotation.
    /// </summary>
    [RequireComponent(typeof(Transform))]
    public class KinematicCharacterController : MonoBehaviour
    {
        [SerializeField] private Player _player;

        [Header("Ground Detection")]
        [SerializeField] private float _groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask _groundMask;

        [Header("Collision")]
        [SerializeField] private float _collisionRadius = 0.4f;

        [Header("Movement")]
        private float _gravity = -9.8f;
        [SerializeField] private float _rotationSpeed = 8f; // Increased for more responsive rotation

        // Runtime state
        private Vector3 _velocity;
        private bool _isGrounded = true;
        private Vector3 _groundNormal = Vector3.up;
        private Vector3 _groundPoint;
        private float _groundDistance;
        private Transform _transform;

        // Movement is handled through velocity modifications from states

        // Rotation override (for compatibility with Player.cs)
        private bool _hasRotationOverride;
        private Quaternion _rotationOverrideTarget = Quaternion.identity;
        private float _rotationOverrideTimer;
        private bool _rotationOverrideExclusive;

        // Properties
        public Vector3 Velocity => _velocity;
        public bool IsGrounded => _isGrounded;
        public Vector3 GroundNormal => _groundNormal;
        public Vector3 GroundPoint => _groundPoint;
        public float GroundDistance => _groundDistance;

        private void Awake()
        {
            _transform = transform;
            _velocity = Vector3.zero;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            // Update timers
            UpdateTimers(deltaTime);

            UpdateGroundDetection();

            UpdateRotation(deltaTime);

            if (_isGrounded)
            {
                //SnapToGround();
            }

            ApplyMovement(deltaTime);
        }

        private void UpdateTimers(float deltaTime)
        {
            if (_hasRotationOverride)
            {
                _rotationOverrideTimer -= deltaTime;
                if (_rotationOverrideTimer <= 0f)
                {
                    _hasRotationOverride = false;
                }
            }
        }

        private void UpdateGroundDetection()
        {
            if (!_player.IsJumping)
            {
                if (Physics.SphereCast(transform.position + Vector3.up, 0.35f, Vector3.down, out var sphereHit, 1f, _groundMask))
                {
                    _isGrounded = true;
                }
                else
                {
                    _isGrounded = false;
                }
            }
            else
            {
                _isGrounded = false;
            }


            if (Physics.Raycast(_transform.position + Vector3.up * 0.5f, -Vector3.up, out var hit, 1f, _groundMask, QueryTriggerInteraction.Ignore))
            {
                _groundNormal = hit.normal;
                _groundPoint = hit.point;
                _groundDistance = hit.distance - 0.05f; // Adjust for offset
                //_isGrounded = true;
            }
            else
            {
                _groundNormal = Vector3.up;
                _groundDistance = float.MaxValue;
                //_isGrounded = false;
            }
        }


        private void ApplyMovement(float deltaTime)
        {
            if (!_isGrounded)
            {
                _velocity.y += _gravity * deltaTime;
                Vector3 movement = _velocity * deltaTime;
                _transform.position += movement;
                return;
            }

            // Ground movement - follow surface contours with collision detection
            // Project velocity onto ground plane to maintain speed on slopes
            
            Vector3 slopeMove = Vector3.ProjectOnPlane(_velocity, _groundNormal);
            if (_velocity.sqrMagnitude < 0.001f) return;
            Vector3 intendedMovement = slopeMove * deltaTime;

            // Use swept collision to prevent tunneling
            transform.position += intendedMovement;
        }

        private void SnapToGround()
        {
            // Set position directly to the ground surface
            _transform.position = _groundPoint;
        }

        private void UpdateRotation(float deltaTime)
        {
            // For surface-following, ground alignment takes priority
            // Only apply rotation overrides for special cases (jumping, actions, etc.)
            if (_hasRotationOverride && _rotationOverrideExclusive)
            {
                _transform.rotation = Quaternion.Lerp(
                    _transform.rotation,
                    _rotationOverrideTarget,
                    _rotationSpeed * deltaTime
                );
                return;
            }

            //if (!_isGrounded) return;

            // Primary: Align with ground normal for surface following
            Quaternion targetRotation = CalculateGroundAlignmentRotation();

            // Apply rotation with higher speed for more responsive feel
            _transform.rotation = Quaternion.Lerp(
                _transform.rotation,
                targetRotation,
                _rotationSpeed * deltaTime
            );
        }

            private Quaternion CalculateGroundAlignmentRotation()
            {
                // 1. Get the Raw Horizontal Direction (Pure Yaw)
                Vector3 velocityDirection = _velocity;
                velocityDirection.y = 0f; 

                // Handle zero velocity / Fallback
                Vector3 targetForward = velocityDirection;
                if (targetForward.sqrMagnitude < 0.01f)
                {
                    targetForward = _transform.forward;
                    targetForward.y = 0f;
                }
                
                // Normalize the horizontal direction
                if (targetForward.sqrMagnitude > 0.01f)
                {
                    targetForward.Normalize();
                }
                else
                {
                    targetForward = Vector3.forward; 
                }

                // 2. THE FIX: Project the Horizontal Forward onto the Ground Plane
                // This takes your flat forward vector and tilts it to match the slope
                Vector3 slopeForward = Vector3.ProjectOnPlane(targetForward, _groundNormal).normalized;

                // 3. Create the rotation
                // Now both the Forward vector (slopeForward) and Up vector (_groundNormal) are aligned
                return Quaternion.LookRotation(slopeForward, _groundNormal);
            }


        // Rotation override API (for compatibility with Player.cs)
        public void RequestRotationOverride(Quaternion target, float duration, bool exclusive = true)
        {
            _hasRotationOverride = true;
            _rotationOverrideTarget = target;
            _rotationOverrideTimer = duration;
            _rotationOverrideExclusive = exclusive;
        }

        public void ClearRotationOverride()
        {
            _hasRotationOverride = false;
            _rotationOverrideTimer = 0f;
        }

        public bool HasRotationOverride() => _hasRotationOverride;
        public Quaternion GetRotationOverride() => _rotationOverrideTarget;
        public bool IsRotationOverrideExclusive() => _rotationOverrideExclusive;

        // Legacy API compatibility
        public void SetVelocity(Vector3 velocity)
        {
            _velocity = velocity;
        }

        public void AddForce(Vector3 force, ForceMode mode = ForceMode.Acceleration)
        {
            switch (mode)
            {
                case ForceMode.Acceleration:
                    _velocity += force * Time.fixedDeltaTime;
                    break;
                case ForceMode.Impulse:
                case ForceMode.VelocityChange:
                    _velocity += force;
                    break;
            }
        }

        public void MovePosition(Vector3 position)
        {
            _transform.position = position;
        }

        public void MoveRotation(Quaternion rotation)
        {
            _transform.rotation = rotation;
        }

        // Debug visualization
        private void OnDrawGizmos()
        {
// Senin koddaki değerlerin aynısı:
    float castRadius = 0.5f;
    float castDistance = 0.5f;
    Vector3 castDirection = Vector3.down;
    Vector3 castOrigin = transform.position + (Vector3.up * 0.1f);

    // Yerdeysek Yeşil, Havadaysak Kırmızı olsun
    Gizmos.color = _isGrounded ? Color.green : Color.red;

    // 1. BAŞLANGIÇ KÜRESİ (Origin)
    // SphereCast'in başladığı yer
    Gizmos.DrawWireSphere(castOrigin, castRadius);

    // 2. BİTİŞ KÜRESİ (Max Distance)
    // SphereCast'in ulaşacağı en uzak nokta
    Vector3 endPosition = castOrigin + (castDirection * castDistance);
    Gizmos.DrawWireSphere(endPosition, castRadius);
        }
    }
}