using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace JackRussell.States.Action
{
    /// <summary>
    /// Handles the visual behavior of homing attack indicators.
    /// Attached to the indicator prefab (World Space Canvas).
    /// </summary>
    public class HomingIndicator : MonoBehaviour
    {
        [SerializeField] private Image _indicatorImage;
        [SerializeField] private RectTransform _rotatingPart;
        [SerializeField] private RectTransform _parentTransform;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _minScale = 0.5f;
        [SerializeField] private float _maxScale = 1.5f;
        [SerializeField] private float _minDistance = 5f;
        [SerializeField] private float _maxDistance = 20f;
        [SerializeField] private float _appearDuration = 0.3f;
        [SerializeField] private float _rotationSlowDownDuration = 1f;

        private Transform _targetTransform;
        private Camera _mainCamera;
        private Tween _appearTween;
        private Tween _rotationTween;
        private bool _isAppearing;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            // Start appear animation
            Appear();
        }

        private void OnDisable()
        {
            // Kill any ongoing tweens
            if (_appearTween != null)
            {
                _appearTween.Kill();
                _appearTween = null;
            }
            if (_rotationTween != null)
            {
                _rotationTween.Kill();
                _rotationTween = null;
            }
            _isAppearing = false;
        }

        private void LateUpdate()
        {
            if (_targetTransform == null || _mainCamera == null) return;

            // Face the camera
            transform.LookAt(_mainCamera.transform);

            // Scale based on distance
            float distance = Vector3.Distance(_mainCamera.transform.position, _targetTransform.position);
            float normalizedDistance = Mathf.InverseLerp(_minDistance, _maxDistance, distance);
            float scale = Mathf.Lerp(_maxScale, _minScale, normalizedDistance);
            transform.localScale = Vector3.one * scale;
        }

        /// <summary>
        /// Sets the target this indicator is attached to.
        /// </summary>
        public void SetTarget(Transform target)
        {
            _targetTransform = target;
            if (target != null)
            {
                transform.position = target.position; // At the center of the target
            }
        }

        private void Appear()
        {
            if (_isAppearing) return;

            _isAppearing = true;

            // Reset initial state
            _parentTransform.localScale = new Vector3(6f, 6f, 6f);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
            if (_rotatingPart != null)
            {
                _rotatingPart.localRotation = Quaternion.identity;
            }

            // Animate scale down and alpha fade
            Sequence sequence = DOTween.Sequence();
            sequence.Append(_parentTransform.DOScale(Vector3.one, _appearDuration).SetEase(Ease.OutQuad));
            if (_canvasGroup != null)
            {
                sequence.Join(_canvasGroup.DOFade(1f, _appearDuration));
            }

            // Add rotation animation for the rotating part
            if (_rotatingPart != null)
            {
                // Fast rotation then slow down
                _rotationTween = _rotatingPart.DOLocalRotate(new Vector3(0, 0, 360f), _rotationSlowDownDuration, RotateMode.FastBeyond360)
                    .SetEase(Ease.OutQuart) // Starts fast, slows down
                    .SetLoops(-1, LoopType.Incremental); // Continuous rotation at slowing speed
            }

            sequence.OnComplete(() => _isAppearing = false);

            _appearTween = sequence;
        }

        /// <summary>
        /// Instantly hide the indicator (no animation).
        /// </summary>
        public void Disappear()
        {
            if (_appearTween != null)
            {
                _appearTween.Kill();
                _appearTween = null;
            }
            if (_rotationTween != null)
            {
                _rotationTween.Kill();
                _rotationTween = null;
            }
            _isAppearing = false;
            gameObject.SetActive(false);
        }
    }
}