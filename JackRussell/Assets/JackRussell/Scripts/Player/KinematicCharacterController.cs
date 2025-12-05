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
        [SerializeField] private LayerMask _wallMask;

        [Header("Movement")]
        private float _gravity = -9.8f;
        [SerializeField] private float _rotationSpeed = 8f;
        [SerializeField] private float _skinWidth = 0.02f; // Minimum distance to maintain from walls

        // Runtime state
        private Vector3 _velocity;
        private bool _isGrounded = true;
        private Vector3 _groundNormal = Vector3.up;
        private Vector3 _groundPoint;
        private float _groundDistance;
        private Transform _transform;
        private CapsuleCollider _capsule;
        private Vector3 _capsulePoint1, _capsulePoint2;

        // Rotation override
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

            _capsule = GetComponent<CapsuleCollider>();
            if (_capsule != null)
            {
                Vector3 center = _capsule.center;
                float halfHeight = _capsule.height / 2 - _capsule.radius;
                _capsulePoint1 = center + Vector3.up * halfHeight;
                _capsulePoint2 = center - Vector3.up * halfHeight;
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            UpdateTimers(deltaTime);
            UpdateGroundDetection();
            UpdateRotation(deltaTime);
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
            if (_player.IsJumping)
            {
                _isGrounded = false;
            }
            else
            {
                _isGrounded = CheckGroundMultiRay();
            }

            if (Physics.Raycast(_transform.position + Vector3.up * 0.7f, -Vector3.up, out var hit, 5f, _groundMask, QueryTriggerInteraction.Ignore))
            {
                _groundNormal = hit.normal;
                _groundPoint = hit.point;
                _groundDistance = hit.distance - 0.05f;
            }
            else
            {
                _groundNormal = Vector3.up;
                _groundDistance = float.MaxValue;
            }
        }

    private bool CheckGroundMultiRay()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        float maxDistance = 0.6f;
        
        // Center ray
        if (Physics.Raycast(origin, Vector3.down, maxDistance, _groundMask, QueryTriggerInteraction.Ignore))
            return true;
        
        // 4 corner rays around the character
        float offset = 0.3f; // Adjust based on your character size
        Vector3[] offsets = new Vector3[]
        {
            new Vector3(offset, 0, 0),
            new Vector3(-offset, 0, 0),
            new Vector3(0, 0, offset),
            new Vector3(0, 0, -offset)
        };
        
        foreach (var off in offsets)
        {
            if (Physics.Raycast(origin + off, Vector3.down, maxDistance, _groundMask, QueryTriggerInteraction.Ignore))
                return true;
        }
        
        return false;
    }

        private void ApplyMovement(float deltaTime)
        {
            if (!_isGrounded)
            {
                _velocity.y += _gravity * deltaTime;
                Vector3 movement = _velocity * deltaTime;
                
                // Separate horizontal and vertical movement for falling
                Vector3 horizontalMovement = new Vector3(movement.x, 0, movement.z);
                Vector3 verticalMovement = new Vector3(0, movement.y, 0);
                
                // Handle horizontal collision
                horizontalMovement = HandleCollision(horizontalMovement, false);
                
                // Handle vertical collision (for landing)
                verticalMovement = HandleCollision(verticalMovement, true);
                
                // If vertical movement was stopped by ground, zero out vertical velocity
                if (verticalMovement.y > movement.y * 0.1f && movement.y < 0)
                {
                    _velocity.y = 0;
                }
                
                _transform.position += horizontalMovement + verticalMovement;
                return;
            }

            // Ground movement
            Vector3 slopeMove = Vector3.ProjectOnPlane(_velocity, _groundNormal);
            if (_velocity.sqrMagnitude < 0.001f) return;
            Vector3 intendedMovement = slopeMove * deltaTime;

            // Handle collision
            intendedMovement = HandleCollision(intendedMovement, false);

            // Apply movement
            transform.position += intendedMovement;
        }

        private Vector3 HandleCollision(Vector3 movement, bool isVertical)
        {
            if (_capsule == null || movement.sqrMagnitude < 0.0001f) return movement;

            Vector3 currentPos = _transform.position;
            float radius = _capsule.radius;
            Vector3 direction = movement.normalized;
            float distance = movement.magnitude;
            Vector3 remainingMovement = movement;

            // Multiple iterations for sliding
            for (int i = 0; i < 3; i++)
            {
                if (distance < 0.001f) break;

                // Update capsule points with current position
                Vector3 point1 = currentPos + _transform.rotation * _capsulePoint1;
                Vector3 point2 = currentPos + _transform.rotation * _capsulePoint2;

                if (Physics.CapsuleCast(point1, point2, radius, direction, out RaycastHit hit, distance + _skinWidth, _wallMask, QueryTriggerInteraction.Ignore))
                {
                    // Calculate safe movement distance
                    float safeDistance = Mathf.Max(0, hit.distance - _skinWidth);
                    
                    if (safeDistance < 0.001f)
                    {
                        // Too close to surface, stop movement
                        remainingMovement = Vector3.zero;
                        break;
                    }

                    // Move to safe distance
                    Vector3 safeMovement = direction * safeDistance;
                    currentPos += safeMovement;
                    remainingMovement -= safeMovement;

                    // For vertical movement (falling), don't slide
                    if (isVertical)
                    {
                        break;
                    }

                    // Calculate slide direction
                    Vector3 slideDirection = Vector3.ProjectOnPlane(remainingMovement, hit.normal).normalized;
                    float slideDistance = remainingMovement.magnitude;

                    // Prevent sliding into the surface
                    if (Vector3.Dot(slideDirection, hit.normal) < -0.01f)
                    {
                        break;
                    }

                    // Update for next iteration
                    direction = slideDirection;
                    distance = slideDistance;
                    remainingMovement = slideDirection * slideDistance;
                }
                else
                {
                    // No collision, move freely
                    currentPos += remainingMovement;
                    remainingMovement = Vector3.zero;
                    break;
                }
            }

            return currentPos - _transform.position;
        }

        private void UpdateRotation(float deltaTime)
        {
            if (_hasRotationOverride && _rotationOverrideExclusive)
            {
                _transform.rotation = Quaternion.Lerp(
                    _transform.rotation,
                    _rotationOverrideTarget,
                    _rotationSpeed * deltaTime
                );
                return;
            }

            Quaternion targetRotation = CalculateGroundAlignmentRotation();

            _transform.rotation = Quaternion.Lerp(
                _transform.rotation,
                targetRotation,
                _rotationSpeed * deltaTime
            );
        }

        private Quaternion CalculateGroundAlignmentRotation()
        {
            Vector3 velocityDirection = _velocity;
            velocityDirection.y = 0f; 

            Vector3 targetForward = velocityDirection;
            if (targetForward.sqrMagnitude < 0.01f)
            {
                targetForward = _transform.forward;
                targetForward.y = 0f;
            }
            
            if (targetForward.sqrMagnitude > 0.01f)
            {
                targetForward.Normalize();
            }
            else
            {
                targetForward = Vector3.forward; 
            }

            Vector3 slopeForward = Vector3.ProjectOnPlane(targetForward, _groundNormal).normalized;
            return Quaternion.LookRotation(slopeForward, _groundNormal);
        }

        // Rotation override API
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

        public void SetRotationInstant(Quaternion rotation)
        {
            _transform.rotation = rotation;
        }

        // Debug visualization
        private void OnDrawGizmos()
        {
            float castRadius = 0.25f;
            float castDistance = 1f;
            Vector3 castDirection = Vector3.down;
            Vector3 castOrigin = transform.position + (Vector3.up * 1);

            Gizmos.color = _isGrounded ? Color.green : Color.red;

            // Start sphere
            Gizmos.DrawWireSphere(castOrigin, castRadius);

            // End sphere
            Vector3 endPosition = castOrigin + (castDirection * castDistance);
            Gizmos.DrawWireSphere(endPosition, castRadius);
            
            // Draw skin width for debugging
            if (_capsule != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 point1 = transform.position + _capsulePoint1;
                Vector3 point2 = transform.position + _capsulePoint2;
                Gizmos.DrawWireSphere(point1, _capsule.radius + _skinWidth);
                Gizmos.DrawWireSphere(point2, _capsule.radius + _skinWidth);
            }
        }
    }
}