using UnityEngine;
using JackRussell;

namespace JackRussell.States.Action
{
    /// <summary>
    /// Simple example implementation of IHomingTarget.
    /// Attach to enemies, springs or items you want the player to be able to homing-attack.
    /// This example will disable the GameObject on hit; replace with damage/response logic as needed.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SimpleHomingTarget : MonoBehaviour, IHomingTarget
    {
        [SerializeField] private bool _isActive = true;
        [SerializeField] private ParticleSystem _hitEffect;

        public Transform Transform => transform;

        public bool IsActive => _isActive;

        public void OnHomingHit(Player player)
        {
            // Play an optional hit effect
            if (_hitEffect != null)
            {
                _hitEffect.Play(true);
            }

            // Example behavior: deactivate the target (could be destroy, apply damage, spring bounce, etc.)
            //_isActive = false;
            //gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // ensure collider is trigger-capable if designer expects overlap queries
            var col = GetComponent<Collider>();
            if (col != null && !col.enabled)
            {
                Debug.LogWarning($"SimpleHomingTarget on {name} has a disabled Collider.", this);
            }
        }
#endif
    }
}
