using JackRussell;
using JackRussell.Rails;
using JackRussell.CameraController;
using UnityEngine;
using UnityEngine.InputSystem;
using VitalRouter;

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
        private ICommandPublisher _commandPublisher;

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
            _commandPublisher = player.CommandPublisher;
        }

        public override string Name => nameof(GrindState);
        
        public override LocomotionType LocomotionType => LocomotionType.Grind;

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
            _grindSpeed = Mathf.Max(_player.Rigidbody.linearVelocity.magnitude, _currentRail.BaseSpeed);
            _lastPosition = _player.transform.position;
            _isAccelerating = false;

            _player.OnGrindEnter();

            // Publish camera state update command
            _commandPublisher.PublishAsync(new CameraStateUpdateCommand(3.3f, 100f));

            // Subscribe to jump press
            _player.Actions.Player.Jump.performed += OnJumpPressed;

            // Set initial position on rail
            if (_railDetector.GetCurrentRailPosition(out Vector3 railPos, out Vector3 tangent))
            {
                _player.transform.position = railPos;

                // Debug: Check tangent validity
                Debug.Log($"[GrindState] Attach at distance {_currentDistance:F3}, Tangent: {tangent}, Magnitude: {tangent.magnitude}");

                // Safeguard: ensure tangent is valid
                if (tangent.sqrMagnitude < 0.1f)
                {
                    Debug.LogWarning("[GrindState] Invalid tangent detected, using forward direction");
                    tangent = _railDetector.GrindForward ? Vector3.forward : Vector3.back;
                }

                Vector3 grindDirection = _railDetector.GrindForward ? tangent : -tangent;
                grindDirection = grindDirection.normalized;

                _player.Rigidbody.linearVelocity = grindDirection * _grindSpeed;

                Debug.Log($"[GrindState] Final velocity: {_player.Rigidbody.linearVelocity} (speed: {_grindSpeed:F1})");
            }
        }

        public override void Exit()
        {
            // Publish camera state update command to revert to default
            _commandPublisher.PublishAsync(new CameraStateUpdateCommand());

            // Unsubscribe
            _player.Actions.Player.Jump.performed -= OnJumpPressed;

            _railDetector.DetachFromRail();
            _currentRail = null;
            _player.OnGrindExit();
        }

        public override void LogicUpdate()
        {
            // Check if we should detach (end of rail, etc.)
            if (_railDetector.ShouldDetach())
            {
                ChangeState(new FallState(_player, _stateMachine));
                Debug.LogError("ShouldDeatch1");
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
                Debug.LogError("GetCurrentRailPosition1");
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
                Debug.LogError("GetCurrentRailPosition2");
                return;
            }

            // Move player along rail
            Vector3 currentPos = _player.transform.position;
            Vector3 newPos = Vector3.Lerp(currentPos, targetPos, deltaTime / k_PositionSmoothTime);

            // Debug: Check if we're at the start and having issues
            if (_currentDistance < 0.1f)
            {
                Debug.Log($"[GrindState] Physics at start - Current: {currentPos}, Target: {targetPos}, Distance: {_currentDistance:F3}");
            }

            // Apply reduced gravity while grinding
            Vector3 velocity = (newPos - currentPos) / deltaTime;
            velocity.y -= Physics.gravity.y * k_GravityMultiplier * deltaTime;

            // Apply rail friction
            velocity *= (1f - k_RailFriction * deltaTime);

            _player.SetVelocityImmediate(velocity);

            // Rotate player to face rail direction (use tangent for proper 3D alignment)
            if (tangent.sqrMagnitude > 0.1f)
            {
                Vector3 grindDirection = _railDetector.GrindForward ? tangent : -tangent;
                _player.RotateTowardsDirection(grindDirection, deltaTime, isAir: false, instantaneous: false, allow3DRotation: true);
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

        private void OnJumpPressed(InputAction.CallbackContext context)
        {
            if (_currentRail.AllowDismount)
            {
                // Jump off the rail
                Vector3 jumpVelocity = Vector3.up * (_player.JumpVelocity * k_DismountJumpMultiplier);
                if (_railDetector.GetCurrentRailPosition(out Vector3 _, out Vector3 tangent))
                {
                    // Add forward momentum from grind speed
                    jumpVelocity += tangent * (_grindSpeed * 0.7f);
                }

                // Use special detach method for jump dismounts to prevent immediate reattachment
                _railDetector.DetachFromRailJump();

                _player.SetVelocityImmediate(Vector3.zero);
                ChangeState(new JumpState(_player, _stateMachine));
            }
        }
    }
}