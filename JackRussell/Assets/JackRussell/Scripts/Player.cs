using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using JackRussell.States;
using JackRussell.States.Locomotion;
using JackRussell.States.Action;
using VContainer;
using JackRussell.Audio;
using JackRussell.Rails;
using VitalRouter;
using UnityEngine.VFX;
using DG.Tweening;
using AllIn13DShader;
using RootMotion.FinalIK;
using System.Collections;
using HoaxGames;
using JackRussell.GamePostProcessing;

namespace JackRussell
{
    // Player acts as the context for states (locomotion + action).
    // It exposes a small public API that states use to manipulate physics/animator and query inputs.
    public class Player : MonoBehaviour
    {
        // Serialized inspector fields (user requested: use serialized fields, "_" prefix)
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private LayerMask _groundMask;
        [SerializeField] private RailDetector _railDetector;

        [Header("Speeds")]
        [SerializeField] private float _walkSpeed = 6f;
        [SerializeField] private float _runSpeed = 12f;
        [SerializeField] private float _boostSpeed = 28f;
        [SerializeField] private float _dashSpeed = 30f;

        [Header("Acceleration")]
        [SerializeField] private float _accelGround = 60f;
        [SerializeField] private float _accelAir = 20f;
        [SerializeField] private float _deceleration = 80f;
        [SerializeField] private float _damping = 5f;

        [Header("Jump & Gravity")]
        [SerializeField] private float _jumpVelocity = 10f;
        [SerializeField] private float _gravityMultiplier = 1f;

        [Header("Ground Check")]
        [SerializeField] private float _groundCheckRadius = 0.25f;
        [SerializeField] private Vector3 _groundCheckOffset = Vector3.zero;
        [SerializeField] private float _maxSlopeAngle = 45f; // degrees

        [Header("Dash / Boost")]
        [SerializeField] private float _dashDuration = 0.18f;
        [SerializeField] private float _boostDuration = 0.6f;
        [SerializeField] private float _dashCooldown = 0.5f;

        [Header("Dash Charges")]
        [SerializeField] private int _maxCharges = 2;
        [SerializeField] private float _chargeRegenTime = 3f;

        [Header("Effects")]
        [SerializeField] private Material _playerMaterial, _playerHomingAttackMaterial;
        [SerializeField] private ParticleSystem _shockwaveParticle;
        [SerializeField] private GameObject _sprintBoostModel;
        [SerializeField] private AnimationCurve _glitchCurve;
        [SerializeField] private ParticleSystem _footKickParticle, _dashParticle;
        [SerializeField] private SkinnedMeshRenderer[] _playerRenderers;
        private Material[] _originalPlayerMaterials;
        [SerializeField] private GameObject[] _footSprintParticles;
        [SerializeField] private VisualEffect[] _smokeVisualEffects;
        [SerializeField] private TrailRenderer[] _smokeTrailRenderers;
        [Inject] private readonly PostProcessingController _postProcessingController;

        [Header("IK")]
        [SerializeField] private FootIK _footIK;
        [SerializeField] private FullBodyBipedIK _ik;
        [SerializeField] private Transform _pelvisTarget;
        [SerializeField] private float _pelvisOffsetMultiplier = 0.1f;
        [SerializeField] private float _pelvisLerpSpeed = 5f;
        [SerializeField] private float _maxRotationSpeedForIK = 5f; // degrees per second

        [Header("Model Root Adjustments")]
        [SerializeField] private float _moveRollMaxDegrees = -10f;
        [SerializeField] private float _sprintRollMaxDegrees = -15f;
        private Vector3 _basePelvisPosition;
        private Quaternion _originalModelRootLocalRot;
        private Vector3 _originalModelRootLocalPos;

        public Material PlayerMaterial => _playerMaterial;
        public AnimationCurve GlitchCurve => _glitchCurve;

        // Homing attack configuration (used by action states)
        [Header("Homing Attack")]
        [SerializeField] private float _homingRange = 12f;
        [SerializeField] private float _homingConeAngle = 45f;
        [SerializeField] private float _homingSpeed = 28f;
        [SerializeField] private float _homingDuration = 0.6f;
        [SerializeField] private float _homingHitRadius = 1f;
        [SerializeField] private LayerMask _homingMask;
        [SerializeField] private ParticleSystem _homingHitParticle;

        // Input system
        private InputSystem_Actions _actions;
        public InputSystem_Actions Actions => _actions;

        // Input state (polled)
        private Vector2 _moveInput;
        private bool _sprintInput;
        private bool _inhaleInput;
        private bool _crouchInput;
        private bool _jumpRequested;
        private bool _attackRequested;
        private bool _parryRequested;

        // Computed
        private Vector3 _moveDirection = Vector3.zero;

        // State machines
        private StateMachine _locomotionSM;
        private StateMachine _actionSM;

        // Movement override (requested by action states). Simple priority model: exclusive or blend.
        private bool _hasMovementOverride;
        private Vector3 _overrideVelocity;
        private bool _overrideExclusive;
        private float _overrideTimer;

        // Runtime flags
        private bool _isGrounded;
        private bool _wasGrounded;
        private Vector3 _groundNormal = Vector3.up;

        // Dash charges
        private int _currentCharges;
        private float[] _chargeTimers;

        // Star collection
        private int _collectedStars;

        // Smoke effects state
        private bool _smokeEffectsActive;
        private Coroutine _smokeEffectsCoroutine;

        // Animator parameter hashes (dummy names)
        private static readonly int ANIM_SPEED = Animator.StringToHash("Speed");
        private static readonly int ANIM_INPUT_LENGTH = Animator.StringToHash("InputLength");
        private static readonly int ANIM_DISTANCE_TO_GROUND = Animator.StringToHash("DistanceToGround");
        private static readonly int ANIM_IS_GROUNDED = Animator.StringToHash("IsGrounded");
        private static readonly int ANIM_IS_SPRINTING = Animator.StringToHash("IsSprinting");
        private static readonly int ANIM_IS_BOOSTING = Animator.StringToHash("IsBoosting");
        private static readonly int ANIM_IS_DASHING = Animator.StringToHash("IsDashing");
        private static readonly int ANIM_JUMP_TRIGGER = Animator.StringToHash("JumpTrigger");
        private static readonly int ANIM_LAND_TRIGGER = Animator.StringToHash("LandTrigger");

        [Header("Rotation")]
        [SerializeField] private Transform _modelRoot;                 // optional: rotate the visual model instead of root
        [SerializeField] private float _turnSpeed = 0.5f;              // lerp factor for smooth rotation
        [SerializeField] private float _airTurnMultiplier = 0.35f;     // multiplier to turn speed while airborne
        [SerializeField] private bool _debugRotation = false;          // draw debug lines / logs when true

        // Public read-only helpers for states
        public Vector3 MoveDirection => _moveDirection;
        public Rigidbody Rigidbody => _rb;
        public Animator Animator => _animator;
        public bool IsGrounded => _isGrounded;
        public Vector3 GroundNormal => _groundNormal;
        public float WalkSpeed => _walkSpeed;
        public float RunSpeed => _runSpeed;
        public float BoostSpeed => _boostSpeed;
        public float DashSpeed => _dashSpeed;
        public float AccelGround => _accelGround;
        public float AccelAir => _accelAir;
        public float Deceleration => _deceleration;
        public float Damping => _damping;
        public float JumpVelocity => _jumpVelocity;
        public float DashDuration => _dashDuration;
        public float BoostDuration => _boostDuration;
        public float DashCooldown => _dashCooldown;

        // Homing attack accessors (expose serialized tuning to states)
        public float HomingRange => _homingRange;
        public float HomingConeAngle => _homingConeAngle;
        public float HomingSpeed => _homingSpeed;
        public float HomingDuration => _homingDuration;
        public float HomingHitRadius => _homingHitRadius;
        public LayerMask HomingMask => _homingMask;
        public ParticleSystem HomingHitParticle => _homingHitParticle;

        // Input accessor
        public bool SprintRequested => _sprintInput;
        public bool InhaleRequested => _inhaleInput;
        public bool CrouchRequested => _crouchInput;
        public bool ParryRequested => _parryRequested;

        // Debug / read-only state info for on-screen debug overlay
        // These expose internal state machine/current-state info and override timers in a non-invasive, read-only way.
        public string LocomotionStateName => _locomotionSM != null && _locomotionSM.Current != null ? _locomotionSM.Current.Name : "None";
        public float LocomotionStateTime => _locomotionSM != null ? _locomotionSM.TimeInState : 0f;
        public string ActionStateName => _actionSM != null && _actionSM.Current != null ? _actionSM.Current.Name : "None";
        public float ActionStateTime => _actionSM != null ? _actionSM.TimeInState : 0f;

        public Vector3 OverrideVelocity => _overrideVelocity;
        public bool OverrideExclusive => _overrideExclusive;
        public float MovementOverrideTimeRemaining => _overrideTimer;

        public float RotationOverrideTimeRemaining => _rotationOverrideTimer;

        // Dash charges accessors
        public int CurrentCharges => _currentCharges;
        public float[] ChargeTimers => _chargeTimers;

        // Star collection accessors
        public int CollectedStars => _collectedStars;

        // Expose raw move input and animator speed (reads the same parameter the player writes)
        public Vector2 MoveInput => _moveInput;
        public float AnimatorSpeed => _animator != null ? _animator.GetFloat(ANIM_SPEED) : 0f;

        // Rotation override (action states may request temporary rotation targets)
        private bool _hasRotationOverride;
        private Quaternion _rotationOverrideTarget = Quaternion.identity;
        private float _rotationOverrideTimer;
        private bool _rotationOverrideExclusive;

        public float Pressure { get; private set; }
        public bool HasSprintedInAir { get; private set; }
        public bool HasDoubleJumped { get; private set; }

        // IK accessors
        public FullBodyBipedIK IK => _ik;
        public Transform PelvisTarget => _pelvisTarget;
        public float PelvisOffsetMultiplier => _pelvisOffsetMultiplier;
        public float PelvisLerpSpeed => _pelvisLerpSpeed;
        public float MaxRotationSpeedForIK => _maxRotationSpeedForIK;
        public Vector3 BasePelvisPosition => _basePelvisPosition;
        public Transform ModelRoot => _modelRoot;
        public Quaternion OriginalModelRootLocalRot => _originalModelRootLocalRot;
        public Vector3 OriginalModelRootLocalPos => _originalModelRootLocalPos;
        public float MoveRollMaxDegrees => _moveRollMaxDegrees;
public float SprintRollMaxDegrees => _sprintRollMaxDegrees;
        public PostProcessingController PostProcessingController => _postProcessingController;

        /// <summary>
        /// Get the IK weight based on movement and rotation speed.
        /// </summary>
        public float GetIKWeight() => MoveDirection.sqrMagnitude > 0.01f ? Mathf.Clamp01(RotationSpeed / MaxRotationSpeedForIK) : 0f;
        public float RotationSpeed { get; private set; } // degrees per second
        public float TurnDirection { get; private set; } // -1 left, 1 right, 0 none
        private Quaternion _previousRotation;

        [Inject] private readonly AudioManager _audioManager;
        [Inject] private readonly HomingIndicatorManager _indicatorManager;
        [Inject] private readonly ICommandPublisher _commandPublisher;
        [Inject] private readonly HomingExitAnimationConfig _homingExitConfig;

        public ICommandPublisher CommandPublisher => _commandPublisher;

        public HomingExitAnimationConfig HomingExitConfig => _homingExitConfig;
        public int LastHomingExitIndex { get; set; } = -1;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioSource _sprintAudioSource;

        // Foot kick particle accessor
        public ParticleSystem FootKickParticle => _footKickParticle;
        public Transform LeftFootTransform => _animator != null ? _animator.GetBoneTransform(HumanBodyBones.LeftFoot) : null;

        /// <summary>
        /// Request a temporary rotation override. If exclusive=true locomotion rotation is suspended.
        /// </summary>
        public void RequestRotationOverride(Quaternion target, float duration, bool exclusive = true)
        {
            _hasRotationOverride = true;
            _rotationOverrideTarget = target;
            _rotationOverrideTimer = duration;
            _rotationOverrideExclusive = exclusive;
        }

        public void ClearRotationOverride()
        {
            _hasRotationOverride = false;
            _rotationOverrideTimer = 0f;
        }

        public bool HasRotationOverride() => _hasRotationOverride;
        public Quaternion GetRotationOverride() => _rotationOverrideTarget;
        public bool IsRotationOverrideExclusive() => _rotationOverrideExclusive;

        /// <summary>
        /// Rotate player (or modelRoot if assigned) toward the given direction.
        /// Uses Rigidbody.MoveRotation for physics-friendly rotation.
        /// Simple smooth rotation using Lerp.
        /// Falls back to horizontal velocity if move input is tiny.
        /// </summary>
        public void RotateTowardsDirection(Vector3 direction, float deltaTime, bool isAir = false, bool instantaneous = false, bool allow3DRotation = false)
        {
            // If rotation override is exclusive, apply it immediately
            if (_hasRotationOverride && _rotationOverrideExclusive)
            {
                ApplyRotation(_rotationOverrideTarget, instantaneous);
                if (_debugRotation) Debug.DrawLine(transform.position, transform.position + _rotationOverrideTarget * Vector3.forward, Color.magenta, 0.1f);
                return;
            }

            // Primary direction is the provided one; if it's too small, fallback to current horizontal velocity
            Vector3 dir = direction;
            if (dir.sqrMagnitude < 0.0001f)
            {
                Vector3 hv = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
                if (hv.sqrMagnitude < 0.0001f) return; // nothing meaningful to rotate to
                dir = hv.normalized;
            }
            else
            {
                dir.Normalize();
            }

            Quaternion currentRotation = GetCurrentRotation();
            Quaternion targetRotation;

            if (allow3DRotation)
            {
                // Full 3D rotation
                targetRotation = Quaternion.LookRotation(dir, Vector3.up);
            }
            else
            {
                // 2D horizontal rotation using LerpAngle for smooth yaw interpolation
                float currentYaw = currentRotation.eulerAngles.y;
                float targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                float lerpFactor = _turnSpeed * (isAir ? _airTurnMultiplier : 1f);
                float newYaw = instantaneous ? targetYaw : Mathf.LerpAngle(currentYaw, targetYaw, lerpFactor);
                Quaternion newRot = Quaternion.Euler(0f, newYaw, 0f);
                ApplyRotation(newRot, true);
                return;
            }

            // 3D rotation
            // Smooth lerp rotation
            float lerpFactor3D = _turnSpeed * (isAir ? _airTurnMultiplier : 1f);
            Quaternion newRot3D = instantaneous ? targetRotation : Quaternion.Lerp(currentRotation, targetRotation, lerpFactor3D);

            ApplyRotation(newRot3D, true);

            if (_debugRotation)
            {
                Vector3 origin = transform.position + Vector3.up * 1.2f;
                Debug.DrawLine(origin, origin + currentRotation * Vector3.forward * 2f, Color.green, 0.1f);
                Debug.DrawLine(origin, origin + targetRotation * Vector3.forward * 2f, Color.red, 0.1f);
            }
        }

        public Quaternion GetCurrentRotation()
        {
            return transform.rotation;
        }

        private void ApplyRotation(Quaternion rot, bool useRigidbody)
        {
            _rb.MoveRotation(rot);
        }

        // Input consumption API for states
        public bool ConsumeJumpRequest()
        {
            if (!_jumpRequested) return false;
            _jumpRequested = false;
            return true;
        }

        public bool ConsumeAttackRequest()
        {
            if (!_attackRequested) return false;
            _attackRequested = false;
            return true;
        }
        
        public bool ConsumeParryRequest()
        {
            if (!_parryRequested) return false;
            _parryRequested = false;
            return true;
        }

        // Movement override API (action states call this)
        public void RequestMovementOverride(Vector3 velocity, float duration, bool exclusive = true)
        {
            _hasMovementOverride = true;
            _overrideVelocity = velocity;
            _overrideExclusive = exclusive;
            _overrideTimer = duration;
        }

        public void ClearMovementOverride()
        {
            _hasMovementOverride = false;
            _overrideTimer = 0f;
        }

        public bool HasMovementOverride() => _hasMovementOverride;
        public Vector3 GetOverrideVelocity() => _overrideVelocity;
        public bool IsOverrideExclusive() => _overrideExclusive;

        // Locomotion permission checking system
        /// <summary>
        /// Check if the specified locomotion type is currently allowed based on active action state.
        /// </summary>
        public bool IsLocomotionAllowed(LocomotionType locomotionType)
        {
            // If current action state blocks this locomotion type, deny permission
            if (_actionSM?.Current is IBlocksLocomotion blockingState)
            {
                if (blockingState.IsBlockingLocomotion &&
                    (blockingState.BlocksLocomotion & locomotionType) != 0)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Check if any locomotion is currently blocked by the active action state.
        /// </summary>
        public bool IsAnyLocomotionBlocked()
        {
            return _actionSM?.Current is IBlocksLocomotion blockingState &&
                   blockingState.IsBlockingLocomotion;
        }
        
        /// <summary>
        /// Get which locomotion types are currently blocked (returns LocomotionType.None if none blocked).
        /// </summary>
        public LocomotionType GetBlockedLocomotionTypes()
        {
            if (_actionSM?.Current is IBlocksLocomotion blockingState)
            {
                return blockingState.IsBlockingLocomotion ? blockingState.BlocksLocomotion : LocomotionType.None;
            }
            return LocomotionType.None;
        }

        // Simple physics helpers states will use
        public void SetVelocityImmediate(Vector3 v)
        {
            _rb.linearVelocity = v;
        }

        public void AddGroundForce(Vector3 force)
        {
            _rb.AddForce(force, ForceMode.Acceleration);
        }

        public void ClampHorizontalSpeed(float maxSpeed)
        {
            Vector3 horizontal = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            if (horizontal.magnitude > maxSpeed)
            {
                Vector3 clamped = horizontal.normalized * maxSpeed;
                _rb.linearVelocity = new Vector3(clamped.x, _rb.linearVelocity.y, clamped.z);
            }
        }

        // Lifecycle
        private void Awake()
        {
            _actions = new InputSystem_Actions();

            // Ensure Rigidbody uses interpolation for smooth visuals (prevents camera stutter when physics drives the character)
            if (_rb != null && _rb.interpolation == RigidbodyInterpolation.None)
                _rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Warn if Animator root motion is enabled — root motion can override rotation applied in code.
            if (_animator != null && _animator.applyRootMotion)
                Debug.LogWarning("Animator.applyRootMotion is enabled on Player. This may conflict with code-driven rotation. Consider disabling it or assigning a separate _modelRoot.", this);

            // create state machines
            _locomotionSM = new StateMachine();
            _actionSM = new StateMachine();

            // Initialize dash charges
            _currentCharges = _maxCharges;
            _chargeTimers = new float[_maxCharges];

            // Initialize star collection
            _collectedStars = 0;

            // Store base pelvis position for IK
            if (_pelvisTarget != null)
            {
                _basePelvisPosition = _pelvisTarget.localPosition;
            }
            _previousRotation = GetCurrentRotation();

            // Store original model root transform
            if (_modelRoot != null)
            {
                _originalModelRootLocalRot = _modelRoot.localRotation;
                _originalModelRootLocalPos = _modelRoot.localPosition;
            }
        }

        private void OnEnable()
        {
            // TODO: This will be moved to game manager or something. And InputManager will be created.
            _audioManager.Initialize();
            // Cursor.lockState = CursorLockMode.Locked;
            // Cursor.visible = false;

            _actions.Player.Enable();
            _actions.Player.Jump.performed += ctx => _jumpRequested = true;
            _actions.Player.Attack.performed += ctx => _attackRequested = true;
            _actions.Player.Sprint.performed += ctx => _sprintInput = true;
            // Note: Using same Attack input for parry - will be handled in action state logic
            _actions.Player.Sprint.canceled += ctx => _sprintInput = false;
            _actions.Player.Inhale.performed += ctx => _inhaleInput = true;
            _actions.Player.Inhale.canceled += ctx => _inhaleInput = false;
            _actions.Player.Crouch.performed += ctx => _crouchInput = true;
            _actions.Player.Crouch.canceled += ctx => _crouchInput = false;

            // initialize states (create initial state instances)
            var idle = new IdleState(this, _locomotionSM);
            _locomotionSM.Initialize(idle);

            var actionNone = new ActionNoneState(this, _actionSM);
            _actionSM.Initialize(actionNone);
        }

        private void OnDisable()
        {
            _actions.Player.Jump.performed -= ctx => _jumpRequested = true;
            _actions.Player.Attack.performed -= ctx => _attackRequested = true;
            _actions.Player.Sprint.performed -= ctx => _sprintInput = true;
            // Note: Using same Attack input for parry - will be handled in action state logic
            _actions.Player.Sprint.canceled -= ctx => _sprintInput = false;
            _actions.Player.Inhale.performed -= ctx => _inhaleInput = true;
            _actions.Player.Inhale.canceled -= ctx => _inhaleInput = false;
            _actions.Player.Crouch.performed -= ctx => _crouchInput = true;
            _actions.Player.Crouch.canceled -= ctx => _crouchInput = false;

            _actions.Player.Disable();
            _actions.Dispose();
        }

        private void Update()
        {
            // read continuous inputs
            _moveInput = _actions.Player.Move.ReadValue<Vector2>();
            if (_moveInput.sqrMagnitude < 0.0001f) _moveInput = Vector2.zero;
            _moveDirection = ComputeCameraRelativeMove(_moveInput);

            // update override timer
            if (_hasMovementOverride)
            {
                _overrideTimer -= Time.deltaTime;
                if (_overrideTimer <= 0f) ClearMovementOverride();
            }

            // rotation override timer
            if (_hasRotationOverride)
            {
                _rotationOverrideTimer -= Time.deltaTime;
                if (_rotationOverrideTimer <= 0f) ClearRotationOverride();
            }

            // Update dash charge regeneration
            UpdateDashCharges();

            // Check for rail attachment if not already grinding
            if (_railDetector != null && _locomotionSM.Current != null &&
                !(_locomotionSM.Current is GrindState))
            {
                _railDetector.CheckAutoAttach();
                if (_railDetector.IsAttached)
                {
                    // Transition to grind state
                    _locomotionSM.ChangeState(new GrindState(this, _locomotionSM));
                }
            }

            // Logic updates: action first (may request overrides) then locomotion
            _actionSM.LogicUpdate(Time.deltaTime);
            _locomotionSM.LogicUpdate(Time.deltaTime);

            // Update rotation status
            Quaternion currentRot = GetCurrentRotation();
            float deltaYaw = Mathf.DeltaAngle(_previousRotation.eulerAngles.y, currentRot.eulerAngles.y);
            RotationSpeed = Mathf.Abs(deltaYaw) / Time.deltaTime;
            TurnDirection = Mathf.Sign(deltaYaw);
            _previousRotation = currentRot;

            // Update homing indicators
            UpdateHomingIndicators();

            // animator generic properties
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            // ground check
            bool sphereGrounded = Physics.CheckSphere(_groundCheck.position + _groundCheckOffset, _groundCheckRadius, _groundMask, QueryTriggerInteraction.Ignore);

            // ground normal detection
            Vector3 normal = Vector3.up;
            if (sphereGrounded)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, Vector3.down, out hit, 2f, _groundMask))
                {
                    normal = hit.normal;
                }
            }

            // check if slope is walkable
            float slopeAngle = Vector3.Angle(normal, Vector3.up);
            _isGrounded = sphereGrounded && slopeAngle <= _maxSlopeAngle;
            _groundNormal = normal;

            // Reset air flags when landing
            if (_isGrounded && !_wasGrounded)
            {
                HasSprintedInAir = false;
                HasDoubleJumped = false;
            }
            _wasGrounded = _isGrounded;

            // Physics update: action first (so overrides are applied), then locomotion
            _actionSM.PhysicsUpdate();
            _locomotionSM.PhysicsUpdate();
        }

        private Vector3 ComputeCameraRelativeMove(Vector2 input)
        {
            if (input == Vector2.zero) return Vector3.zero;
            var cam = Camera.main;
            Vector3 forward;
            Vector3 right;
            if (cam != null)
            {
                forward = cam.transform.forward;
                forward.y = 0f;
                forward.Normalize();
                right = cam.transform.right;
                right.y = 0f;
                right.Normalize();
            }
            else
            {
                forward = Vector3.forward;
                right = Vector3.right;
            }

            Vector3 dir = forward * input.y + right * input.x;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            return dir;
        }

        private void UpdateAnimator()
        {
            Vector3 horizontalVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _animator.SetFloat(ANIM_SPEED, horizontalVel.magnitude);
            _animator.SetFloat(ANIM_INPUT_LENGTH, _moveInput.magnitude);

            // Distance to ground
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 10f, _groundMask, QueryTriggerInteraction.Ignore))
            {
                _animator.SetFloat(ANIM_DISTANCE_TO_GROUND, hit.distance);
            }
            else
            {
                _animator.SetFloat(ANIM_DISTANCE_TO_GROUND, 10f); // max distance
            }

            _animator.SetBool(ANIM_IS_GROUNDED, _isGrounded);
            _animator.SetBool(ANIM_IS_SPRINTING, _sprintInput && _moveDirection.sqrMagnitude > 0.01f);
        }

        private void UpdateHomingIndicators()
        {
            if (_indicatorManager == null) return;

            if (!_isGrounded)
            {
                var target = FindBestHomingTarget(_homingRange, _homingConeAngle, _homingMask);
                if (target != null)
                {
                    _indicatorManager.ShowIndicators(new List<IHomingTarget> { target });
                }
                else
                {
                    _indicatorManager.HideAllIndicators();
                }
            }
            else
            {
                _indicatorManager.HideAllIndicators();
            }
        }

        /// <summary>
        /// Find the best IHomingTarget within range and cone in front of the player.
        /// Returns null if none found.
        /// </summary>
        public IHomingTarget FindBestHomingTarget(float range, float coneAngleDeg, LayerMask mask)
        {
            // Use OverlapSphere to find candidate colliders
            Collider[] cols = Physics.OverlapSphere(transform.position, range, mask, QueryTriggerInteraction.Collide);
            if (cols == null || cols.Length == 0) return null;

            Vector3 forward = GetCurrentRotation() * Vector3.forward;
            float bestSqr = float.MaxValue;
            IHomingTarget bestTarget = null;

            foreach (var c in cols)
            {
                if (c == null) continue;

                // try to get IHomingTarget from the collider's GameObject or parents
                var target = c.GetComponent<IHomingTarget>();
                if (target == null)
                {
                    target = c.GetComponentInParent<IHomingTarget>();
                    if (target == null) continue;
                }

                if (!target.IsActive) continue;

                Vector3 toTarget = target.TargetTransform.position - transform.position;
                float sqrDist = toTarget.sqrMagnitude;
                if (sqrDist > range * range) continue;

                // angle check: is the target within the forward cone
                float angle = Vector3.Angle(forward, toTarget);
                if (angle > coneAngleDeg * 0.5f) continue;

                if (sqrDist < bestSqr)
                {
                    bestSqr = sqrDist;
                    bestTarget = target;
                }
            }

            return bestTarget;
        }

        // Expose small debug method
        public void ForceEnterLocomotionState(IState state)
        {
            _locomotionSM.ForceSetState(state);
        }

        /// <summary>
        /// Force the player into path following state along the specified rail.
        /// </summary>
        public void EnterPathFollowState(JackRussell.Rails.SplineRail path)
        {
            if (path != null)
            {
                var pathState = new PathFollowState(this, _locomotionSM, path);
                _locomotionSM.ChangeState(pathState);
            }
        }

        /// <summary>
        /// Force the player into path following state along the specified rail with easing.
        /// </summary>
        public void EnterPathFollowState(JackRussell.Rails.SplineRail path, AnimationCurve speedCurve, float duration)
        {
            if (path != null)
            {
                var pathState = new PathFollowState(this, _locomotionSM, path, speedCurve, duration);
                _locomotionSM.ChangeState(pathState);
            }
        }

        public void OnSprintEnter()
        {
            _shockwaveParticle.Play();
            _sprintBoostModel.SetActive(true);
            foreach (var footParticle in _footSprintParticles)
            {
                footParticle.gameObject.SetActive(true);
            }
        }

        public void OnSprintExit()
        {
            _sprintBoostModel.SetActive(false);
            foreach (var footParticle in _footSprintParticles)
            {
                footParticle.gameObject.SetActive(false);
            }
        }

        public void OnJumpEnter()
        {
            _audioManager.PlaySound(SoundType.Jump, _audioSource);
        }

        public void OnGrindEnter()
        {
            Animator.Play("5080_0_narancia_01_skill_03_lp");
            Animator.SetBool("IsGrinding", true);
            PlaySound(SoundType.RailStart);
            StartLoopedSound(SoundType.RailLoop);
        }

        public void OnGrindExit()
        {
            Animator.SetBool("IsGrinding", false);
            StopLoopedSound(SoundType.RailLoop);
        }

        public void SetPressure(float pressure)
        {
            if (pressure >= 100) pressure = 100;
            if (pressure < 0) pressure = 0;
            Pressure = pressure;
            _commandPublisher.PublishAsync(new PressureUpdateCommand(Pressure));
        }

        public void MarkSprintInAir()
        {
            HasSprintedInAir = true;
        }

        public void ResetSprintInAir()
        {
            HasSprintedInAir = false;
        }

        public void MarkDoubleJumped()
        {
            HasDoubleJumped = true;
        }

        public void PlaySound(SoundType soundType)
        {
            _audioManager.PlaySound(soundType, _audioSource);
        }

        public void StartLoopedSound(SoundType soundType, float fadeInDuration = 0.5f)
        {
            _audioManager.StartLoopedSound(soundType, fadeInDuration);
        }

        public void StopLoopedSound(SoundType soundType, float fadeOutDuration = 0.5f)
        {
            _audioManager.StopLoopedSound(soundType, fadeOutDuration);
        }

        public void PlaySprintSpeedUpSound()
        {
            if (_sprintAudioSource != null)
            {
                _sprintAudioSource.volume = 1f;
            }
            _audioManager.PlaySound(SoundType.SprintSpeedUp, _sprintAudioSource);
        }

        public void StopSprintSpeedUp(float fadeDuration = 0.5f)
        {
            if (_sprintAudioSource != null && _sprintAudioSource.isPlaying)
            {
                _sprintAudioSource.DOKill();
                _sprintAudioSource.DOFade(0f, fadeDuration).OnComplete(() =>
                {
                    if (_sprintAudioSource != null) _sprintAudioSource.Stop();
                });
            }
        }

        public void HideHomingIndicators()
        {
            _indicatorManager?.HideAllIndicators();
        }

        /// <summary>
        /// Enables smoke effects (VisualEffects and TrailRenderers) immediately.
        /// </summary>
        public void EnableSmokeEffects()
        {
            if (_smokeEffectsActive) return;

            _smokeEffectsActive = true;

            // Stop any existing coroutine
            if (_smokeEffectsCoroutine != null)
            {
                StopCoroutine(_smokeEffectsCoroutine);
                _smokeEffectsCoroutine = null;
            }

            // Enable VisualEffects
            if (_smokeVisualEffects != null)
            {
                foreach (var vfx in _smokeVisualEffects)
                {
                    if (vfx != null)
                    {
                        vfx.enabled = true;
                        vfx.Play();
                    }
                }
            }

            // Enable TrailRenderers
            if (_smokeTrailRenderers != null)
            {
                foreach (var trail in _smokeTrailRenderers)
                {
                    if (trail != null)
                    {
                        trail.enabled = true;
                        trail.emitting = true;
                    }
                }
            }
        }

        /// <summary>
        /// Disables smoke effects after a delay, allowing existing particles to fade naturally.
        /// </summary>
        public void DisableSmokeEffects(float delay = 0.5f)
        {
            if (!_smokeEffectsActive) return;

            _smokeEffectsActive = false;

            // Stop any existing coroutine
            if (_smokeEffectsCoroutine != null)
            {
                StopCoroutine(_smokeEffectsCoroutine);
            }

            // Start coroutine to disable after delay
            _smokeEffectsCoroutine = StartCoroutine(DisableSmokeEffectsAfterDelay(delay));
        }

        private IEnumerator DisableSmokeEffectsAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            // Disable VisualEffects (stop emitting but let existing particles finish)
            if (_smokeVisualEffects != null)
            {
                foreach (var vfx in _smokeVisualEffects)
                {
                    if (vfx != null)
                    {
                        vfx.Stop();
                    }
                }
            }

            // Disable TrailRenderers (stop emitting but keep existing trails)
            if (_smokeTrailRenderers != null)
            {
                foreach (var trail in _smokeTrailRenderers)
                {
                    if (trail != null)
                    {
                        trail.emitting = false;
                    }
                }
            }

            _smokeEffectsCoroutine = null;
        }

        public void OnHomingAttackEnter()
        {
            Animator.ResetTrigger("HomingAttackReach");
            Animator.Play("HomingAttack");

            // Store original materials before changing
            _originalPlayerMaterials = new Material[_playerRenderers.Length];
            for (int i = 0; i < _playerRenderers.Length; i++)
            {
                _originalPlayerMaterials[i] = _playerRenderers[i].material;
                _playerRenderers[i].material = _playerHomingAttackMaterial;
            }


            // Implement player material effects
            // if (_playerMaterial != null)
            // {
            //     DemoUtils.SetMaterialTransparent(_playerMaterial);

            //     _playerMaterial.DOKill();

            //     // Enable keywords and set properties
            //     _playerMaterial.SetFloat("_FadeOn", 1f);
            //     _playerMaterial.EnableKeyword("_FADE_ON");

            //     // _playerMaterial.SetFloat("_IntersectionFadeOn", 1f);
            //     // _playerMaterial.EnableKeyword("_INTERSECTION_FADE_ON");

            //     _playerMaterial.SetFloat("_VertexDistortionOn", 1f);
            //     _playerMaterial.EnableKeyword("_VERTEX_DISTORTION_ON");

            //     _playerMaterial.SetFloat("_ScrollTextureOn", 1f);
            //     _playerMaterial.EnableKeyword("_SCROLL_TEXTURE_ON");

            //     // Animate float properties to target values
            //     _playerMaterial.DOFloat(0.25f, "_FadeAmount", 0.1f);
            //     // _playerMaterial.DOFloat(0.7f, "_IntersectionFadeFactor", 0.1f);
            //     _playerMaterial.DOFloat(0.2f, "_VertexDistortionAmount", 0.1f);
            //     _playerMaterial.DOFloat(20f, "_ScrollTextureY", 0.1f);
            // }
        }

        public void OnHomingAttackReach()
        {
            Animator.SetTrigger("HomingAttackReach");

            // Restore original materials
            if (_originalPlayerMaterials != null && _originalPlayerMaterials.Length == _playerRenderers.Length)
            {
                for (int i = 0; i < _playerRenderers.Length; i++)
                {
                    _playerRenderers[i].material = _originalPlayerMaterials[i];
                }
            }

            // Implement player material effects revert
            // if (_playerMaterial != null)
            // {
            //     _playerMaterial.DOKill();

            //     // Animate float properties to 0, then disable keywords
            //     Sequence seq = DOTween.Sequence();
            //     seq.Append(_playerMaterial.DOFloat(0f, "_FadeAmount", 0.8f));
            //     // seq.Join(_playerMaterial.DOFloat(0.1f, "_IntersectionFadeFactor", 0.3f));
            //     seq.Join(_playerMaterial.DOFloat(0f, "_VertexDistortionAmount", 0.8f));
            //     seq.Join(_playerMaterial.DOFloat(0f, "_ScrollTextureY", 0.8f));
            //     seq.OnComplete(() =>
            //     {
            //         DemoUtils.SetMaterialOpaque(_playerMaterial);

            //         _playerMaterial.SetFloat("_FadeOn", 0f);
            //         _playerMaterial.DisableKeyword("_FADE_ON");

            //         // _playerMaterial.SetFloat("_IntersectionFadeOn", 0f);
            //         // _playerMaterial.DisableKeyword("_INTERSECTION_FADE_ON");

            //         _playerMaterial.SetFloat("_VertexDistortionOn", 0f);
            //         _playerMaterial.DisableKeyword("_VERTEX_DISTORTION_ON");

            //         _playerMaterial.SetFloat("_ScrollTextureOn", 0f);
            //         _playerMaterial.DisableKeyword("_SCROLL_TEXTURE_ON");
            //     });
            // }
        }

        // Instant versions for editor testing
        public void SetHomingAttackPlayerMaterial()
        {
            if (_playerMaterial != null)
            {
                _playerMaterial.DOKill();
                _playerMaterial.SetFloat("_FadeOn", 1f);
                _playerMaterial.EnableKeyword("_FADE_ON");

                // _playerMaterial.SetFloat("_IntersectionFadeOn", 1f);
                // _playerMaterial.EnableKeyword("_INTERSECTION_FADE_ON");

                _playerMaterial.SetFloat("_VertexDistortionOn", 1f);
                _playerMaterial.EnableKeyword("_VERTEX_DISTORTION_ON");

                _playerMaterial.SetFloat("_ScrollTextureOn", 1f);
                _playerMaterial.EnableKeyword("_SCROLL_TEXTURE_ON");

                _playerMaterial.SetFloat("_FadeAmount", 0.25f);
                // _playerMaterial.SetFloat("_IntersectionFadeFactor", 0.7f);
                _playerMaterial.SetFloat("_VertexDistortionAmount", 0.1f);
                _playerMaterial.SetFloat("_ScrollTextureY", 20f);

                DemoUtils.SetMaterialTransparent(_playerMaterial);
            }
        }

        public void ResetPlayerMaterial()
        {
            if (_playerMaterial != null)
            {
                _playerMaterial.DOKill();
                _playerMaterial.SetFloat("_FadeOn", 0f);
                _playerMaterial.DisableKeyword("_FADE_ON");

                // _playerMaterial.SetFloat("_IntersectionFadeOn", 0f);
                // _playerMaterial.DisableKeyword("_INTERSECTION_FADE_ON");

                _playerMaterial.SetFloat("_VertexDistortionOn", 0f);
                _playerMaterial.DisableKeyword("_VERTEX_DISTORTION_ON");

                _playerMaterial.SetFloat("_ScrollTextureOn", 0f);
                _playerMaterial.DisableKeyword("_SCROLL_TEXTURE_ON");

                _playerMaterial.SetFloat("_FadeAmount", 0f);
                // _playerMaterial.SetFloat("_IntersectionFadeFactor", 0.1f);
                _playerMaterial.SetFloat("_VertexDistortionAmount", 0f);
                _playerMaterial.SetFloat("_ScrollTextureY", 0f);

                DemoUtils.SetMaterialOpaque(_playerMaterial);
            }
        }

        /// <summary>
        /// Apply all turn adjustments: IK pelvis offset and ModelRoot roll.
        /// </summary>
        public void ApplyTurnAdjustments(float weight, float rollMaxDegrees, float pelvisMultiplier = 1f)
        {
            // IK: Offset pelvis target
            if (IK != null && PelvisTarget != null)
            {
                Vector3 targetPos;
                if (weight > 0)
                {
                    Vector3 offset = new Vector3(TurnDirection * PelvisOffsetMultiplier * weight * pelvisMultiplier, 0, 0);
                    targetPos = BasePelvisPosition + offset;
                }
                else
                {
                    targetPos = BasePelvisPosition;
                }
                PelvisTarget.localPosition = Vector3.Lerp(PelvisTarget.localPosition, targetPos, Time.fixedDeltaTime * PelvisLerpSpeed);
                IK.solver.bodyEffector.positionWeight = Mathf.Lerp(IK.solver.bodyEffector.positionWeight, weight, Time.fixedDeltaTime * 10f);
            }

            // ModelRoot: Roll based on turn, scaled by player speed
            if (_modelRoot != null)
            {
                float speedFactor = Mathf.Clamp01(_rb.linearVelocity.magnitude / WalkSpeed); // 0 to 1 based on walk speed
                if (weight > 0)
                {
                    Quaternion targetRot = _originalModelRootLocalRot * Quaternion.Euler(0, 0, TurnDirection * rollMaxDegrees * weight * speedFactor);
                    _modelRoot.localRotation = Quaternion.Lerp(_modelRoot.localRotation, targetRot, Time.fixedDeltaTime * 5f);
                }
                else
                {
                    _modelRoot.localRotation = Quaternion.Lerp(_modelRoot.localRotation, _originalModelRootLocalRot, Time.fixedDeltaTime * 5f);
                }
            }
        }

        // Dash charge methods
        public bool CanDash() => _currentCharges > 0;

        public void ConsumeCharge()
        {
            if (_currentCharges > 0)
            {
                _currentCharges--;
                _commandPublisher.PublishAsync(new DashChargesUpdateCommand(_currentCharges));
            }
        }

        public Vector3 GetDashDirection()
        {
            Vector3 dashDir = MoveDirection;
            if (dashDir.sqrMagnitude < 0.001f)
            {
                // Fallback to camera-relative forward
                var cam = Camera.main;
                if (cam != null)
                {
                    dashDir = cam.transform.forward;
                    dashDir.y = 0f;
                    dashDir.Normalize();
                }
                else
                {
                    dashDir = Vector3.forward;
                }
            }
            return dashDir;
        }

        private void UpdateDashCharges()
        {
            bool chargesChanged = false;
            for (int i = _maxCharges - _currentCharges - 1; i >= 0; i--)
            {
                if (i < _chargeTimers.Length)
                {
                    _chargeTimers[i] += Time.deltaTime;
                    if (_chargeTimers[i] >= _chargeRegenTime)
                    {
                        _currentCharges++;
                        _chargeTimers[i] = 0f;
                        chargesChanged = true;
                    }
                }
            }
            if (chargesChanged)
            {
                _commandPublisher.PublishAsync(new DashChargesUpdateCommand(_currentCharges));
            }
        }

        // Star collection methods
        public void CollectStar()
        {
            _collectedStars++;
            _commandPublisher.PublishAsync(new StarCollectedUpdateCommand(_collectedStars));
            PlaySound(SoundType.StarCollect);
        }

        public void DisableFootIK()
        {
            _footIK.enabled = false;
        }
        
        public void EnableFootIK()
        {
            _footIK.enabled = true;
        }

        public void OnDashEnter()
        {
            DisableFootIK();
            _dashParticle.Play();
        }

        public void ResetLocomotionState()
        {
            _locomotionSM.ChangeState(new IdleState(this, _locomotionSM));
            SetVelocityImmediate(Vector3.zero);
        }
    }
}
