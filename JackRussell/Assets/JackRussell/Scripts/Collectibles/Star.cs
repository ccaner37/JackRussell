using UnityEngine;
using DG.Tweening;

namespace JackRussell.Collectibles
{
    /// <summary>
    /// Star collectible item. When collected, notifies the player to increment star count.
    /// Features hover animation with up/down movement and Y-axis rotation.
    /// </summary>
    public class Star : CollectibleItem
    {
        [Header("Hover Animation Settings")]
        [SerializeField] private float _hoverHeight = 0.5f;
        [SerializeField] private float _hoverDuration = 2f;
        [SerializeField] private float _rotationSpeed = 90f; // degrees per second

        private Vector3 _originalPosition;
        private Tween _hoverTween;
        private Tween _rotationTween;

        private void Start()
        {
            _originalPosition = transform.position;

            // Start hover animation
            StartHoverAnimation();
            StartRotationAnimation();
        }

        private void OnDestroy()
        {
            // Clean up tweens
            _hoverTween?.Kill();
            _rotationTween?.Kill();
        }

        private void StartHoverAnimation()
        {
            // Create up and down hover movement
            _hoverTween = transform.DOMoveY(_originalPosition.y + _hoverHeight, _hoverDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo); // Infinite loop with yoyo (up and down)
        }

        private void StartRotationAnimation()
        {
            // Create continuous Y-axis rotation
            _rotationTween = transform.DORotate(new Vector3(0f, 360f, 0f), 360f / _rotationSpeed, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental); // Infinite loop with incremental rotation
        }

        protected override void OnCollected()
        {
            // Stop animations before collection
            _hoverTween?.Kill();
            _rotationTween?.Kill();

            // Find player and notify collection
            var player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                player.CollectStar();
            }
        }
    }
}