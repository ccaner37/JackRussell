using JackRussell;
using JackRussell.CameraController;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using VitalRouter;

namespace JackRussell.States.Locomotion
{
    public class SprintState : PlayerStateBase
    {
        private float _defaultSprintTime = 0f;
        private float _airSprintTimer = 0f;
        private bool _enteredFromAir;
        private SprintController _sprintController;
        private ICommandPublisher _commandPublisher;

        public SprintState(Player player, StateMachine stateMachine) : base(player, stateMachine)
        {
            _commandPublisher = player.CommandPublisher;
            _sprintController = player.SprintController;
            if (_sprintController == null)
            {
                Debug.LogError("SprintState requires a SprintController component on the Player!");
            }
        }

        public override string Name => nameof(SprintState);
        
        public override LocomotionType LocomotionType => LocomotionType.Sprint;

        public override void Enter()
        {
            // Check pressure
            if (_player.Pressure < 5f)
            {
                // not enough pressure, go back to move or idle
                if (_player.MoveDirection.sqrMagnitude > 0.001f)
                    ChangeState(new MoveState(_player, _stateMachine));
                else
                    ChangeState(new IdleState(_player, _stateMachine));
                return;
            }

            // Consume initial pressure
            _player.SetPressure(_player.Pressure - 5f);

            // If entering from air, neutralize Y velocity and mark as sprinted
            if (!_player.IsGrounded)
            {
                Vector3 v = _player.KinematicController.Velocity;
                v.y = 0f;
                _player.SetVelocityImmediate(v);
                _player.MarkSprintInAir();
                _enteredFromAir = true;
                _airSprintTimer = 0f;
            }
            else
            {
                _enteredFromAir = false;
            }

            // Subscribe to jump and dash press
            _player.Actions.Player.Jump.performed += OnJumpPressed;
            _player.Actions.Player.Dash.performed += OnDashPressed;

            // Start sprint through SprintController
            if (_sprintController != null)
            {
                _sprintController.TryStartSprint();
            }

            // Publish camera state update command
            _commandPublisher.PublishAsync(CameraStateUpdateCommand.WithDistanceAndFOV(3.1f, 110f, 3f));

            // Store default sprint time
            _defaultSprintTime = 0f;
        }

        public override void Exit(IState nextState = null)
        {
            // Unsubscribe
            _player.Actions.Player.Jump.performed -= OnJumpPressed;
            _player.Actions.Player.Dash.performed -= OnDashPressed;

            // Publish camera state update command to revert to default
            _commandPublisher.PublishAsync(new CameraStateUpdateCommand(transitionDuration: 0.3f));

            // Stop sprinting only if not transitioning to a state that allows sprint
            if (nextState is PlayerStateBase nextPlayerState &&
                (nextPlayerState.LocomotionType == LocomotionType.DashPanel ||
                 nextPlayerState.LocomotionType == LocomotionType.Grind))
            {
                // Keep sprinting
            }
            else
            {
                // Stop sprinting in the controller
                _sprintController?.StopSprint();
            }
        }

        public override void LogicUpdate()
        {
            // Kinematic controller handles automatic rotation toward movement direction

            // If sprint is released or no move input, go back to appropriate state
            if (!_player.SprintRequested)
            {
                if (_player.MoveDirection.sqrMagnitude > 0.001f)
                {
                    if (_player.IsGrounded)
                        ChangeState(new MoveState(_player, _stateMachine));
                    else
                    {
                        ChangeState(new FallState(_player, _stateMachine));
                    }
                }
                else
                {
                    if (_player.IsGrounded)
                        ChangeState(new SprintStopState(_player, _stateMachine));
                    else
                    {
                        ChangeState(new FallState(_player, _stateMachine));
                    }
                }
                return;
            }

            // Check for landing from air
            if (_enteredFromAir && _player.IsGrounded)
            {
                ChangeState(new LandState(_player, _stateMachine));
                return;
            }

            HandleSprint();
        }

        public override void PhysicsUpdate()
        {
            
        }

        private void HandleSprint()
        {
            // Sprint applies stronger acceleration and higher max speed
            if (_player.HasMovementOverride() && _player.IsOverrideExclusive())
            {
                _player.SetVelocityImmediate(_player.GetOverrideVelocity());
                return;
            }

            Vector3 desiredDirection = _player.MoveDirection;
            float targetSpeed = _player.RunSpeed;

            // Apply sprint speed modifier through controller
            if (_sprintController != null)
            {
                targetSpeed = _sprintController.GetModifiedSpeed(targetSpeed);
            }

            Vector3 currentVelocity = _player.KinematicController.Velocity;
            Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
            float currentSpeed = horizontalVelocity.magnitude;

            Vector3 newVelocity;

            if (desiredDirection.sqrMagnitude > 0.001f)
            {
                // Sprint acceleration (faster than normal movement)
                if (currentSpeed < targetSpeed)
                {
                    float sprintAcceleration = _player.AccelGround * 2f * Time.deltaTime; // Faster acceleration
                    Vector3 targetVelocity = desiredDirection.normalized * targetSpeed;
                    newVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, sprintAcceleration);
                }
                else
                {
                    // Maintain or slightly adjust speed
                    float deceleration = _player.Deceleration * 0.5f * Time.deltaTime; // Less deceleration
                    Vector3 targetVelocity = desiredDirection.normalized * targetSpeed;
                    newVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, deceleration);
                }
            }
            else
            {
                // No input - decelerate to stop (slower than normal for momentum)
                float deceleration = _player.Deceleration * 0.7f * Time.deltaTime;
                newVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, deceleration);
            }

            // Preserve vertical velocity (gravity)
            newVelocity.y = currentVelocity.y;

            // Set the calculated velocity
            _player.SetVelocityImmediate(newVelocity);

            // Update sprint time and effects through controller
            float deltaTime = Time.fixedDeltaTime;
            _defaultSprintTime += deltaTime;

            // Check air sprint timer
            if (_enteredFromAir && !_player.IsGrounded)
            {
                _airSprintTimer += deltaTime;
                if (_airSprintTimer >= 1f)
                {
                    ChangeState(new FallState(_player, _stateMachine));
                    return;
                }
            }

            // Update sprint effects through controller
            if (_sprintController != null)
            {
                float speedFactor = Mathf.Clamp01(currentSpeed / _player.RunSpeed);
                _sprintController.UpdateSprint(deltaTime, speedFactor);
            }

            if (!_sprintController.HasEnoughPressure(Time.deltaTime))
            {
                ChangeState(new MoveState(_player, _stateMachine));
                return;
            }

            // Handle air sprint gravity reduction
            if (!_player.IsGrounded)
            {
                // Reduce gravity during air sprint for straighter flight
                Vector3 reducedGravity = -Physics.gravity * 0.5f;
                _player.SetVelocityImmediate(_player.KinematicController.Velocity + reducedGravity * Time.fixedDeltaTime);
            }

            // Apply turn adjustments (more pronounced for sprint)
            _player.ApplyTurnAdjustments(_player.GetIKWeight(), _player.SprintRollMaxDegrees, 1.5f);
        }

        private void OnJumpPressed(InputAction.CallbackContext context)
        {
            if (_player.IsGrounded)
            {
                ChangeState(new JumpState(_player, _stateMachine));
            }
        }

        private void OnDashPressed(InputAction.CallbackContext context)
        {
            if (_player.CanDash())
            {
                Vector3 dashDir = _player.GetDashDirection();
                ChangeState(new DashState(_player, _stateMachine, dashDir, this));
            }
        }
    }
}
