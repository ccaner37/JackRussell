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
    public class SimpleHomingTarget : MonoBehaviour, IHomingTarget
    {
        [SerializeField] private bool _isActive = true;
        [SerializeField] private ParticleSystem _hitEffect;
        [SerializeField] private MeshRenderer[] _hitEffectRenderers;

        public Transform TargetTransform => transform;

        public bool IsActive => _isActive;

        public void OnHomingHit(Player player)
        {
            // Play an optional hit effect
            if (_hitEffect != null)
            {
                _hitEffect.Play(true);
            }

            // Apply hit material effect
            OnHitMaterialEffect();

            // Example behavior: deactivate the target (could be destroy, apply damage, spring bounce, etc.)
            transform.DOPunchScale(Vector3.one, 0.25f, 10, 1).OnComplete(() => StartCoroutine(TestingEnableBack()));
        }

        private void OnHitMaterialEffect()
        {
            if (_hitEffectRenderers != null)
            {
                foreach (var renderer in _hitEffectRenderers)
                {
                    if (renderer != null && renderer.material != null)
                    {
                        renderer.material.DOFloat(1f, "_HitBlend", 0.1f);
                    }
                }
            }
        }
        
        public void OnHitStopEnd(Player player)
        {
            // Default implementation does nothing
            // Override for custom behaviors
        }

        private IEnumerator TestingEnableBack()
        {
            //yield return new WaitForSeconds(0.1f);
            _isActive = false;
            foreach (var renderer in _hitEffectRenderers)
            {
                renderer.enabled = false;
            }
            yield return new WaitForSeconds(2f);
            foreach (var renderer in _hitEffectRenderers)
            {
                renderer.enabled = true;
            }
            _isActive = true;

            foreach (var renderer in _hitEffectRenderers)
            {
                renderer.material.SetFloat("_HitBlend", 0);
            }
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
