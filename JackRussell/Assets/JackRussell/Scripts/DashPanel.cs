using UnityEngine;
using JackRussell.Rails;

namespace JackRussell
{
    /// <summary>
    /// Dash Panel component: triggers dash panel state when player interacts with it.
    /// </summary>
    public class DashPanel : MonoBehaviour
    {
        [Header("Path")]
        [SerializeField] private SplinePath _splinePath;

        [Header("Dash Settings")]
        [SerializeField] private float _dashSpeed = 30f;
        [SerializeField] private float _duration = 2f;
        [SerializeField] private bool _allowSprint = true;
        [SerializeField] private float _sprintSpeedMultiplier = 1.5f;

        [Header("Trigger")]
        [SerializeField] private LayerMask _playerLayer = 1 << 0; // Default layer

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & _playerLayer) != 0)
            {
                Player player = other.GetComponent<Player>();
                if (player != null)
                {
                    player.EnterDashPanelState(_splinePath, _dashSpeed, _duration, _allowSprint, _sprintSpeedMultiplier);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 2f); // Simple cube gizmo
        }
    }
}