using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VitalRouter;
using DG.Tweening;
using System.Collections.Generic;
using Coffee.UIExtensions;

namespace JackRussell.UI
{
    public class ParticleEffectUI : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _targetRectTransform; // The pressure bar image
        [SerializeField] private GameObject _particlePrefab; // Prefab with Image component
        [SerializeField] private float _animationDuration = 1f;
        [SerializeField] private float _maxDistance = 50f; // Max distance for scaling
        [SerializeField] private float _minScale = 0.5f;
        [SerializeField] private float _maxScale = 2f;
        [SerializeField] private int _particleCount = 5; // Number of particles to spawn

        [Inject] private readonly ICommandSubscribable _commandSubscribable;

        private Camera _mainCamera;
        private List<GameObject> _activeParticles = new List<GameObject>();

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            _commandSubscribable.Subscribe<PressureCollectParticleCommand>((cmd, ctx) => OnPressureCollectParticle(cmd));
        }

        private void OnPressureCollectParticle(PressureCollectParticleCommand command)
        {
            SpawnParticles(command.EnemyWorldPosition);
        }

        private void SpawnParticles(Vector3 enemyWorldPosition)
        {
            if (_canvas == null || _targetRectTransform == null || _particlePrefab == null) return;

            // Convert world position to viewport position (0-1)
            Vector3 viewportPos = _mainCamera.WorldToViewportPoint(enemyWorldPosition);

            // Convert viewport to canvas local position
            RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
            Vector2 canvasSize = canvasRect.sizeDelta;
            Vector2 canvasPos = new Vector2(
                (viewportPos.x - 0.5f) * canvasSize.x,
                (viewportPos.y - 0.5f) * canvasSize.y
            );

            // Calculate distance for scaling
            float distance = Vector3.Distance(_mainCamera.transform.position, enemyWorldPosition);
            float scale = Mathf.Lerp(_maxScale, _minScale, Mathf.Clamp01(distance / _maxDistance));

            // Target position is the local position of the target RectTransform
            Vector2 targetPos = _targetRectTransform.localPosition;
            Debug.LogError(targetPos);

            // Spawn particles
            for (int i = 0; i < _particleCount; i++)
            {
                GameObject particle = Instantiate(_particlePrefab, _canvas.transform);
                particle.SetActive(true);
                RectTransform particleRect = particle.GetComponent<RectTransform>();
                particleRect.localPosition = canvasPos + Random.insideUnitCircle * 100f; // Slight random offset

                // Set scale using UIParticle component
                var uiParticle = particle.GetComponent<UIParticle>();
                uiParticle.scale = scale;

                // Animate to target with curved path
                AnimateParticle(particleRect, uiParticle, targetPos, scale);
            }
        }

        private void AnimateParticle(RectTransform particleRect, UIParticle uiParticle, Vector2 targetPos, float initialScale)
        {
            // Create a curved path using bezier
            Vector2 startPos = particleRect.localPosition;
            Vector2 controlPoint = (startPos + targetPos) / 2 + Vector2.up * 100f; // Control point above

            // Animate position
            particleRect.DOLocalPath(
                new Vector3[] { startPos, controlPoint, targetPos },
                _animationDuration,
                PathType.CatmullRom
            ).SetEase(Ease.InOutQuad);

            // Animate scale to grow bigger
            DOTween.To(() => uiParticle.scale, x => uiParticle.scale = x, initialScale * 2f, _animationDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() =>
                {
                    // Cleanup
                    Destroy(particleRect.gameObject);
                });
        }
    }
}