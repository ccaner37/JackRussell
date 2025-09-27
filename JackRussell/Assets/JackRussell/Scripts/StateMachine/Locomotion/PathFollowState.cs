using JackRussell;
using JackRussell.Rails;
using UnityEngine;

namespace JackRussell.States.Locomotion
{
    /// <summary>
    /// Path Follow state: handles player movement along a predefined spline path.
    /// Triggered by path launcher interactions, provides forced path following like spring pads.
    /// </summary>
    public class PathFollowState : PlayerStateBase
    {
        private SplineRail _path;
        private float _currentDistance;
        private float _pathSpeed;
        private Vector3 _lastPosition;

        // Constants
        private const float k_PathSpeed = 25f; // Fixed speed for path following
        private const float k_PositionSmoothTime = 0.1f;

        public PathFollowState(Player player, StateMachine stateMachine, SplineRail path) : base(player, stateMachine)
        {
            _path = path;
            _pathSpeed = k_PathSpeed;
        }

        public override string Name => nameof(PathFollowState);

        public override void Enter()
        {
            if (_path == null)
            {
                Debug.LogError("[PathFollowState] No path assigned!");
                ChangeState(new FallState(_player, _stateMachine));
                return;
            }

            _player.Animator.Play("thug_life");

            // Find closest point on path to start
            _currentDistance = _path.FindClosestDistance(_player.transform.position);

            // Small safeguard: avoid exact t=0
            if (_currentDistance < 0.01f)
            {
                _currentDistance = 0.01f;
            }

            // Set initial position and velocity
            if (_path.GetPositionAndTangent(_currentDistance, out Vector3 startPos, out Vector3 tangent))
            {
                _player.transform.position = startPos;
                _player.Rigidbody.linearVelocity = tangent.normalized * _pathSpeed;
                _lastPosition = startPos;

                Debug.Log($"[PathFollowState] Starting path at distance {_currentDistance:F3}");
            }
            else
            {
                Debug.LogError("[BellPathState] Failed to get initial position on path");
                ChangeState(new FallState(_player, _stateMachine));
            }
        }

        public override void Exit()
        {
            // No special cleanup needed
        }

        public override void LogicUpdate()
        {
            // No input handling - forced movement
        }

        public override void PhysicsUpdate()
        {
            if (_path == null) return;

            // Calculate movement along path
            float deltaTime = Time.fixedDeltaTime;
            float distanceDelta = _pathSpeed * deltaTime;

            // Update position along path
            _currentDistance += distanceDelta;
            _currentDistance = Mathf.Clamp(_currentDistance, 0f, _path.TotalLength);

            // Get new position
            if (_path.GetPositionAndTangent(_currentDistance, out Vector3 targetPos, out Vector3 tangent))
            {
                // Move player along path
                Vector3 currentPos = _player.transform.position;
                Vector3 newPos = Vector3.Lerp(currentPos, targetPos, deltaTime / k_PositionSmoothTime);

                // Calculate velocity for physics
                Vector3 velocity = (newPos - currentPos) / deltaTime;
                _player.SetVelocityImmediate(velocity);

                // Rotate player to face path direction
                if (tangent.sqrMagnitude > 0.1f)
                {
                    _player.RotateTowardsDirection(tangent, deltaTime, isAir: true, instantaneous: false, allow3DRotation: false);
                }

                _lastPosition = newPos;

                // Check if reached end of path
                if (_currentDistance >= _path.TotalLength - 0.1f)
                {
                    ChangeState(new FallState(_player, _stateMachine));
                    Debug.Log("[PathFollowState] Path completed, transitioning to fall");
                }
            }
            else
            {
                // Lost path, transition to fall
                ChangeState(new FallState(_player, _stateMachine));
                Debug.LogError("[PathFollowState] Lost path during movement");
            }
        }
    }
}