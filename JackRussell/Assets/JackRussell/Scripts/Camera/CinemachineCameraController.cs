using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using JackRussell.Rails;
using System;

namespace JackRussell.CameraController
{
    /// <summary>
    /// Cinemachine-based orbital camera controller with collision detection and state-based adjustments.
    /// Requires manually set up Cinemachine components in the scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class CinemachineCameraController : MonoBehaviour
    {
        [Header("Cinemachine Components")]
        [Tooltip("The Cinemachine Camera for orbital view")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;

        [Tooltip("The Cinemachine Deoccluder for automatic collision detection")]
        [SerializeField] private CinemachineDeoccluder _cinemachineDeoccluder;

        [Tooltip("The Cinemachine Input Axis Controller for input handling")]
        [SerializeField] private CinemachineInputAxisController _inputAxisController;

        [Header("Target")]
        [Tooltip("Transform to follow (usually the Player root)")]
        [SerializeField] private Transform _target;

        [Header("Camera Settings")]
        [Tooltip("Default camera distance")]
        [SerializeField] private float _defaultDistance = 5f;

        [Tooltip("Camera distance during sprint")]
        [SerializeField] private float _sprintDistance = 6f;

        [Tooltip("Camera distance during grinding")]
        [SerializeField] private float _grindDistance = 4f;

        [Tooltip("How quickly camera distance adjusts")]
        [SerializeField] private float _distanceSmoothTime = 0.5f;

        [Header("Shake Settings")]
        [Tooltip("Default shake intensity")]
        [SerializeField] private float _defaultShakeIntensity = 0.1f;

        [Tooltip("Shake duration in seconds")]
        [SerializeField] private float _shakeDuration = 0.5f;

        [Tooltip("Shake frequency")]
        [SerializeField] private float _shakeFrequency = 10f;

        // Input actions
        private InputSystem_Actions _actions;

        // Runtime state
        private CinemachineOrbitalFollow _orbitalFollow;
        private float _currentRadius;
        private float _targetRadius;
        private float _radiusVelocity;

        private void Awake()
        {
            _actions = new InputSystem_Actions();

            // Get the OrbitalFollow component
            if (_cinemachineCamera != null)
            {
                _orbitalFollow = _cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
                if (_orbitalFollow != null)
                {
                    _currentRadius = _orbitalFollow.Radius;
                    _targetRadius = _currentRadius;
                }
            }

            // Configure Cinemachine Deoccluder if present
            if (_cinemachineDeoccluder != null)
            {
                // Configure Deoccluder properties - these will be set in the inspector
                // User can manually configure collision layers and damping in the editor
            }

            // Set up CinemachineInputAxisController if not assigned
            if (_inputAxisController == null)
            {
                _inputAxisController = GetComponent<CinemachineInputAxisController>();
            }
        }

        private void OnEnable()
        {
            _actions.Player.Enable();
        }

        private void OnDisable()
        {
            _actions.Player.Disable();
            _actions.Dispose();
        }

        private void LateUpdate()
        {
            if (_target == null || _orbitalFollow == null) return;

            // Check player states
            bool isSprinting = IsPlayerSprinting();
            bool isGrinding = IsPlayerGrinding();

            // Adjust camera radius based on state
            if (isSprinting)
            {
                _targetRadius = _sprintDistance;
            }
            else if (isGrinding)
            {
                _targetRadius = _grindDistance;
            }
            else
            {
                _targetRadius = _defaultDistance;
            }

            // Smooth radius transition
            _currentRadius = Mathf.SmoothDamp(_currentRadius, _targetRadius, ref _radiusVelocity, _distanceSmoothTime);
            _orbitalFollow.Radius = _currentRadius;

            // Handle grinding camera adjustments
            if (isGrinding)
            {
                AdjustGrindingCamera();
            }
        }

        /// <summary>
        /// Triggers camera shake using Cinemachine Impulse.
        /// </summary>
        public void ShakeCamera(float intensity = -1f, float duration = -1f)
        {
            float shakeIntensity = intensity > 0 ? intensity : _defaultShakeIntensity;
            float shakeDuration = duration > 0 ? duration : _shakeDuration;

            // Create impulse source if it doesn't exist
            var impulseSource = FindAnyObjectByType<CinemachineImpulseSource>();

            // Generate impulse
            Vector3 impulseVelocity = new Vector3(
                UnityEngine.Random.Range(-shakeIntensity, shakeIntensity),
                UnityEngine.Random.Range(-shakeIntensity, shakeIntensity),
                UnityEngine.Random.Range(-shakeIntensity, shakeIntensity)
            );

            impulseSource.GenerateImpulseWithVelocity(impulseVelocity);
        }

        /// <summary>
        /// Sets the camera target at runtime.
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
            if (_cinemachineCamera != null)
            {
                _cinemachineCamera.Follow = target;
                _cinemachineCamera.LookAt = target;
            }
        }

        /// <summary>
        /// Clears the camera target.
        /// </summary>
        public void ClearTarget()
        {
            _target = null;
            if (_cinemachineCamera != null)
            {
                _cinemachineCamera.Follow = null;
                _cinemachineCamera.LookAt = null;
            }
        }

        private bool IsPlayerSprinting()
        {
            if (_target == null) return false;

            var player = _target.GetComponent<JackRussell.Player>();
            if (player == null) return false;

            return player.LocomotionStateName == "SprintState";
        }

        private bool IsPlayerGrinding()
        {
            if (_target == null) return false;

            var player = _target.GetComponent<JackRussell.Player>();
            if (player == null) return false;

            return player.LocomotionStateName == "GrindState";
        }

        private void AdjustGrindingCamera()
        {
            // if (_target == null) return;

            // var player = _target.GetComponent<JackRussell.Player>();
            // if (player == null) return;

            // var railDetector = player.GetComponentInChildren<RailDetector>();
            // if (railDetector == null || !railDetector.IsAttached) return;

            // // Get rail position and tangent for look-ahead
            // if (railDetector.GetCurrentRailPosition(out Vector3 railPos, out Vector3 tangent))
            // {
            //     // Look ahead along the rail
            //     float lookAheadDistance = 3f;
            //     SplineRail currentRail = railDetector.CurrentRail;

            //     if (currentRail != null)
            //     {
            //         float currentDistance = railDetector.CurrentDistance;
            //         float lookAheadT = (currentDistance + lookAheadDistance) / currentRail.TotalLength;
            //         lookAheadT = Mathf.Clamp01(lookAheadT);

            //         if (currentRail.GetPositionAndTangent(lookAheadT * currentRail.TotalLength, out Vector3 lookAheadPos, out Vector3 _))
            //         {
            //             // Adjust camera to look ahead
            //             if (_cinemachineCamera != null)
            //             {
            //                 _cinemachineCamera.LookAt = null; // Temporarily disable look-at to manually control
            //                 // The OrbitalFollow will handle the basic following, we could add custom look-ahead here
            //             }
            //         }
            //     }
            // }
        }

        // Editor validation
        private void OnValidate()
        {
            if (_cinemachineCamera != null && _orbitalFollow == null)
            {
                _orbitalFollow = _cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
            }
        }
    }
}