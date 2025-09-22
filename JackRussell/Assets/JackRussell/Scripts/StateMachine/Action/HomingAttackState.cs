using UnityEngine;
using JackRussell;
using JackRussell.CameraController;

namespace JackRussell.States.Action
{
    /// <summary>
    /// Airborne homing attack state.
    /// Locks to the nearest IHomingTarget within the player's configured range/cone,
    /// drives the player towards it, and invokes OnHomingHit when close enough.
    /// After hit (or timeout) the state returns to ActionNoneState.
    /// </summary>
    public class HomingAttackState : PlayerActionStateBase
    {
        private readonly float _maxDuration;
        private readonly float _speed;
        private readonly float _hitRadius;

        private IHomingTarget _target;
        private float _timer;
        private float _initialDistance;
        private bool _reachTriggered;
        private bool _effectTriggered;
        private bool _hitStopActive;
        private float _hitStopTimer;

        public HomingAttackState(Player player, StateMachine stateMachine) : base(player, stateMachine)
        {
            _maxDuration = player.HomingDuration;
            _speed = player.HomingSpeed;
            _hitRadius = player.HomingHitRadius;
        }

        public override string Name => nameof(HomingAttackState);

        public override void Enter()
        {
            // Check pressure
            if (_player.Pressure < 10f)
            {
                // not enough pressure, exit immediately
                ChangeState(new ActionNoneState(_player, _stateMachine));
                return;
            }

            // Consume pressure
            _player.SetPressure(_player.Pressure - 10f);

            // find a valid target using player's helper
            _target = _player.FindBestHomingTarget(_player.HomingRange, _player.HomingConeAngle, _player.HomingMask);
            _timer = _maxDuration;

            if (_target == null)
            {
                // no target, exit immediately
                ChangeState(new ActionNoneState(_player, _stateMachine));
                return;
            }

            _player.OnHomingAttackEnter();

            // initialize homing attack variables
            Vector3 toTarget = (_target.Transform.position - _player.transform.position);
            _initialDistance = toTarget.magnitude;
            _reachTriggered = false;
            _hitStopActive = false;

            // request movement override toward target (will be refreshed each physics update)
            Vector3 horiz = new Vector3(toTarget.x, 0f, toTarget.z);
            Vector3 vel = (horiz.sqrMagnitude > 0.0001f ? horiz.normalized : Vector3.zero) * _speed;
            // preserve current vertical velocity by adding rb.velocity.y
            vel.y = _player.Rigidbody.linearVelocity.y;
            _player.RequestMovementOverride(vel, _maxDuration, true);

            // rotate towards target (full 3D rotation, instantaneous for initial snap)
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                _player.RotateTowardsDirection(toTarget, Time.fixedDeltaTime, isAir: true, instantaneous: true, allow3DRotation: true);
            }

            _player.PlaySound(Audio.SoundType.HomingAttackStart);
        }

        public override void Exit()
        {
            _player.ClearMovementOverride();
            _player.HideHomingIndicators();

            // Reset vertical rotation to horizontal
            Quaternion currentRot = _player.transform.rotation;
            Vector3 euler = currentRot.eulerAngles;
            euler.x = 0f; // reset pitch
            euler.z = 0f; // reset roll
            _player.transform.rotation = Quaternion.Euler(euler);
        }

        public override void LogicUpdate()
        {
            // simple timer in logic loop as well (keeps consistent shutdown if logic runs faster)
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                ChangeState(new ActionNoneState(_player, _stateMachine));
                return;
            }

            // if the target became invalid, abort
            if (_target == null || !_target.IsActive)
            {
                ChangeState(new ActionNoneState(_player, _stateMachine));
                return;
            }
        }

        public override void PhysicsUpdate()
        {
            if (_target == null || !_target.IsActive)
            {
                ChangeState(new ActionNoneState(_player, _stateMachine));
                return;
            }

            // recompute direction to target
            Vector3 toTarget = _target.Transform.position - _player.transform.position;
            float currentDistance = toTarget.magnitude;
            Vector3 horiz = new Vector3(toTarget.x, 0f, toTarget.z); // horizontal direction for hit check

            // trigger OnHomingAttackReach when close to target
            if (!_reachTriggered && currentDistance <= 4f)
            {
                _player.OnHomingAttackReach();
                _reachTriggered = true;
            }

            if (!_effectTriggered && currentDistance <= 2f)
            {
                // camera shake and sounds
                UnityEngine.Object.FindAnyObjectByType<CinemachineCameraController>().ShakeCamera(1.2f, 2f);
                _player.PlaySound(Audio.SoundType.Kick);
                _player.PlaySound(Audio.SoundType.Spinz808);
                _effectTriggered = true;
            }

            // hit check (use 3D distance for consistency, with buffer to prevent overshooting)
                if (currentDistance <= _hitRadius)
                {
                    if (!_hitStopActive)
                    {
                        // start hit stop
                        _hitStopActive = true;
                        _hitStopTimer = 0.2f;
                        _player.SetVelocityImmediate(Vector3.zero);
                        _player.ClearMovementOverride();
                    }
                }

            if (_hitStopActive)
            {
                // during hit stop
                _hitStopTimer -= Time.fixedDeltaTime;
                if (_hitStopTimer <= 0f)
                {
                    // invoke target hit
                    _target.OnHomingHit(_player);

                    // play optional particle from player
                    var ps = _player.HomingHitParticle;
                    if (ps != null) ps.Play();

                    // apply upward push
                    _player.SetVelocityImmediate(new Vector3(0f, _player.JumpVelocity, 0f));

                    // exit state
                    ChangeState(new ActionNoneState(_player, _stateMachine));
                    return;
                }
            }
            else
            {
                // normal movement and rotation
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    Vector3 desired = toTarget.normalized * _speed;
                    _player.SetVelocityImmediate(desired);
                    _player.RequestMovementOverride(desired, Mathf.Max(0f, _timer), true);
                    _player.RotateTowardsDirection(toTarget, Time.fixedDeltaTime, isAir: true, instantaneous: false, allow3DRotation: true);
                }
            }
        }
    }
}
