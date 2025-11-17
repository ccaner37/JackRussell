using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using JackRussell.GamePostProcessing;
using System;
using JackRussell;
using VContainer;

namespace JackRussell
{
    /// <summary>
    /// Shared controller for sprint functionality across different states.
    /// Handles speed boosts, effects, VFX/SFX, and pressure management.
    /// </summary>
    public class SprintController : MonoBehaviour
    {
        [Header("Sprint Settings")]
        //[SerializeField] private float _sprintSpeedMultiplier = 1.8f;
        [SerializeField] private float _sprintPressureCost = 3f; // Pressure cost per second
        [SerializeField] private float _sprintEffectIntensity = 1f;

        // Events
        public event Action<bool> OnSprintStateChanged;
        public event Action<float> OnSprintIntensityChanged;

        // State
        public bool IsSprinting => _player.IsSprinting;
        public float CurrentIntensity { get; private set; }
        public float SprintTime { get; private set; }

        // References
        private Player _player;
        
        [Inject]
        private readonly PostProcessingController _postProcessingController;
        
        // Post-processing effects
        private LensDistortion _lensDistortion;
        private ChromaticAberration _chromaticAberration;
        private float _defaultLensDistortion = 0f;
        private float _defaultChromaticAberration = 0f;
        private float _defaultGlitchAmount = 0f;
        public float PressureCostPerSecond => _sprintPressureCost;

        private void Awake()
        {
            _player = GetComponent<Player>();
            if (_player == null)
            {
                Debug.LogError("SprintController requires a Player component!");
                return;
            }

            InitializePostProcessing();
        }

        private void InitializePostProcessing()
        {
            _postProcessingController.Volume.profile.TryGet<LensDistortion>(out _lensDistortion);
            _postProcessingController.Volume.profile.TryGet<ChromaticAberration>(out _chromaticAberration);

            // Store defaults
            if (_lensDistortion != null)
            {
                _defaultLensDistortion = _lensDistortion.intensity.value;
            }
            if (_chromaticAberration != null)
            {
                _defaultChromaticAberration = _chromaticAberration.intensity.value;
            }
        }

        /// <summary>
        /// Start sprinting if conditions are met
        /// </summary>
        public bool TryStartSprint()
        {
            if (IsSprinting) return true;
            SprintTime = 0f;
            CurrentIntensity = 0f;

            // Enable visual effects
            _player.OnSprintEnter();
            _player.Animator.SetBool(Animator.StringToHash("IsSprinting"), true);
            _player.PlaySound(Audio.SoundType.SprintStart);
            _player.EnableSmokeEffects();

            if (_player.IsGrounded ||_player.IsRailGrinding) _player.PlaySprintSpeedUpSound();

            // Enable glitch effect
            if (_player.PlayerMaterial != null)
            {
                _player.PlayerMaterial.EnableKeyword("_GLITCH_ON");
            }

            OnSprintStateChanged?.Invoke(true);
            Debug.Log("[SprintController] Started sprint");

            return true;
        }

        /// <summary>
        /// Stop sprinting
        /// </summary>
        public void StopSprint()
        {
            if (!IsSprinting) return;
            
            CurrentIntensity = 0f;

            // Disable visual effects
            _player.Animator.SetBool(Animator.StringToHash("IsSprinting"), false);
            _player.StopSprintSpeedUpSound();
            _player.DisableSmokeEffects();
            _player.OnSprintExit();

            // Disable glitch effect
            if (_player.PlayerMaterial != null)
            {
                _player.PlayerMaterial.DisableKeyword("_GLITCH_ON");
            }

            // Cleanup post-processing effects
            CleanupPostProcessingEffects();

            OnSprintStateChanged?.Invoke(false);
            Debug.Log("[SprintController] Stopped sprint");
        }

        /// <summary>
        /// Update sprint effects and consume pressure
        /// </summary>
        public void UpdateSprint(float deltaTime, float speedFactor = 1f)
        {
            if (!IsSprinting) return;

            // Update timer
            SprintTime += deltaTime;

            // Calculate intensity based on speed
            CurrentIntensity = Mathf.Clamp01(speedFactor * _sprintEffectIntensity);
            OnSprintIntensityChanged?.Invoke(CurrentIntensity);

            // Consume pressure
            float pressureCost = _sprintPressureCost * deltaTime;
            if (_player.Pressure >= pressureCost)
            {
                _player.SetPressure(_player.Pressure - pressureCost);
            }
            else
            {
                // Not enough pressure, stop sprinting
                StopSprint();
                return;
            }

            // Update visual effects
            UpdatePostProcessingEffects();
        }

        /// <summary>
        /// Get sprint-modified speed
        /// </summary>
        public float GetModifiedSpeed(float baseSpeed)
        {
            //return IsSprinting ? baseSpeed * _sprintSpeedMultiplier : baseSpeed;
            return baseSpeed;
        }

        /// <summary>
        /// Check if sprinting is available
        /// </summary>
        public bool CanSprint()
        {
            return _player.Pressure >= _sprintPressureCost;
        }

        private void UpdatePostProcessingEffects()
        {
            float effectValue = CurrentIntensity;
            if (_player.GlitchCurve != null)
            {
                effectValue = _player.GlitchCurve.Evaluate(SprintTime);
            }

            // Lens Distortion
            if (_lensDistortion != null)
            {
                _lensDistortion.intensity.value = -0.1f * CurrentIntensity;
            }

            // Chromatic Aberration
            if (_chromaticAberration != null)
            {
                _chromaticAberration.intensity.value = 1f * effectValue;
            }

            // Glitch effect
            if (_player.PlayerMaterial != null)
            {
                float glitchValue = 0.6f * effectValue;
                _player.PlayerMaterial.SetFloat("_GlitchAmount", glitchValue);
            }

            // Speed lines and radial blur
            if (_postProcessingController != null)
            {
                _postProcessingController.SetSpeedLinesIntensity(CurrentIntensity);
                _postProcessingController.SetRadialBlurIntensity(CurrentIntensity * 0.4f);
                _postProcessingController.SetRadialMotionBlurIntensity(CurrentIntensity * 0.08f);
            }
        }

        private void CleanupPostProcessingEffects()
        {
            // Revert post-processing effects
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
            }
            if (_postProcessingController != null)
            {
                _postProcessingController.SetSpeedLinesIntensity(0f);
                _postProcessingController.SetRadialBlurIntensity(0f);
                _postProcessingController.SetRadialMotionBlurIntensity(0f);
            }
        }

        private void OnDestroy()
        {
            // Ensure cleanup on destroy
            if (IsSprinting)
            {
                CleanupPostProcessingEffects();
            }
        }
    }
}