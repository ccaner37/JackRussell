using JackRussell;
using JackRussell.CameraController;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using DG.Tweening;
using RootMotion.FinalIK;
using VitalRouter;

namespace JackRussell.States.Locomotion
{
    public class SprintState : PlayerStateBase
    {
        private float _defaultLensDistortion = 0f;
        private float _defaultChromaticAberration = 0f;
        private float _defaultGlitchAmount = 0f;
        private float _sprintTime = 0f;
        private float _airSprintTimer = 0f;
        private bool _enteredFromAir;
        private CinemachineCameraController _cameraController;
        private Volume _volume;
        private LensDistortion _lensDistortion;
        private ChromaticAberration _chromaticAberration;
        private RendererController _rendererController;
        private ICommandPublisher _commandPublisher;

        public SprintState(Player player, StateMachine stateMachine) : base(player, stateMachine)
        {
            _commandPublisher = player.CommandPublisher;
        }

        public override string Name => nameof(SprintState);

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

            // Consume pressure
            _player.SetPressure(_player.Pressure - 5f);

            // If entering from air, neutralize Y velocity and mark as sprinted
            if (!_player.IsGrounded)
            {
                Vector3 v = _player.Rigidbody.linearVelocity;
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

            // Subscribe to jump press
            _player.Actions.Player.Jump.performed += OnJumpPressed;

            // Set animator sprint flag if desired
            _player.OnSprintEnter();
            _player.Animator.SetBool(Animator.StringToHash("IsSprinting"), true);
            _player.PlaySound(Audio.SoundType.SprintStart);
            if (_player.IsGrounded) _player.PlaySprintSpeedUpSound();

            // Enable smoke effects
            _player.EnableSmokeEffects();

            // Publish camera state update command
            _commandPublisher.PublishAsync(new CameraStateUpdateCommand(3.2f, 90f));

            // Find components
            _cameraController = Object.FindObjectOfType<CinemachineCameraController>();
            _volume = _cameraController.Volume;
            _rendererController = Object.FindObjectOfType<RendererController>();
            if (_volume != null && _volume.profile != null)
            {
                _volume.profile.TryGet<LensDistortion>(out _lensDistortion);
                _volume.profile.TryGet<ChromaticAberration>(out _chromaticAberration);
            }

            // Store defaults
            _sprintTime = 0f;
            if (_lensDistortion != null)
            {
                _defaultLensDistortion = _lensDistortion.intensity.value;
            }
            if (_chromaticAberration != null)
            {
                _defaultChromaticAberration = _chromaticAberration.intensity.value;
            }
            if (_player.PlayerMaterial != null)
            {
                _defaultGlitchAmount = _player.PlayerMaterial.GetFloat("_GlitchAmount");
                _player.PlayerMaterial.EnableKeyword("_GLITCH_ON");
            }
        }

        public override void Exit()
        {
            // Unsubscribe
            _player.Actions.Player.Jump.performed -= OnJumpPressed;

            _player.Animator.SetBool(Animator.StringToHash("IsSprinting"), false);
            _player.StopSprintSpeedUp();
            _player.OnSprintExit();

            // Disable smoke effects with delay
            _player.DisableSmokeEffects();

            // Publish camera state update command to revert to default
            _commandPublisher.PublishAsync(new CameraStateUpdateCommand(2.65f, 75f));

            // Revert sprint effects
            if (_lensDistortion != null)
            {
                _lensDistortion.intensity.value = _defaultLensDistortion;
            }
            if (_chromaticAberration != null)
            {
                _chromaticAberration.intensity.value = _defaultChromaticAberration;
            }
            if (_player.PlayerMaterial != null)
            {
                _player.PlayerMaterial.SetFloat("_GlitchAmount", _defaultGlitchAmount);
                _player.PlayerMaterial.DisableKeyword("_GLITCH_ON");
            }
            if (_rendererController != null)
            {
                _rendererController.SetSpeedLinesIntensity(0f);
            }
        }

        public override void LogicUpdate()
        {
            // Rotate toward movement direction
            _player.RotateTowardsDirection(_player.MoveDirection, Time.deltaTime, isAir: !_player.IsGrounded);

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

            // Jump handled by event subscription

            // Attack => boost
            if (_player.ConsumeAttackRequest())
            {
                ChangeState(new BoostState(_player, _stateMachine));
                return;
            }
        }

        public override void PhysicsUpdate()
        {
            // Sprint applies stronger acceleration and higher max speed
            if (_player.HasMovementOverride() && _player.IsOverrideExclusive())
            {
                _player.SetVelocityImmediate(_player.GetOverrideVelocity());
                return;
            }

            Vector3 desired = _player.MoveDirection;
            float targetSpeed = _player.RunSpeed;

            Vector3 currentVel = new Vector3(_player.Rigidbody.linearVelocity.x, 0f, _player.Rigidbody.linearVelocity.z);
            float currentSpeed = currentVel.magnitude;

            Vector3 force;
            if (currentSpeed < targetSpeed)
            {
                force = desired.normalized * (_player.AccelGround * 10) - currentVel * (_player.Damping * 0.5f);
            }
            else
            {
                force = -currentVel.normalized * _player.Deceleration;
            }

            _player.AddGroundForce(force);

            // Update sprint time
            _sprintTime += Time.fixedDeltaTime;

            // Check air sprint timer
            if (_enteredFromAir && !_player.IsGrounded)
            {
                _airSprintTimer += Time.fixedDeltaTime;
                if (_airSprintTimer >= 0.4f)
                {
                    ChangeState(new FallState(_player, _stateMachine));
                    return;
                }
            }

            // Update sprint effects
            float factor = Mathf.Clamp01(currentSpeed / _player.RunSpeed);
            UpdateSprintEffects(factor);

            // Project velocity onto ground plane to keep movement along the surface
            if (_player.IsGrounded)
            {
                Vector3 projectedVel = Vector3.ProjectOnPlane(_player.Rigidbody.linearVelocity, _player.GroundNormal);
                _player.Rigidbody.linearVelocity = projectedVel;
            }
            else
            {
                // Reduce gravity during air sprint for straighter flight
                _player.AddGroundForce(-Physics.gravity * 0.5f);
            }

            // Apply turn adjustments (more pronounced for sprint)
            _player.ApplyTurnAdjustments(_player.GetIKWeight(), _player.SprintRollMaxDegrees, 1.5f);

        }

        private void UpdateSprintEffects(float factor)
        {
            float effectValue = factor;
            if (_player.GlitchCurve != null)
            {
                effectValue = _player.GlitchCurve.Evaluate(_sprintTime);
            }

            // Lens Distortion
            if (_lensDistortion != null)
            {
                _lensDistortion.intensity.value = -0.1f * factor;
            }
            // Chromatic Aberration
            if (_chromaticAberration != null)
            {
                _chromaticAberration.intensity.value = 1f * effectValue;
            }
            // Glitch
            if (_player.PlayerMaterial != null)
            {
                float glitchValue = 0.6f * effectValue;
                _player.PlayerMaterial.SetFloat("_GlitchAmount", glitchValue);
            }
            // Speed Lines
            if (_rendererController != null)
            {
                _rendererController.SetSpeedLinesIntensity(factor);
            }
        }

        private void OnJumpPressed(InputAction.CallbackContext context)
        {
            if (_player.IsGrounded)
            {
                ChangeState(new JumpState(_player, _stateMachine));
            }
        }
    }
}
