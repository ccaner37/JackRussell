using UnityEngine;
using JackRussell;
using JackRussell.Rails;
using DG.Tweening;

namespace JackRussell.States.Action
{
    /// <summary>
    /// Abstract base class for objects that launch the player along a predefined spline path.
    /// Extends HomingTarget to provide path-following functionality for spring pads, launchers, etc.
    /// </summary>
    public abstract class PathLauncherTarget : HomingTarget
    {
        [Header("Path Launch Settings")]
        [SerializeField] protected SplineRail _launchPath;
        [SerializeField] protected bool _allowCollisionLaunch = true;

        [Header("Easing Settings")]
        [SerializeField] protected AnimationCurve _speedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // Starts slow, speeds up, slows down
        [SerializeField] protected float _launchDurationMultiplier = 1f;

        /// <summary>
        /// The spline path to launch the player along.
        /// </summary>
        public SplineRail LaunchPath => _launchPath;

        /// <summary>
        /// Whether collision with this target should trigger path launch.
        /// </summary>
        public bool AllowCollisionLaunch => _allowCollisionLaunch;

        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (!_allowCollisionLaunch) return;

            // Check if the colliding object is the player
            var player = collision.gameObject.GetComponent<Player>();
            if (player != null)
            {
                // Don't launch if player is currently in homing attack state
                if (player.ActionStateName == "HomingAttackState")
                {
                    return;
                }

                OnPathLaunch(player);
            }
        }

        /// <summary>
        /// Called when the player should be launched along the path.
        /// Default implementation triggers the PathFollowState with easing.
        /// </summary>
        protected virtual void OnPathLaunch(Player player)
        {
            if (_launchPath != null)
            {
                // Calculate duration based on path length and base speed (25f from PathFollowState)
                float baseDuration = _launchPath.TotalLength / 25f;
                float duration = baseDuration * _launchDurationMultiplier;
                player.EnterPathFollowState(_launchPath, _speedCurve, duration);
            }
            else
            {
                Debug.LogWarning("[PathLauncherTarget] No launch path assigned!", this);
            }
        }

        public override void OnHitStopEnd(Player player)
        {
            // Call base implementation if needed, then launch
            OnPathLaunch(player);
        }
    }
}