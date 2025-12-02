using UnityEngine;
using JackRussell;
using JackRussell.Rails;
using DG.Tweening;

namespace JackRussell.States.Action
{
    /// <summary>
    /// Dash Ring component: triggers path launch when player enters the trigger.
    /// </summary>
    public class DashRing : PathLauncherTarget
    {
        [Header("Trigger Settings")]
        [SerializeField] private LayerMask _playerLayer = 1 << 0; // Default layer

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & _playerLayer) != 0)
            {
                Player player = other.GetComponent<Player>();
                if (player != null)
                {
                    // Instantly rotate player towards the path direction
                    if (_launchPath != null && _launchPath.GetPositionAndTangent(0f, out Vector3 _, out Vector3 tangent))
                    {
                        player.RotateTowardsDirection(tangent, 0f, isAir: true, instantaneous: true, allow3DRotation: false);
                    }

                    OnPathLaunch(player);
                    player.PlaySound(Audio.SoundType.DashRing);
                    player.Animator.Play("RingDash");
                }
            }
        }
    }
}