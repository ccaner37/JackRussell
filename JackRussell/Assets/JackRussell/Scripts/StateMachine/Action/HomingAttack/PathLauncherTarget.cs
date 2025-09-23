using UnityEngine;
using JackRussell;
using JackRussell.Rails;

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
        /// Default implementation triggers the PathFollowState.
        /// </summary>
        protected virtual void OnPathLaunch(Player player)
        {
            if (_launchPath != null)
            {
                player.EnterPathFollowState(_launchPath);
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