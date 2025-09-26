using UnityEngine;
using JackRussell;
using System.Collections;
using DG.Tweening;

namespace JackRussell.States.Action
{
    /// <summary>
    /// Simple example implementation of HomingTarget.
    /// Attach to enemies, springs or items you want the player to be able to homing-attack.
    /// This example will disable the GameObject on hit; replace with damage/response logic as needed.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SimpleHomingTarget : HomingTarget
    {
        [SerializeField] private bool _isActive = true;
        [SerializeField] private ParticleSystem _hitEffect;
        [SerializeField] private MeshRenderer _renderer;

        public override Transform TargetTransform => transform;

        public override bool IsActive => _isActive;

        public override void OnHomingHit(Player player)
        {
            // Play an optional hit effect
            if (_hitEffect != null)
            {
                _hitEffect.Play(true);
            }

            // Example behavior: deactivate the target (could be destroy, apply damage, spring bounce, etc.)
            transform.DOPunchScale(Vector3.one, 0.25f, 10, 1).OnComplete(() => StartCoroutine(EnableBack()));
        }

        private IEnumerator EnableBack()
        {
            //yield return new WaitForSeconds(0.1f);
            _isActive = false;
            _renderer.enabled = false;
            yield return new WaitForSeconds(2f);
            _renderer.enabled = true;
            _isActive = true;
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
