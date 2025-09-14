using UnityEngine;
using UnityEngine.InputSystem;
using System;
using JackRussell.States;
using JackRussell.States.Locomotion;
using JackRussell.States.Action;

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

        [Header("Speeds")]
        [SerializeField] private float _walkSpeed = 6f;
        [SerializeField] private float _runSpeed = 12f;
        [SerializeField] private float _boostSpeed = 28f;
        [SerializeField] private float _dashSpeed = 30f;

        [Header("Acceleration")]
        [SerializeField] private float _accelGround = 60f;
        [SerializeField] private float _accelAir = 20f;
        [SerializeField] private float _deceleration = 80f;

        [Header("Jump & Gravity")]
        [SerializeField] private float _jumpVelocity = 10f;
        [SerializeField] private float _gravityMultiplier = 1f;

        [Header("Ground Check")]
        [SerializeField] private float _groundCheckRadius = 0.25f;
        [SerializeField] private Vector3 _groundCheckOffset = Vector3.zero;

        [Header("Dash / Boost")]
        [SerializeField] private float _dashDuration = 0.18f;
        [SerializeField] private float _boostDuration = 0.6f;
        [SerializeField] private float _dashCooldown = 0.5f;

[Header("Effects")]
        [SerializeField] private ParticleSystem _shockwaveParticle;

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

        // Input state (polled)
        private Vector2 _moveInput;
        private bool _sprintInput;
        private bool _inhaleInput;
        private bool _jumpRequested;
        private bool _attackRequested;

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

        // Animator parameter hashes (dummy names)
        private static readonly int ANIM_SPEED = Animator.StringToHash("Speed");
        private static readonly int ANIM_IS_GROUNDED = Animator.StringToHash("IsGrounded");
        private static readonly int ANIM_IS_SPRINTING = Animator.StringToHash("IsSprinting");
        private static readonly int ANIM_IS_BOOSTING = Animator.StringToHash("IsBoosting");
        private static readonly int ANIM_IS_DASHING = Animator.StringToHash("IsDashing");
        private static readonly int ANIM_JUMP_TRIGGER = Animator.StringToHash("JumpTrigger");
        private static readonly int ANIM_LAND_TRIGGER = Animator.StringToHash("LandTrigger");

        [Header("Rotation")]
        [SerializeField] private Transform _modelRoot;                 // optional: rotate the visual model instead of root
        [SerializeField] private float _turnSpeed = 720f;              // degrees per second (tune for feel)
        [SerializeField] private float _airTurnMultiplier = 0.35f;     // multiplier to turn speed while airborne
        [SerializeField] private float _rotationSmoothing = 0.12f;     // (kept for potential blending; primary uses MoveTowardsAngle)
        [SerializeField] private bool _debugRotation = false;          // draw debug lines / logs when true
        [SerializeField] private float _snapAngleThreshold = 160f;     // if angle diff > this and instantaneous requested, snap

        // Public read-only helpers for states
        public Vector3 MoveDirection => _moveDirection;
        public Rigidbody Rigidbody => _rb;
        public Animator Animator => _animator;
        public bool IsGrounded => _isGrounded;
        public float WalkSpeed => _walkSpeed;
        public float RunSpeed => _runSpeed;
        public float BoostSpeed => _boostSpeed;
        public float DashSpeed => _dashSpeed;
        public float AccelGround => _accelGround;
        public float AccelAir => _accelAir;
        public float Deceleration => _deceleration;
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

        // Expose raw move input and animator speed (reads the same parameter the player writes)
        public Vector2 MoveInput => _moveInput;
        public float AnimatorSpeed => _animator != null ? _animator.GetFloat(ANIM_SPEED) : 0f;

        // Rotation override (action states may request temporary rotation targets)
        private bool _hasRotationOverride;
        private Quaternion _rotationOverrideTarget = Quaternion.identity;
        private float _rotationOverrideTimer;
        private bool _rotationOverrideExclusive;

        public float Pressure { get; private set; }

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
        /// Implemented with explicit yaw MoveTowardsAngle for predictable, tunable turning.
        /// Falls back to horizontal velocity if move input is tiny.
        /// </summary>
        public void RotateTowardsDirection(Vector3 direction, float deltaTime, bool isAir = false, bool instantaneous = false)
        {
            // If rotation override is exclusive, apply it immediately
            if (_hasRotationOverride && _rotationOverrideExclusive)
            {
                ApplyRotation(_rotationOverrideTarget, instantaneous);
                if (_debugRotation) Debug.DrawLine(transform.position, transform.position + _rotationOverrideTarget * Vector3.forward, Color.magenta, 0.1f);
                return;
            }

            // Primary direction is the provided one; if it's too small, fallback to current horizontal velocity
            Vector3 dir = new Vector3(direction.x, 0f, direction.z);
            if (dir.sqrMagnitude < 0.0001f)
            {
                Vector3 hv = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
                if (hv.sqrMagnitude < 0.0001f) return; // nothing meaningful to rotate to
                dir = hv.normalized;
            }
            else
            {
                dir.Normalize();
            }

            // compute target yaw (degrees)
            float targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

            // current yaw from the chosen rotation target
            float currentYaw = GetCurrentRotation().eulerAngles.y;

            // compute max angular change this frame
            float maxDelta = _turnSpeed * (isAir ? _airTurnMultiplier : 1f) * deltaTime;

            // if instantaneous requested and angle is large, snap; otherwise move toward angle
            float angleDiff = Mathf.DeltaAngle(currentYaw, targetYaw);
            float newYaw;
            if (instantaneous && Mathf.Abs(angleDiff) >= _snapAngleThreshold)
            {
                newYaw = targetYaw;
            }
            else
            {
                newYaw = instantaneous ? targetYaw : Mathf.MoveTowardsAngle(currentYaw, targetYaw, maxDelta);
            }

            Quaternion newRot = Quaternion.Euler(0f, newYaw, 0f);
            ApplyRotation(newRot, true);

            if (_debugRotation)
            {
                // draw debug lines for target direction vs forward
                Vector3 origin = transform.position + Vector3.up * 1.2f;
                Vector3 forward = Quaternion.Euler(0f, currentYaw, 0f) * Vector3.forward;
                Vector3 targetFwd = Quaternion.Euler(0f, targetYaw, 0f) * Vector3.forward;
                Debug.DrawLine(origin, origin + forward * 2f, Color.green, 0.1f);
                Debug.DrawLine(origin, origin + targetFwd * 2f, Color.red, 0.1f);
                //Debug.Log($"Rotate: curYaw={currentYaw:F1} targetYaw={targetYaw:F1} newYaw={newYaw:F1} angleDiff={angleDiff:F1}");
            }
        }

        private Quaternion GetCurrentRotation()
        {
            if (_modelRoot != null) return _modelRoot.rotation;
            return transform.rotation;
        }

        private void ApplyRotation(Quaternion rot, bool useRigidbody)
        {
            if (_modelRoot != null)
            {
                _modelRoot.rotation = rot;
            }
            else if (_rb != null && useRigidbody)
            {
                _rb.MoveRotation(rot);
            }
            else
            {
                transform.rotation = rot;
            }
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

        // Simple physics helpers states will use
        public void SetVelocityImmediate(Vector3 v)
        {
            _rb.velocity = v;
        }

        public void AddGroundForce(Vector3 force)
        {
            _rb.AddForce(force, ForceMode.Acceleration);
        }

        public void ClampHorizontalSpeed(float maxSpeed)
        {
            Vector3 horizontal = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
            if (horizontal.magnitude > maxSpeed)
            {
                Vector3 clamped = horizontal.normalized * maxSpeed;
                _rb.velocity = new Vector3(clamped.x, _rb.velocity.y, clamped.z);
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
        }

        private void OnEnable()
        {
            _actions.Player.Enable();
            _actions.Player.Jump.performed += ctx => _jumpRequested = true;
            _actions.Player.Attack.performed += ctx => _attackRequested = true;
            _actions.Player.Sprint.performed += ctx => _sprintInput = true;
            _actions.Player.Sprint.canceled += ctx => _sprintInput = false;
            _actions.Player.Inhale.performed += ctx => _inhaleInput = true;
            _actions.Player.Inhale.canceled += ctx => _inhaleInput = false;

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
            _actions.Player.Sprint.canceled -= ctx => _sprintInput = false;
            _actions.Player.Inhale.performed -= ctx => _inhaleInput = true;
            _actions.Player.Inhale.canceled -= ctx => _inhaleInput = false;

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

            // Logic updates: action first (may request overrides) then locomotion
            _actionSM.LogicUpdate(Time.deltaTime);
            _locomotionSM.LogicUpdate(Time.deltaTime);

            // animator generic properties
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            // ground check
            _isGrounded = Physics.CheckSphere(_groundCheck.position + _groundCheckOffset, _groundCheckRadius, _groundMask, QueryTriggerInteraction.Ignore);

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
            Vector3 horizontalVel = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
            _animator.SetFloat(ANIM_SPEED, horizontalVel.magnitude);
            _animator.SetBool(ANIM_IS_GROUNDED, _isGrounded);
            _animator.SetBool(ANIM_IS_SPRINTING, _sprintInput && _moveDirection.sqrMagnitude > 0.01f);
        }

        /// <summary>
        /// Find the best IHomingTarget within range and cone in front of the player.
        /// Returns null if none found.
        /// </summary>
        public JackRussell.States.Action.IHomingTarget FindBestHomingTarget(float range, float coneAngleDeg, LayerMask mask)
        {
            // Use OverlapSphere to find candidate colliders
            Collider[] cols = Physics.OverlapSphere(transform.position, range, mask, QueryTriggerInteraction.Collide);
            if (cols == null || cols.Length == 0) return null;

            Vector3 forward = GetCurrentRotation() * Vector3.forward;
            float bestSqr = float.MaxValue;
            JackRussell.States.Action.IHomingTarget bestTarget = null;

            foreach (var c in cols)
            {
                if (c == null) continue;

                // try to get IHomingTarget from the collider's GameObject or parents
                var target = c.GetComponent<JackRussell.States.Action.IHomingTarget>();
                if (target == null)
                {
                    target = c.GetComponentInParent<JackRussell.States.Action.IHomingTarget>();
                    if (target == null) continue;
                }

                if (!target.IsActive) continue;

                Vector3 toTarget = target.Transform.position - transform.position;
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

        public void OnSprintEnter()
        {
            _shockwaveParticle.Play();
        }

        public void SetPressure(float pressure)
        {
            if (pressure >= 100) return;
            Pressure = pressure;
        }
    }
}
