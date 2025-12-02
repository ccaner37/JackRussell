using UnityEngine;
using JackRussell;
using DG.Tweening;

namespace JackRussell.States.Action
{
    public class BellTarget : PathLauncherTarget, IHomingTarget
    {
        [SerializeField] private MeshRenderer _meshRenderer;

        public Transform TargetTransform => transform;

        public bool IsActive => true;

        public void OnHomingHit(Player player)
        {
            // Play bell effects
            if (_meshRenderer != null && _meshRenderer.material != null)
            {
                _meshRenderer.material.SetFloat("_GlitchAmount", 0.6f);
                DOVirtual.DelayedCall(0.3f, () => _meshRenderer.material.DOFloat(0f, "_GlitchAmount", 0.2f));
                player.PlaySound(Audio.SoundType.Bell);
            }
        }
    }
}