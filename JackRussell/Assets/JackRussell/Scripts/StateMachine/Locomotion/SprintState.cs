using JackRussell;
using JackRussell.CameraController;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using DG.Tweening;

namespace JackRussell.States.Locomotion
{
    public class SprintState : PlayerStateBase
    {
        private float _defaultFOV = 60f;
        private float _defaultLensDistortion = 0f;
        private float _defaultChromaticAberration = 0f;
        private float _defaultGlitchAmount = 0f;
        private float _sprintTime = 0f;
        private CinemachineCameraController _cameraController;
        private Volume _volume;
        private LensDistortion _lensDistortion;
        private ChromaticAberration _chromaticAberration;
        private RendererController _rendererController;

        public SprintState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

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
            }

            // Set animator sprint flag if desired
            _player.OnSprintEnter();
            _player.Animator.SetBool(Animator.StringToHash("IsSprinting"), true);
            _player.PlaySound(Audio.SoundType.SprintStart);
            _player.PlaySprintSpeedUp();

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
            if (_cameraController != null && _cameraController.GetCinemachineCamera() != null)
            {
                _defaultFOV = _cameraController.GetCinemachineCamera().Lens.FieldOfView;
            }
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
            _player.Animator.SetBool(Animator.StringToHash("IsSprinting"), false);
            _player.StopSprintSpeedUp();

            // Revert sprint effects
            if (_cameraController != null && _cameraController.GetCinemachineCamera() != null)
            {
                _cameraController.GetCinemachineCamera().Lens.FieldOfView = _defaultFOV;
            }
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
                        ChangeState(new FallState(_player, _stateMachine));
                }
                else
                {
                    if (_player.IsGrounded)
                        ChangeState(new IdleState(_player, _stateMachine));
                    else
                        ChangeState(new FallState(_player, _stateMachine));
                }
                return;
            }

            // Jump
            if (_player.ConsumeJumpRequest() && _player.IsGrounded)
            {
                ChangeState(new JumpState(_player, _stateMachine));
                return;
            }

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
                force = desired.normalized * (_player.AccelGround * 10) - currentVel * _player.Damping;
            }
            else
            {
                force = -currentVel * _player.Damping;
            }

            _player.AddGroundForce(force);

            // Update sprint time
            _sprintTime += Time.fixedDeltaTime;

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
                _player.AddGroundForce(-Physics.gravity * 0.5f); // counteract 50% of gravity
            }

        }

        private void UpdateSprintEffects(float factor)
        {
            float effectValue = factor;
            if (_player.GlitchCurve != null)
            {
                effectValue = _player.GlitchCurve.Evaluate(_sprintTime);
            }

            // FOV
            if (_cameraController != null && _cameraController.GetCinemachineCamera() != null)
            {
                float targetFOV = _defaultFOV + (80f - _defaultFOV) * factor;
                _cameraController.GetCinemachineCamera().Lens.FieldOfView = targetFOV;
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
    }
}
