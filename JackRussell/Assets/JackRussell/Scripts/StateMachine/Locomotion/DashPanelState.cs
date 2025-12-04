using JackRussell;
using JackRussell.Rails;
using UnityEngine;
using UnityEngine.InputSystem;
using VitalRouter;

namespace JackRussell.States.Locomotion
{
    /// <summary>
    /// Dash Panel state: handles player movement along a spline path at high speed.
    /// Allows sprinting to boost speed, similar to grinding.
    /// Triggered by dash panel interactions.
    /// </summary>
    public class DashPanelState : PlayerStateBase
    {
        private SplinePath _path;
        private float _currentDistance;
        private float _dashSpeed;
        private float _duration;
        private bool _allowSprint;
        private float _sprintSpeedMultiplier;
        private float _startTime;
        private Vector3 _lastPosition;
        private SprintController _sprintController;
        private ICommandPublisher _commandPublisher;

        // Constants
        private const float k_PositionSmoothTime = 0.1f;

        public DashPanelState(Player player, StateMachine stateMachine, SplinePath path, float speed, float duration, bool allowSprint = true, float sprintMultiplier = 1.5f)
            : base(player, stateMachine)
        {
            _path = path;
            _dashSpeed = speed;
            _duration = duration;
            _allowSprint = allowSprint;
            _sprintSpeedMultiplier = sprintMultiplier;
            _sprintController = player.SprintController;
            _commandPublisher = player.CommandPublisher;
        }

        public override string Name => nameof(DashPanelState);

        public override LocomotionType LocomotionType => LocomotionType.DashPanel;

        public override void Enter()
        {
            if (_path == null)
            {
                Debug.LogError("[DashPanelState] No path assigned!");
                ChangeState(new FallState(_player, _stateMachine));
                return;
            }

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
                _player.SetVelocityImmediate(tangent.normalized * _dashSpeed);
                _lastPosition = startPos;

                // Immediately rotate towards movement direction
                if (tangent.sqrMagnitude > 0.1f)
                {
                    _player.RotateTowardsDirection(tangent, 0f, isAir: true, instantaneous: true);
                }

                Debug.Log($"[DashPanelState] Starting dash at distance {_currentDistance:F3}");
            }
            else
            {
                Debug.LogError("[DashPanelState] Failed to get initial position on path");
                ChangeState(new FallState(_player, _stateMachine));
                return;
            }

            _startTime = Time.time;

            // Subscribe to sprint inputs if allowed
            if (_allowSprint)
            {
                _player.Actions.Player.Sprint.performed += OnSprintPressed;
                _player.Actions.Player.Sprint.canceled += OnSprintCanceled;
            }

            // Subscribe to jump (allow jumping off)
            _player.Actions.Player.Jump.performed += OnJumpPressed;

            _player.PlaySound(Audio.SoundType.DashPanelEnter);
        }

        public override void Exit(IState nextState = null)
        {
            // Unsubscribe
            if (_allowSprint)
            {
                _player.Actions.Player.Sprint.performed -= OnSprintPressed;
                _player.Actions.Player.Sprint.canceled -= OnSprintCanceled;
            }
            _player.Actions.Player.Jump.performed -= OnJumpPressed;

            // Keep sprinting active if it was (don't stop it)
        }

        public override void LogicUpdate()
        {
            // Check duration
            if (Time.time - _startTime >= _duration)
            {
                TransitionToAppropriateState("Duration expired");
                return;
            }

            HandleDashPanelMovement();
        }

        public override void PhysicsUpdate()
        {
        }

        private void HandleDashPanelMovement()
        {
            if (_path == null) return;

            float deltaTime = Time.deltaTime;

            // Calculate speed (base + sprint bonus)
            float currentSpeed = _dashSpeed;
            if (_allowSprint && _sprintController != null && _sprintController.IsSprinting)
            {
                currentSpeed *= _sprintSpeedMultiplier;
                // Update sprint effects
                float speedFactor = Mathf.Clamp01(currentSpeed / _dashSpeed);
                _sprintController.UpdateSprint(deltaTime, speedFactor);
            }

            // Advance along path
            float distanceDelta = currentSpeed * deltaTime;
            _currentDistance += distanceDelta;
            _currentDistance = Mathf.Clamp(_currentDistance, 0f, _path.TotalLength);

            if (_path.GetPositionAndTangent(_currentDistance, out Vector3 targetPos, out Vector3 tangent))
            {
                Vector3 currentPos = _player.transform.position;
                Vector3 newPos = Vector3.Lerp(currentPos, targetPos, deltaTime / k_PositionSmoothTime);
                Vector3 velocity = (newPos - currentPos) / deltaTime;
                _player.SetVelocityImmediate(velocity);

                if (tangent.sqrMagnitude > 0.1f)
                {
                    _player.RotateTowardsDirection(tangent, deltaTime, isAir: true, instantaneous: false, allow3DRotation: false);
                }

                _lastPosition = newPos;

                // Check if path completed
                if (_currentDistance >= _path.TotalLength - 0.1f)
                {
                    TransitionToAppropriateState("Path completed");
                }
            }
            else
            {
                ChangeState(new FallState(_player, _stateMachine));
                Debug.LogError("[DashPanelState] Lost path during movement");
            }
        }

        private void OnSprintPressed(InputAction.CallbackContext context)
        {
            if (!_sprintController.IsSprinting)
            {
                if (_player.Pressure < 5f) return;
                _player.SetPressure(_player.Pressure - 5f);
                _sprintController.TryStartSprint();
            }
        }

        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            if (_sprintController.IsSprinting)
            {
                _sprintController.StopSprint();
            }
        }

        private void TransitionToAppropriateState(string reason)
        {
            if (!_player.IsGrounded)
            {
                ChangeState(new FallState(_player, _stateMachine));
                Debug.Log($"[DashPanelState] {reason}, transitioning to fall");
            }
            else if (_sprintController != null && _sprintController.IsSprinting)
            {
                ChangeState(new SprintState(_player, _stateMachine));
                Debug.Log($"[DashPanelState] {reason}, transitioning to sprint");
            }
            else
            {
                ChangeState(new MoveState(_player, _stateMachine));
                Debug.Log($"[DashPanelState] {reason}, transitioning to move");
            }
        }

        private void OnJumpPressed(InputAction.CallbackContext context)
        {
            // Allow jumping off the dash panel
            Vector3 jumpVelocity = Vector3.up * _player.JumpVelocity;
            if (_path.GetPositionAndTangent(_currentDistance, out Vector3 _, out Vector3 tangent))
            {
                // Add forward momentum
                jumpVelocity += tangent * (_dashSpeed * 0.5f);
            }

            _player.SetVelocityImmediate(jumpVelocity);
            ChangeState(new JumpState(_player, _stateMachine));
        }
    }
}