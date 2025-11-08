using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using JackRussell.Rails;
using System;
using UnityEngine.Rendering;
using VitalRouter;
using DG.Tweening;
using VContainer;
using JackRussell.CameraController;

namespace JackRussell.CameraController
{
    /// <summary>
    /// Cinemachine-based orbital camera controller with collision detection and state-based adjustments.
    /// Includes robust camera priority management system for multiple camera types.
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

        [Header("Camera Management")]
        [Tooltip("List of camera definitions for different camera types")]
        [SerializeField] private CameraDefinition[] _cameraDefinitions;

        // Input actions
        private InputSystem_Actions _actions;

        // Runtime state
        private CinemachineOrbitalFollow _orbitalFollow;
        private float _currentRadius;
        private float _targetRadius;
        private float _radiusVelocity;
        private float _defaultRadius;

        // DOTween references
        private Tween _radiusTween;
        private Tween _fovTween;

        private float _targetFov;
        private float _defaultFov;

        public Volume Volume;

        [Inject] private readonly ICommandSubscribable _commandSubscribable;

        private void Awake()
        {
            _actions = new InputSystem_Actions();

            // Initialize camera definitions
            InitializeCameraDefinitions();

            // Get the OrbitalFollow component
            if (_cinemachineCamera != null)
            {
                _orbitalFollow = _cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
                if (_orbitalFollow != null)
                {
                    _currentRadius = _orbitalFollow.Radius;
                    _defaultRadius = _currentRadius;
                    _targetRadius = _currentRadius;
                    _defaultFov = _cinemachineCamera.Lens.FieldOfView;
                    _targetFov = _defaultFov;
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

            // Subscribe to camera state update commands
            _commandSubscribable.Subscribe<CameraStateUpdateCommand>((cmd, ctx) => OnCameraStateUpdate(cmd));
            
            // Subscribe to camera switch commands
            _commandSubscribable.Subscribe<CameraSwitchCommand>((cmd, ctx) => OnCameraSwitch(cmd));
            
            // Subscribe to camera shake commands
            _commandSubscribable.Subscribe<CameraShakeCommand>((cmd, ctx) => OnCameraShake(cmd));
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
            // No polling needed - camera updates are event-driven via commands
        }

        private void OnCameraStateUpdate(CameraStateUpdateCommand command)
        {
            // Animate camera distance
            _targetRadius = command.TargetDistance ?? _defaultRadius;
            if (Mathf.Abs(_orbitalFollow.Radius - _targetRadius) > 0.01f)
            {
                _radiusTween?.Kill();
                _radiusTween = DOTween.To(() => _orbitalFollow.Radius, x => _orbitalFollow.Radius = x, _targetRadius, command.TransitionDuration)
                    .SetEase(Ease.OutQuad);
            }

            // Animate FOV
            _targetFov = command.TargetFOV ?? _defaultFov;
            if (Mathf.Abs(_cinemachineCamera.Lens.FieldOfView - _targetFov) > 0.01f)
            {
                _fovTween?.Kill();
                _fovTween = DOTween.To(() => _cinemachineCamera.Lens.FieldOfView, x => _cinemachineCamera.Lens.FieldOfView = x, _targetFov, command.TransitionDuration)
                    .SetEase(Ease.OutQuad);
            }
        }

        /// <summary>
        /// Handles camera switch commands
        /// </summary>
        private void OnCameraSwitch(CameraSwitchCommand command)
        {
            SwitchToCamera(command.TargetCamera, command.TransitionDuration);
        }

        /// <summary>
        /// Handles camera shake commands
        /// </summary>
        private void OnCameraShake(CameraShakeCommand command)
        {
            ShakeCamera(command.Intensity, command.Duration);
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
        /// Switches to a specific camera type with smooth transition
        /// </summary>
        /// <param name="cameraType">The camera type to switch to</param>
        /// <param name="transitionDuration">Transition duration (0 for instant)</param>
        public void SwitchToCamera(CameraType cameraType, float transitionDuration = 0.3f)
        {
            // First, disable all cameras
            DisableAllCameras();
            
            // Then activate the target camera
            var targetCamera = GetCamera(cameraType);
            if (targetCamera != null && targetCamera.IsValid)
            {
                targetCamera.SetActive(true);
            }
        }

        /// <summary>
        /// Disables all cameras by setting their priority to 0
        /// </summary>
        private void DisableAllCameras()
        {
            if (_cameraDefinitions == null) return;
            
            foreach (var camera in _cameraDefinitions)
            {
                if (camera != null && camera.IsValid)
                {
                    camera.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Gets a camera definition by type
        /// </summary>
        public CameraDefinition GetCamera(CameraType cameraType)
        {
            if (_cameraDefinitions == null) return null;
            
            foreach (var camera in _cameraDefinitions)
            {
                if (camera != null && camera.Type == cameraType)
                    return camera;
            }
            return null;
        }

        /// <summary>
        /// Sets a specific camera type active or inactive
        /// </summary>
        public void SetCameraActive(CameraType cameraType, bool isActive)
        {
            var camera = GetCamera(cameraType);
            if (camera != null && camera.IsValid)
            {
                camera.SetActive(isActive);
            }
        }

        /// <summary>
        /// Initializes camera definitions and sets up initial state
        /// </summary>
        private void InitializeCameraDefinitions()
        {
            if (_cameraDefinitions == null) return;

            // First, disable all cameras
            DisableAllCameras();

            // Then enable the ones that should be active at start
            foreach (var camera in _cameraDefinitions)
            {
                if (camera != null && camera.IsValid && camera.EnabledAtStart)
                {
                    camera.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Gets the CinemachineCamera component.
        /// </summary>
        public CinemachineCamera GetCinemachineCamera() => _cinemachineCamera;

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