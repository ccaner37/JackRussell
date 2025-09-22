using UnityEngine;
using JackRussell;
using DG.Tweening;

namespace JackRussell.States.Action
{
    public class BellTarget : HomingTarget
    {
        [SerializeField] private MeshRenderer _meshRenderer;

        public override Transform TargetTransform => transform;

        public override bool IsActive => true;

        public override void OnHomingHit(Player player)
        {
            if (_meshRenderer != null && _meshRenderer.material != null)
            {
                _meshRenderer.material.DOFloat(0.5f, "_GlitchAmount", 0.03f).OnComplete(() => _meshRenderer.material.DOFloat(0f, "_GlitchAmount", 0.2f).SetDelay(0.3f));
                player.PlaySound(Audio.SoundType.Bell);
            }
        }
    }
}