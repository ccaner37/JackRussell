using UnityEngine;
using DG.Tweening;

namespace JackRussell
{
    /// <summary>
    /// Manages tentacle idle animations and future aiming functionality.
    /// Provides a clean separation between animation logic and state machine.
    /// </summary>
    public class TentacleController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _tentacleTarget; // The transform that the IK rig follows
        
        [Header("Idle Animation Settings")]
        [SerializeField] private float _idleAmplitude = 0.1f;     // How much the tentacle sways
        [SerializeField] private float _idleFrequency = 1.5f;     // How fast it sways
        [SerializeField] private Vector3 _idleAxis = new Vector3(0f, 0.1f, 0.05f); // Sway direction per axis
        [SerializeField] private float _idleTransitionDuration = 0.5f; // Smooth transition in/out
        
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
        private Tween _idleTween;
        private Tween _positionTween;
        
        // Public properties for external access
        public bool IsIdleAnimating => _isIdleAnimating;
        public bool IsAiming => _isAiming;
        public Transform TentacleTarget => _tentacleTarget;
        public Vector3 CurrentAimPosition => _currentAimPosition;
        
        private void Awake()
        {
            // Store original transform state
            if (_tentacleTarget != null)
            {
                _originalPosition = _tentacleTarget.localPosition;
                _originalRotation = _tentacleTarget.localRotation;
                _basePosition = _originalPosition;
                _currentAimPosition = _originalPosition;
            }
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
            
            // Smoothly return to base position
            _tentacleTarget.localPosition = _basePosition;
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
                
            if (_isAiming)
            {
                // Move toward aim position
                MoveTowardAimPosition();
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
            }
        }
        
        private Tween CreateIdleSwayTween()
        {
            // Create a smooth, looping sway animation using DOTween
            Sequence idleSequence = DOTween.Sequence();
            idleSequence.SetLoops(-1, LoopType.Yoyo); // Infinite loop with yoyo for smooth back-and-forth
            
            // We'll create a compound animation by adding position offsets over time
            Vector3 startPos = _basePosition;
            Vector3 endPos = _basePosition + _idleAxis * _idleAmplitude;
            
            // Tween from start to end position
            Tween positionTween = _tentacleTarget.DOLocalMove(endPos, 1f / _idleFrequency)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
                
            return positionTween;
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
            
            // Reset base position if target is assigned
            if (_tentacleTarget != null && Application.isPlaying == false)
            {
                _basePosition = _tentacleTarget.localPosition;
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
                
                // Draw current aim position if aiming
                if (_isAiming)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(_currentAimPosition, 0.15f);
                    Gizmos.DrawLine(basePos, _currentAimPosition);
                }
            }
        }
        #endif
    }
}