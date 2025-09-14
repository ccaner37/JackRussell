using JackRussell;
using JackRussell.Rails;
using UnityEngine;

namespace JackRussell.States.Locomotion
{
    /// <summary>
    /// Grind state: handles player movement along rails.
    /// Manages attachment, movement, and dismounting from rails.
    /// </summary>
    public class GrindState : PlayerStateBase
    {
        private RailDetector _railDetector;
        private SplineRail _currentRail;
        private float _grindSpeed;
        private float _currentDistance;
        private Vector3 _lastPosition;
        private bool _isAccelerating;

        // Constants
        private const float k_MinGrindSpeed = 5f;
        private const float k_MaxGrindSpeed = 35f;
        private const float k_SpeedSmoothTime = 0.2f;
        private const float k_PositionSmoothTime = 0.1f;

        // Tuning parameters (could be exposed in inspector if needed)
        private const float k_GravityMultiplier = 0.3f; // Reduced gravity while grinding
        private const float k_RailFriction = 0.05f; // Friction applied to rail movement
        private const float k_DismountJumpMultiplier = 1.2f; // Extra jump power when dismounting

        public GrindState(Player player, StateMachine stateMachine) : base(player, stateMachine)
        {
            _railDetector = player.GetComponent<RailDetector>();
            if (_railDetector == null)
            {
                Debug.LogError("GrindState requires a RailDetector component on the player!");
            }
        }

        public override string Name => nameof(GrindState);

        public override void Enter()
        {
            if (_railDetector == null || !_railDetector.IsAttached)
            {
                // Try to find and attach to a rail
                SplineRail rail = _railDetector.FindBestRail();
                if (rail != null)
                {
                    _railDetector.TryAttachToRail(rail);
                }
                else
                {
                    // No rail found, transition to fall state
                    ChangeState(new FallState(_player, _stateMachine));
                    return;
                }
            }

            _currentRail = _railDetector.CurrentRail;
            _currentDistance = _railDetector.CurrentDistance;
            _grindSpeed = Mathf.Max(_player.Rigidbody.velocity.magnitude, _currentRail.BaseSpeed);
            _lastPosition = _player.transform.position;
            _isAccelerating = false;

            // Set initial position on rail
            if (_railDetector.GetCurrentRailPosition(out Vector3 railPos, out Vector3 tangent))
            {
                _player.transform.position = railPos;

                // Set initial velocity based on grinding direction
                Vector3 grindDirection = _railDetector.GrindForward ? tangent : -tangent;
                _player.Rigidbody.velocity = grindDirection * _grindSpeed;
            }
        }

        public override void Exit()
        {
            _railDetector.DetachFromRail();
            _currentRail = null;
        }

        public override void LogicUpdate()
        {
            // Check for dismount input (jump)
            if (_player.ConsumeJumpRequest() && _currentRail.AllowDismount)
            {
                // Jump off the rail
                    Vector3 jumpVelocity = Vector3.up * (_player.JumpVelocity * k_DismountJumpMultiplier);
                    if (_railDetector.GetCurrentRailPosition(out Vector3 _, out Vector3 tangent))
                    {
                        // Add forward momentum from grind speed
                        jumpVelocity += tangent * (_grindSpeed * 0.7f);
                    }
    
                    _player.SetVelocityImmediate(jumpVelocity);
                    ChangeState(new JumpState(_player, _stateMachine));
                    return;
            }

            // Check if we should detach (end of rail, etc.)
            if (_railDetector.ShouldDetach())
            {
                ChangeState(new FallState(_player, _stateMachine));
                return;
            }

            // Update grind speed based on input
            UpdateGrindSpeed();
        }

        public override void PhysicsUpdate()
        {
            if (_currentRail == null || !_railDetector.IsAttached) return;

            // Get current rail position and tangent
            if (!_railDetector.GetCurrentRailPosition(out Vector3 targetPos, out Vector3 tangent))
            {
                // Lost rail, transition to fall
                ChangeState(new FallState(_player, _stateMachine));
                return;
            }

            // Calculate movement along rail
            float deltaTime = Time.fixedDeltaTime;
            float distanceDelta = _grindSpeed * deltaTime;

            // Apply direction based on grinding direction
            if (!_railDetector.GrindForward)
            {
                distanceDelta = -distanceDelta;
            }

            // Update position along rail
            _railDetector.UpdateRailPosition(distanceDelta);
            _currentDistance = _railDetector.CurrentDistance;

            // Get new position
            if (!_railDetector.GetCurrentRailPosition(out targetPos, out tangent))
            {
                ChangeState(new FallState(_player, _stateMachine));
                return;
            }

            // Move player along rail
            Vector3 currentPos = _player.transform.position;
            Vector3 newPos = Vector3.Lerp(currentPos, targetPos, deltaTime / k_PositionSmoothTime);

            // Apply reduced gravity while grinding
            Vector3 velocity = (newPos - currentPos) / deltaTime;
            velocity.y -= Physics.gravity.y * k_GravityMultiplier * deltaTime;

            // Apply rail friction
            velocity *= (1f - k_RailFriction * deltaTime);

            _player.SetVelocityImmediate(velocity);

            // Rotate player to face movement direction
            if (velocity.sqrMagnitude > 0.1f)
            {
                _player.RotateTowardsDirection(velocity, deltaTime, isAir: false);
            }

            _lastPosition = newPos;
        }

        private void UpdateGrindSpeed()
        {
            float targetSpeed = _currentRail.BaseSpeed;

            // Accelerate/decelerate based on input
            if (_player.MoveDirection.sqrMagnitude > 0.1f)
            {
                // Player wants to go faster
                _isAccelerating = true;
                targetSpeed = Mathf.Min(targetSpeed + _currentRail.Acceleration * Time.deltaTime, k_MaxGrindSpeed);
            }
            else
            {
                // Player wants to slow down
                _isAccelerating = false;
                targetSpeed = Mathf.Max(targetSpeed - _currentRail.Deceleration * Time.deltaTime, k_MinGrindSpeed);
            }

            // Smooth speed changes
            _grindSpeed = Mathf.Lerp(_grindSpeed, targetSpeed, Time.deltaTime / k_SpeedSmoothTime);
        }
    }
}