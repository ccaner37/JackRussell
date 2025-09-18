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

            _player.Animator.ResetTrigger("HomingAttackReach");
            _player.Animator.Play("3001_1_stapla_05_Kick_01_in");

            // request movement override toward target (will be refreshed each physics update)
            Vector3 toTarget = (_target.Transform.position - _player.transform.position);
            Vector3 horiz = new Vector3(toTarget.x, 0f, toTarget.z);
            Vector3 vel = (horiz.sqrMagnitude > 0.0001f ? horiz.normalized : Vector3.zero) * _speed;
            // preserve current vertical velocity by adding rb.velocity.y
            vel.y = _player.Rigidbody.velocity.y;
            _player.RequestMovementOverride(vel, _maxDuration, true);

            // request rotation override so the model faces the target while homing
            if (horiz.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(horiz.normalized, Vector3.up);
                _player.RequestRotationOverride(look, _maxDuration, true);
            }

            _player.PlaySound(Audio.SoundType.HomingAttackStart);
        }

        public override void Exit()
        {
            _player.ClearMovementOverride();
            _player.ClearRotationOverride();
            _player.HideHomingIndicators();
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

            // recompute horizontal direction to target and steer
            Vector3 toTarget = _target.Transform.position - _player.transform.position;
            Vector3 horiz = new Vector3(toTarget.x, toTarget.y, toTarget.z);
            Vector3 currentVel = _player.Rigidbody.velocity;

            if (horiz.sqrMagnitude > 0.0001f)
            {
                Vector3 desired = horiz.normalized * _speed;
                // preserve vertical velocity
                //desired.y = currentVel.y;
                // apply immediate velocity so physics drives the motion predictably
                _player.SetVelocityImmediate(desired);

                // refresh movement override and rotation override so timers remain accurate
                _player.RequestMovementOverride(desired, Mathf.Max(0f, _timer), true);

                Quaternion look = Quaternion.LookRotation(horiz.normalized, Vector3.up);
                _player.RequestRotationOverride(look, Mathf.Max(0f, _timer), true);
            }

            // hit check (use horizontal distance)
            float horizDist = horiz.magnitude;
            if (horizDist <= _hitRadius)
            {
                _player.Animator.SetTrigger("HomingAttackReach");

                // invoke target hit
                _target.OnHomingHit(_player);

                // play optional particle from player
                var ps = _player.HomingHitParticle;
                if (ps != null) ps.Play();

                // apply small bounce using player's jump velocity
                //Vector3 after = _player.Rigidbody.velocity;
                //after.y = _player.JumpVelocity;
                _player.SetVelocityImmediate(new Vector3(0, 5,0));

                // clear overrides and exit
                ChangeState(new ActionNoneState(_player, _stateMachine));

                UnityEngine.Object.FindAnyObjectByType<CinemachineCameraController>().ShakeCamera(1.2f, 2f);
                _player.PlaySound(Audio.SoundType.Kick);

                return;
            }
        }
    }
}
