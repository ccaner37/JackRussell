using UnityEngine;
using System.Collections.Generic;

namespace JackRussell.Rails
{
    /// <summary>
    /// Handles detection and attachment to rails for the player.
    /// Scans for nearby rails and manages attachment/detachment.
    /// </summary>
    public class RailDetector : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private float _detectionRadius = 10f; // Increased from 5f
        [SerializeField] private LayerMask _railLayer = 1 << 0;
        [SerializeField] private float _maxAttachAngle = 60f; // Increased from 45f for more lenient attachment

        [Header("Attachment Settings")]
        [SerializeField] private float _attachCooldown = 0.5f;
        [SerializeField] private bool _autoAttach = true;

        // Runtime data
        private SplineRail _currentRail;
        private float _currentDistance;
        private float _lastAttachTime;
        private float _lastDetachTime;
        private bool _isAttached;
        private bool _grindForward; // true = grind in positive direction, false = grind in negative direction
        private bool _detachedFromEnd; // true if we just detached from the end of a rail
        private bool _jumpDismount; // true if we just jumped off a rail

        // Cached components
        private Rigidbody _rb;
        private Transform _playerTransform;

        public SplineRail CurrentRail => _currentRail;
        public float CurrentDistance => _currentDistance;
        public bool IsAttached => _isAttached;
        public bool GrindForward => _grindForward;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _playerTransform = transform;
        }

        /// <summary>
        /// Scan for nearby rails and return the best attachment candidate
        /// </summary>
        public SplineRail FindBestRail()
        {
            if (Time.time - _lastAttachTime < _attachCooldown) return null;

            // Prevent immediate reattachment after detaching from end of rail or jump dismount
            if ((_detachedFromEnd || _jumpDismount) && Time.time - _lastDetachTime < 0.5f)
            {
                string reason = _detachedFromEnd ? "end-of-rail detachment" : "jump dismount";
                Debug.Log($"[RailDetector] Blocking reattachment - cooldown after {reason}");
                return null;
            }

            // Find all SplineRail components in the scene
            SplineRail[] allRails = FindObjectsOfType<SplineRail>();
            if (allRails.Length == 0) return null;

            SplineRail bestRail = null;
            float bestScore = float.MaxValue;

            foreach (SplineRail rail in allRails)
            {
                // Skip rails that are not grindable (e.g., path-only rails)
                if (!rail.IsGrindable)
                {
                    continue;
                }

                // Sample points along the spline to find the closest point to player
                float closestSplineDistance = rail.FindClosestDistance(_playerTransform.position);
                rail.GetPositionAndTangent(closestSplineDistance, out Vector3 closestSplinePos, out Vector3 railTangent);

                // Check distance from player to the closest point on the spline
                float distanceToSpline = Vector3.Distance(_playerTransform.position, closestSplinePos);

                if (distanceToSpline > _detectionRadius)
                {
                    //Debug.Log($"[RailDetector] Rail {rail.gameObject.name} too far from spline: {distanceToSpline} > {_detectionRadius}");
                    continue;
                }

                // Check if player is within attach range of the spline
                if (distanceToSpline > rail.AttachDistance)
                {
                    //Debug.Log($"[RailDetector] Rail {rail.gameObject.name} not within attach range: {distanceToSpline} > {rail.AttachDistance}");
                    continue;
                }

                // Calculate attachment score based on distance and angle
                float distanceScore = distanceToSpline;
                float angleScore = 0f;

                // Check angle between player velocity and rail tangent
                if (_rb != null && _rb.linearVelocity.sqrMagnitude > 0.1f)
                {
                    Vector3 velocityDir = _rb.linearVelocity.normalized;
                    float angle = Vector3.Angle(velocityDir, railTangent);
                    angleScore = Mathf.Min(angle, 180f - angle); // Handle direction reversal
                }

                // Only consider rails within angle threshold (temporarily disabled for testing)
                // if (angleScore > _maxAttachAngle)
                // {
                //     Debug.Log($"[RailDetector] Rail {rail.gameObject.name} angle too large: {angleScore} > {_maxAttachAngle}");
                //     continue;
                // }

                // Combined score (lower is better)
                float totalScore = distanceScore + (angleScore * 0.1f);

                Debug.Log($"[RailDetector] Rail {rail.gameObject.name} score: {totalScore} (dist: {distanceScore}, angle: {angleScore})");

                if (totalScore < bestScore)
                {
                    bestScore = totalScore;
                    bestRail = rail;
                }
            }

            return bestRail;
        }

        /// <summary>
        /// Attempt to attach to a rail
        /// </summary>
        public bool TryAttachToRail(SplineRail rail)
        {
            if (rail == null || !rail.IsWithinAttachRange(_playerTransform.position)) return false;
            if (Time.time - _lastAttachTime < _attachCooldown) return false;

            _currentRail = rail;
            float rawDistance = rail.FindClosestDistance(_playerTransform.position);

            // Small safeguard: avoid exact t=0 which can cause issues
            if (rawDistance < 0.01f) // Within 1cm of start
            {
                rawDistance = 0.01f; // Small offset from start
                Debug.Log("[RailDetector] Applied small offset from rail start to avoid t=0 issues");
            }

            _currentDistance = rawDistance;
            _isAttached = true;
            _lastAttachTime = Time.time;

            // Reset detachment flags since we successfully attached
            _detachedFromEnd = false;
            _jumpDismount = false;

            // Determine grinding direction based on player's facing
            DetermineGrindDirection();

            return true;
        }

        /// <summary>
        /// Determine the grinding direction based on player's facing direction
        /// </summary>
        private void DetermineGrindDirection()
        {
            if (_currentRail == null) return;

            // Get the rail tangent at current position
            if (_currentRail.GetPositionAndTangent(_currentDistance, out Vector3 _, out Vector3 railTangent))
            {
                // Get player's forward direction (horizontal only)
                Vector3 playerForward = new Vector3(_playerTransform.forward.x, 0f, _playerTransform.forward.z).normalized;

                // Check if player's forward aligns more with positive or negative rail direction
                float dotProduct = Vector3.Dot(playerForward, railTangent);

                // If dot product is positive, grind forward; if negative, grind backward
                _grindForward = dotProduct >= 0f;
            }
            else
            {
                // Fallback to forward direction
                _grindForward = true;
            }
        }

        /// <summary>
        /// Detach from current rail
        /// </summary>
        public void DetachFromRail()
        {
            // Check if we're detaching from the end of the rail
            if (_currentRail != null && _currentDistance >= _currentRail.TotalLength - 0.1f)
            {
                _detachedFromEnd = true;
                _lastDetachTime = Time.time;
                Debug.Log("[RailDetector] Detached from end of rail - preventing immediate reattachment");
            }

            _currentRail = null;
            _isAttached = false;
            _currentDistance = 0f;
            _grindForward = true; // Reset to default
        }

        /// <summary>
        /// Detach from current rail due to jump dismount
        /// </summary>
        public void DetachFromRailJump()
        {
            _jumpDismount = true;
            _lastDetachTime = Time.time;
            Debug.Log("[RailDetector] Jump dismount - preventing immediate reattachment");

            _currentRail = null;
            _isAttached = false;
            _currentDistance = 0f;
            _grindForward = true; // Reset to default
        }

        /// <summary>
        /// Update current position along the rail
        /// </summary>
        public void UpdateRailPosition(float deltaDistance)
        {
            if (!_isAttached || _currentRail == null) return;

            _currentDistance += deltaDistance;
            _currentDistance = Mathf.Clamp(_currentDistance, 0f, _currentRail.TotalLength);
        }

        /// <summary>
        /// Get current position and tangent on the rail
        /// </summary>
        public bool GetCurrentRailPosition(out Vector3 position, out Vector3 tangent)
        {
            position = Vector3.zero;
            tangent = Vector3.forward;

            if (!_isAttached || _currentRail == null) return false;

            return _currentRail.GetPositionAndTangent(_currentDistance, out position, out tangent);
        }

        /// <summary>
        /// Check if player should auto-attach to nearby rails
        /// </summary>
        public void CheckAutoAttach()
        {
            if (!_autoAttach || _isAttached) return;

            SplineRail rail = FindBestRail();
            if (rail != null)
            {
                Debug.Log($"[RailDetector] Found rail to attach to: {rail.gameObject.name}");
                bool success = TryAttachToRail(rail);
                if (success)
                {
                    Debug.Log($"[RailDetector] Successfully attached to rail: {rail.gameObject.name}");
                }
                else
                {
                    Debug.Log($"[RailDetector] Failed to attach to rail: {rail.gameObject.name}");
                }
            }
            else
            {
                // Debug: List all rails found (uncomment for debugging)
                // SplineRail[] allRails = FindObjectsOfType<SplineRail>();
                // Debug.Log($"[RailDetector] No suitable rail found. Total rails in scene: {allRails.Length}");
            }
        }

        /// <summary>
        /// Check if player should detach (e.g., reached end of rail)
        /// </summary>
        public bool ShouldDetach()
        {
            if (!_isAttached || _currentRail == null) return false;

            // Don't detach immediately after attaching (prevent false positives at start)
            if (Time.time - _lastAttachTime < 0.2f) return false;

            // Detach if reached end of rail
            return _currentDistance >= _currentRail.TotalLength - 0.1f ||
                    _currentDistance <= 0.05f; // Reduced from 0.1f to avoid conflict with our 0.01f offset
        }

        private void OnDrawGizmosSelected()
        {
            // Draw detection radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);

            // Draw attach range for all rails
            SplineRail[] allRails = FindObjectsOfType<SplineRail>();
            foreach (SplineRail rail in allRails)
            {
                if (rail.IsWithinAttachRange(transform.position))
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(transform.position, 0.5f);
                }
            }

            // Draw current rail attachment
            if (_isAttached && _currentRail != null && GetCurrentRailPosition(out Vector3 pos, out Vector3 tangent))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(pos, 0.2f);
                Gizmos.DrawLine(pos, pos + tangent * 2f);
            }
        }

        /// <summary>
        /// Force attach to nearest rail (for debugging/testing)
        /// </summary>
        [ContextMenu("Force Attach to Nearest Rail")]
        private void ForceAttachToNearestRail()
        {
            SplineRail[] allRails = FindObjectsOfType<SplineRail>();
            if (allRails.Length == 0)
            {
                Debug.LogWarning("[RailDetector] No rails found in scene!");
                return;
            }

            SplineRail nearestRail = null;
            float nearestDistance = float.MaxValue;

            foreach (SplineRail rail in allRails)
            {
                float distance = Vector3.Distance(transform.position, rail.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestRail = rail;
                }
            }

            if (nearestRail != null)
            {
                Debug.Log($"[RailDetector] Force attaching to: {nearestRail.gameObject.name}");
                TryAttachToRail(nearestRail);
            }
        }
    }
}