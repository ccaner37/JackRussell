using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// Third-person camera controller:
/// - Follows a target (player) at a configurable distance and pitch limits
/// - Smooth position & rotation following
/// - Supports look input from InputSystem_Actions.Player.Look (mouse delta / gamepad right stick)
/// - Collision push-in (spherecast) to avoid clipping through level geometry
/// - Exposes simple public API to set/clear target at runtime
/// </summary>
namespace JackRussell.CameraController
{
    [DisallowMultipleComponent]
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Transform to follow (usually the Player root)")]
        [SerializeField] private Transform _target;

        [Tooltip("Offset from the target's position where camera pivots around (e.g. head height)")]
        [SerializeField] private Vector3 _pivotOffset = new Vector3(0f, 1.6f, 0f);

        [Header("Distance")]
        [SerializeField] private float _defaultDistance = 5f;
        [SerializeField] private float _minDistance = 0.5f;
        [SerializeField] private float _maxDistance = 7f;

        [Header("Pitch (vertical)")]
        [SerializeField] private float _minPitch = -10f;
        [SerializeField] private float _maxPitch = 60f;
        [SerializeField] private float _initialPitch = 10f;

        [Header("Smoothing")]
        [SerializeField] private float _positionSmoothTime = 0.12f;
        [SerializeField] private float _rotationSmoothSpeed = 8f;

        [Header("Look/Input")]
        [SerializeField] private float _sensitivity = 1.0f;
        [SerializeField] private bool _invertY = false;
        [SerializeField] private bool _lockCursor = false;

        [Header("Collision")]
        [SerializeField] private bool _enableCollision = true;
        [SerializeField] private LayerMask _collisionLayers = ~0; // everything by default
        [SerializeField] private float _collisionRadius = 0.35f;
        [SerializeField] private float _collisionPushBack = 0.08f; // small offset to keep camera off geometry

        [Header("Shake")]
        [SerializeField] private float _shakeDuration = 0.5f;
        [SerializeField] private float _shakeIntensity = 0.1f;
        [SerializeField] private float _shakeFrequency = 10f;
        [SerializeField] private AnimationCurve _shakeDecay = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));

        // Input actions
        private InputSystem_Actions _actions;

        // runtime state
        private float _yaw;
        private float _pitch;
        private float _currentDistance;
        private Vector3 _currentVelocity = Vector3.zero;

        private Transform _shakeTransform;
        private Vector3 _shakeOffset = Vector3.zero;

        private void Awake()
        {
            _actions = new InputSystem_Actions();

            // initialize angles
            _yaw = transform.eulerAngles.y;
            _pitch = Mathf.Clamp(_initialPitch, _minPitch, _maxPitch);
            _currentDistance = Mathf.Clamp(_defaultDistance, _minDistance, _maxDistance);

            // create dummy transform for shake
            _shakeTransform = new GameObject("CameraShake").transform;
            _shakeTransform.SetParent(transform);
            _shakeTransform.localPosition = Vector3.zero;
            _shakeTransform.localRotation = Quaternion.identity;

            if (_lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnEnable()
        {
            _actions.Player.Enable();
        }

        private void OnDisable()
        {
            _actions.Player.Disable();
            if (_lockCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // Read look input (could be mouse delta or gamepad right stick)
            Vector2 lookInput = _actions.Player.Look.ReadValue<Vector2>();
            float deltaMultiplier = DetermineLookMultiplier();

            float lookX = lookInput.x * _sensitivity * deltaMultiplier;
            float lookY = lookInput.y * _sensitivity * deltaMultiplier;

            // apply to yaw/pitch (invert Y if requested)
            _yaw += lookX;
            _pitch += (_invertY ? lookY : -lookY);
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

            // Desired camera position (relative to pivot)
            Vector3 pivotWorld = _target.position + _pivotOffset;

            Quaternion camRot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desiredLocalOffset = camRot * (Vector3.back * _defaultDistance);
            Vector3 desiredWorldPos = pivotWorld + desiredLocalOffset;

            // Collision: spherecast from pivot to desired position
            float targetDistance = _defaultDistance;
            if (_enableCollision)
            {
                Vector3 dir = desiredWorldPos - pivotWorld;
                float dist = dir.magnitude;
                if (dist > 0.001f)
                {
                    if (Physics.SphereCast(pivotWorld, _collisionRadius, dir.normalized, out RaycastHit hit, dist, _collisionLayers, QueryTriggerInteraction.Ignore))
                    {
                        float hitDist = Mathf.Max(hit.distance - _collisionPushBack, _minDistance);
                        targetDistance = Mathf.Clamp(hitDist, _minDistance, _maxDistance);
                    }
                }
            }

            // Smooth distance change to avoid pops
            _currentDistance = Mathf.Lerp(_currentDistance, targetDistance, 1f - Mathf.Exp(-10f * Time.deltaTime));

            // recompute desired world pos using smoothed distance
            desiredWorldPos = pivotWorld + camRot * (Vector3.back * _currentDistance);

            // Apply shake offset
            desiredWorldPos += _shakeTransform.localPosition;

            // Smooth position
            transform.position = Vector3.SmoothDamp(transform.position, desiredWorldPos, ref _currentVelocity, _positionSmoothTime);

            // Smooth rotation to look at pivot
            Quaternion desiredLookRot = Quaternion.LookRotation(pivotWorld - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredLookRot, Mathf.Clamp01(_rotationSmoothSpeed * Time.deltaTime));
        }

        /// <summary>
        /// Heuristic to pick a multiplier so mouse delta (pixels) and gamepad delta (unit axis) both feel reasonable.
        /// If a meaningful mouse delta exists this assumes mouse and scales it down; otherwise it uses an approx per-second scaling.
        /// </summary>
        private float DetermineLookMultiplier()
        {
            // If a mouse device exists and produces a delta, treat input as raw pixel delta -> scale down heavily.
            if (Mouse.current != null)
            {
                Vector2 md = Mouse.current.delta.ReadValue();
                if (md.sqrMagnitude > 0.0001f)
                    return 0.02f; // tuned so pixel deltas become reasonable
            }

            // Otherwise assume gamepad / joystick input and scale per-frame by deltaTime to make rate consistent.
            return Time.deltaTime * 120f;
        }

        /// <summary>
        /// Public API: assign a follow target at runtime.
        /// </summary>
        public void SetTarget(Transform t)
        {
            _target = t;
            if (_target != null)
            {
                // immediately place camera behind target
                _yaw = _target.eulerAngles.y;
            }
        }

        /// <summary>
        /// Clears the follow target (camera will stop updating).
        /// </summary>
        public void ClearTarget()
        {
            _target = null;
        }

        /// <summary>
        /// Triggers camera shake effect using DOTween punch.
        /// </summary>
        /// <param name="duration">Duration of shake in seconds. Uses default if <=0.</param>
        /// <param name="intensity">Intensity of shake. Uses default if <=0.</param>
        public void ShakeCamera(float duration = -1f, float intensity = -1f)
        {
            float dur = duration > 0 ? duration : _shakeDuration;
            float str = intensity > 0 ? intensity : _shakeIntensity;
            _shakeTransform.DOPunchPosition(Vector3.one * str, dur, 20, 1f);
        }

        // Editor convenience: draw pivot + collision sphere
        private void OnDrawGizmosSelected()
        {
            if (_target == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_target.position + _pivotOffset, 0.05f);

            if (_enableCollision)
            {
                Gizmos.color = Color.yellow;
                Vector3 pivotWorld = _target.position + _pivotOffset;
                Quaternion camRot = Quaternion.Euler(_pitch, _yaw, 0f);
                Vector3 desired = pivotWorld + camRot * (Vector3.back * _defaultDistance);
                Gizmos.DrawLine(pivotWorld, desired);
                Gizmos.DrawWireSphere(desired, _collisionRadius);
            }
        }
    }
}
