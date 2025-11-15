using UnityEngine;
using DG.Tweening;

namespace JackRussell
{
    /// <summary>
    /// Manages tentacle idle animations and future aiming functionality.
    /// Provides a clean separation between animation logic and state machine.
    /// Features advanced camera-responsive idle animations and position-based aiming.
    /// </summary>
    public class TentacleController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _tentacleTarget; // The transform that the IK rig follows
        [SerializeField] private Player _player;            // Reference to player for camera access
        
        [Header("Idle Animation Settings")]
        [SerializeField] private float _idleAmplitude = 0.1f;     // How much the tentacle sways
        [SerializeField] private float _idleFrequency = 1.5f;     // How fast it sways
        [SerializeField] private Vector3 _idleAxis = new Vector3(0f, 0.1f, 0.05f); // Sway direction per axis
        [SerializeField] private float _idleTransitionDuration = 0.5f; // Smooth transition in/out
        
        [Header("Camera-Responsive Settings")]
        [SerializeField] private bool _isCameraResponsive = true;  // Enable/disable camera responsiveness
        [SerializeField] private float _cameraResponsiveStrength = 0.7f; // How much camera affects idle animation
        [SerializeField] private float _cameraLerpSpeed = 5f;       // How smoothly it follows camera direction
        [SerializeField] private float _cameraResponsiveRange = 1.0f; // Range of movement from camera influence
        
        [Header("Aiming Settings (Future Use)")]
        [SerializeField] private float _aimSpeed = 10f;           // How fast it can move toward targets
        [SerializeField] private float _aimLerpSpeed = 5f;        // Smoothness of aim transitions
        
        // Private state
        private bool _isIdleAnimating = false;
        private bool _isAiming = false;
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private Vector3 _basePosition;
        private Vector3 _currentAimPosition;
        private Vector3 _cameraBasedOffset;
        private Vector3 _lastCameraDirection;
        private Tween _idleTween;
        private Tween _positionTween;
        
        // Current position tracking for proper combination
        private Vector3 _currentIdlePosition;
        private Vector3 _lastAppliedPosition;
        
        // Public properties for external access
        public bool IsIdleAnimating => _isIdleAnimating;
        public bool IsAiming => _isAiming;
        public Transform TentacleTarget => _tentacleTarget;
        public Vector3 CurrentAimPosition => _currentAimPosition;
        public bool IsCameraResponsive => _isCameraResponsive;
        
        private void Awake()
        {
            // Store original transform state
            if (_tentacleTarget != null)
            {
                _originalPosition = _tentacleTarget.localPosition;
                _originalRotation = _tentacleTarget.localRotation;
                _basePosition = _originalPosition;
                _currentAimPosition = _originalPosition;
                _currentIdlePosition = _originalPosition;
                _lastAppliedPosition = _originalPosition;
            }
            
            // Initialize last camera direction
            _lastCameraDirection = Vector3.forward;
        }
        
        /// <summary>
        /// Start the idle animation (swaying/hovering effect)
        /// </summary>
        public void StartIdleAnimation()
        {
            if (_tentacleTarget == null || _isIdleAnimating)
                return;
                
            // Stop any existing animations
            StopIdleAnimation();
            StopAiming();
            
            _isIdleAnimating = true;
            
            // Reset to base position first
            _tentacleTarget.localPosition = _basePosition;
            _currentIdlePosition = _basePosition;
            _lastAppliedPosition = _basePosition;
            
            // Create the idle sway animation using DOTween
            _idleTween = CreateIdleSwayTween();
        }
        
        /// <summary>
        /// Stop the idle animation and return to base position
        /// </summary>
        public void StopIdleAnimation()
        {
            if (!_isIdleAnimating)
                return;
                
            _isIdleAnimating = false;
            
            // Kill the current tween
            if (_idleTween != null)
            {
                _idleTween.Kill();
                _idleTween = null;
            }
            
            // Reset positions
            _currentIdlePosition = _basePosition;
            _cameraBasedOffset = Vector3.zero;
            
            // Smoothly return to base position
            _tentacleTarget.localPosition = _basePosition;
            _lastAppliedPosition = _basePosition;
        }
        
        /// <summary>
        /// Begin aiming at a position (future functionality)
        /// </summary>
        public void StartAiming(Vector3 targetPosition)
        {
            if (_tentacleTarget == null)
                return;
                
            // Stop idle animation
            StopIdleAnimation();
            
            _currentAimPosition = targetPosition;
            _isAiming = true;
        }
        
        /// <summary>
        /// Update the aim position (continuous aiming)
        /// </summary>
        public void UpdateAimPosition(Vector3 newTargetPosition)
        {
            if (!_isAiming)
                return;
                
            _currentAimPosition = newTargetPosition;
        }
        
        /// <summary>
        /// Stop aiming and optionally return to idle
        /// </summary>
        public void StopAiming(bool returnToIdle = false)
        {
            _isAiming = false;
            _currentAimPosition = _basePosition;
            
            if (returnToIdle)
            {
                StartIdleAnimation();
            }
        }
        
        /// <summary>
        /// Update the controller (called from external script)
        /// </summary>
        public void UpdateController()
        {
            if (_tentacleTarget == null)
                return;
                
            // Update camera-based offset if enabled
            if (_isCameraResponsive && _player != null)
            {
                UpdateCameraResponsiveOffset();
            }
            else
            {
                _cameraBasedOffset = Vector3.zero;
            }
                
            if (_isAiming)
            {
                // Move toward aim position
                MoveTowardAimPosition();
            }
            else if (_isIdleAnimating)
            {
                // Apply combined position (idle animation + camera responsiveness)
                ApplyCombinedPosition();
            }
        }
        
        /// <summary>
        /// Configure idle animation parameters
        /// </summary>
        public void ConfigureIdleAnimation(float amplitude, float frequency, Vector3 axis)
        {
            _idleAmplitude = amplitude;
            _idleFrequency = frequency;
            _idleAxis = axis;
            
            // If currently animating, restart with new parameters
            if (_isIdleAnimating)
            {
                StartIdleAnimation();
            }
        }
        
        /// <summary>
        /// Configure camera responsiveness
        /// </summary>
        public void ConfigureCameraResponsiveness(bool enable, float strength = 0.7f, float lerpSpeed = 5f, float range = 1.0f)
        {
            _isCameraResponsive = enable;
            _cameraResponsiveStrength = strength;
            _cameraLerpSpeed = lerpSpeed;
            _cameraResponsiveRange = range;
        }
        
        /// <summary>
        /// Reset to original transform state
        /// </summary>
        public void ResetToOriginalState()
        {
            StopIdleAnimation();
            StopAiming(false);
            
            if (_tentacleTarget != null)
            {
                _tentacleTarget.localPosition = _originalPosition;
                _tentacleTarget.localRotation = _originalRotation;
                _basePosition = _originalPosition;
                _currentAimPosition = _originalPosition;
                _cameraBasedOffset = Vector3.zero;
                _currentIdlePosition = _originalPosition;
                _lastAppliedPosition = _originalPosition;
            }
        }
        
        private Tween CreateIdleSwayTween()
        {
            // Create a smooth, looping sway animation using DOTween
            // We'll update our internal position tracking instead of directly moving the transform
            
            Vector3 startPos = _basePosition;
            Vector3 endPos = _basePosition + _idleAxis * _idleAmplitude;
            
            // Tween the internal position tracking
            Tween positionTween = DOTween.To(() => _currentIdlePosition, 
                                           x => _currentIdlePosition = x, 
                                           endPos, 1f / _idleFrequency)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .OnUpdate(() => 
                {
                    // When DOTween updates, we apply the combined position
                    ApplyCombinedPosition();
                });
                
            return positionTween;
        }
        
        private void UpdateCameraResponsiveOffset()
        {
            if (_player == null) return;
            
            // Get main camera
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;
            
            // Get camera position and player position
            Vector3 cameraPosition = mainCamera.transform.position;
            Vector3 playerPosition = _player.transform.position;
            
            // Calculate direction from player to camera (this is key!)
            Vector3 playerToCamera = cameraPosition - playerPosition;
            Vector3 horizontalPlayerToCamera = playerToCamera;
            horizontalPlayerToCamera.y = 0f; // Project onto horizontal plane
            
            // Get player's forward direction (for left/right calculation)
            Vector3 playerForward = _player.transform.forward;
            playerForward.y = 0f; // Project onto horizontal plane
            playerForward.Normalize();
            
            // Calculate left/right position of camera relative to player
            // Cross product gives us signed value: positive = right, negative = left
            float rightLeftValue = Vector3.Cross(playerForward, horizontalPlayerToCamera.normalized).y;
            
            // Calculate forward/backward position of camera relative to player
            float forwardBackwardValue = Vector3.Dot(playerForward, horizontalPlayerToCamera.normalized);
            
            // Calculate up/down position of camera relative to player
            float upDownValue = playerToCamera.y;
            
            // Create the target offset based on camera position
            Vector3 targetOffset = Vector3.zero;
            
            // Left/Right movement based on camera position
            // When camera is to the left of player, move tentacle left (negative X)
            // When camera is to the right of player, move tentacle right (positive X)
            if (Mathf.Abs(rightLeftValue) > 0.1f) // Threshold to prevent tiny movements
            {
                float lateralStrength = Mathf.Clamp01(Mathf.Abs(horizontalPlayerToCamera.magnitude) / 5f); // Scale by distance
                float horizontalOffset = rightLeftValue * _cameraResponsiveStrength * _cameraResponsiveRange * lateralStrength * 0.15f;
                targetOffset += new Vector3(horizontalOffset, 0f, 0f); // Only X-axis for left/right
            }
            
            // Forward/Backward movement (subtle, for depth)
            if (Mathf.Abs(forwardBackwardValue) > 0.1f)
            {
                float depthStrength = Mathf.Clamp01(Mathf.Abs(horizontalPlayerToCamera.magnitude) / 5f);
                float forwardOffset = forwardBackwardValue * _cameraResponsiveStrength * _cameraResponsiveRange * depthStrength * 0.05f;
                targetOffset += new Vector3(0f, 0f, forwardOffset);
            }
            
            // Up/Down movement based on camera height and angle
            if (Mathf.Abs(upDownValue) > 0.5f) // Only move vertically if camera is significantly above/below
            {
                float heightStrength = Mathf.Clamp01(Mathf.Abs(upDownValue) / 3f); // Scale by height difference
                float verticalOffset = Mathf.Sign(upDownValue) * _cameraResponsiveStrength * _cameraResponsiveRange * heightStrength * 0.08f;
                targetOffset += new Vector3(0f, verticalOffset, 0f);
            }
            
            // Apply smoothing to prevent jarring movements
            _cameraBasedOffset = Vector3.Lerp(_cameraBasedOffset, targetOffset, Time.deltaTime * _cameraLerpSpeed);
            
            // Store last camera direction for reference
            _lastCameraDirection = (cameraPosition - playerPosition).normalized;
        }
        
        private void ApplyCombinedPosition()
        {
            // Combine idle animation position with camera responsiveness
            // This ensures both systems work together harmoniously
            
            Vector3 targetPosition = _currentIdlePosition + _cameraBasedOffset;
            
            // Smooth transition to prevent jarring movements
            _tentacleTarget.localPosition = Vector3.Lerp(_lastAppliedPosition, targetPosition, Time.deltaTime * _cameraLerpSpeed);
            _lastAppliedPosition = _tentacleTarget.localPosition;
        }
        
        private void MoveTowardAimPosition()
        {
            // Calculate current world position of the tentacle target
            Vector3 currentWorldPos = _tentacleTarget.position;
            
            // Calculate direction and distance to aim position
            Vector3 direction = (_currentAimPosition - currentWorldPos);
            float distance = direction.magnitude;
            
            if (distance < 0.01f) // Close enough to target
            {
                _tentacleTarget.position = _currentAimPosition;
                return;
            }
            
            // Move toward target with speed and smoothing
            float moveDistance = Mathf.Min(distance, _aimSpeed * Time.deltaTime);
            Vector3 newPosition = currentWorldPos + direction.normalized * moveDistance;
            
            // Apply smooth transition
            _tentacleTarget.position = Vector3.Lerp(currentWorldPos, newPosition, Time.deltaTime * _aimLerpSpeed);
        }
        
        private void OnDestroy()
        {
            // Clean up tweens
            if (_idleTween != null)
            {
                _idleTween.Kill();
            }
            if (_positionTween != null)
            {
                _positionTween.Kill();
            }
        }
        
        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure we have reasonable values in the inspector
            _idleAmplitude = Mathf.Max(0f, _idleAmplitude);
            _idleFrequency = Mathf.Max(0.1f, _idleFrequency);
            _aimSpeed = Mathf.Max(0f, _aimSpeed);
            _aimLerpSpeed = Mathf.Max(0f, _aimLerpSpeed);
            _cameraResponsiveStrength = Mathf.Clamp01(_cameraResponsiveStrength);
            _cameraLerpSpeed = Mathf.Max(0f, _cameraLerpSpeed);
            _cameraResponsiveRange = Mathf.Max(0.1f, _cameraResponsiveRange);
            
            // Reset base position if target is assigned
            if (_tentacleTarget != null && Application.isPlaying == false)
            {
                _basePosition = _tentacleTarget.localPosition;
                _currentIdlePosition = _basePosition;
                _lastAppliedPosition = _basePosition;
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (_tentacleTarget != null)
            {
                // Draw the idle sway area
                Gizmos.color = Color.cyan;
                Vector3 basePos = _tentacleTarget.position;
                Vector3 swayOffset = _idleAxis * _idleAmplitude;
                
                Gizmos.DrawWireSphere(basePos + swayOffset, 0.1f);
                Gizmos.DrawWireSphere(basePos - swayOffset, 0.1f);
                Gizmos.DrawLine(basePos, basePos + swayOffset);
                
                // Draw current idle position
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(basePos + (_currentIdlePosition - _basePosition), 0.08f);
                
                // Draw current aim position if aiming
                if (_isAiming)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(_currentAimPosition, 0.15f);
                    Gizmos.DrawLine(basePos, _currentAimPosition);
                }
                
                // Draw camera-based offset if active
                if (_isCameraResponsive && _player != null)
                {
                    Gizmos.color = Color.green;
                    Vector3 cameraOffsetPos = basePos + _cameraBasedOffset * 2f;
                    Gizmos.DrawWireSphere(cameraOffsetPos, 0.08f);
                    Gizmos.DrawLine(basePos, cameraOffsetPos);
                    
                    // Draw camera position and calculations
                    Camera mainCamera = Camera.main;
                    if (mainCamera != null)
                    {
                        Vector3 cameraPos = mainCamera.transform.position;
                        Vector3 playerPos = _player.transform.position;
                        
                        // Draw line from player to camera
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(playerPos, cameraPos);
                        
                        // Draw player forward
                        Gizmos.color = Color.white;
                        Gizmos.DrawLine(playerPos, playerPos + _player.transform.forward * 1f);
                        
                        // Draw left/right calculation
                        if (Application.isPlaying)
                        {
                            Vector3 horizontalPlayerToCamera = cameraPos - playerPos;
                            horizontalPlayerToCamera.y = 0f;
                            
                            float rightLeftValue = Vector3.Cross(_player.transform.forward, horizontalPlayerToCamera.normalized).y;
                            
                            // Visualize left/right direction
                            if (Mathf.Abs(rightLeftValue) > 0.1f)
                            {
                                Vector3 arrowDir = Vector3.right * Mathf.Sign(rightLeftValue);
                                Gizmos.color = rightLeftValue > 0 ? Color.magenta : Color.cyan; // Right = magenta, Left = cyan
                                Gizmos.DrawLine(basePos + Vector3.up * 0.3f, basePos + Vector3.up * 0.3f + arrowDir * 0.4f);
                            }
                            
                            // Draw distance-based strength
                            float lateralStrength = Mathf.Clamp01(horizontalPlayerToCamera.magnitude / 5f);
                            Gizmos.color = Color.Lerp(Color.gray, Color.yellow, lateralStrength);
                            Gizmos.DrawWireSphere(cameraPos, 0.1f + lateralStrength * 0.1f);
                        }
                    }
                }
            }
        }
        #endif
    }
}