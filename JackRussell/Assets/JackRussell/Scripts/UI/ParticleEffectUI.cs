using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using VContainer;
using VitalRouter;
using DG.Tweening;
using System.Collections.Generic;
using Coffee.UIExtensions;
using JackRussell.Audio;

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
        [Inject] private readonly ICommandPublisher _commandPublisher;
        [Inject] private readonly AudioManager _audioManager;

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
            SpawnParticles(command.EnemyWorldPosition, command.TotalPressure);
        }

        private void SpawnParticles(Vector3 enemyWorldPosition, float totalPressure)
        {
            if (_canvas == null || _targetRectTransform == null || _particlePrefab == null) return;

            // Calculate pressure per particle
            float pressurePerParticle = totalPressure / _particleCount;

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

            // Spawn particles with random delays
            for (int i = 0; i < _particleCount; i++)
            {
                GameObject particle = Instantiate(_particlePrefab, _canvas.transform);
                RectTransform particleRect = particle.GetComponent<RectTransform>();
                particleRect.localPosition = canvasPos + Random.insideUnitCircle * 100f; // Slight random offset

                // Set scale using UIParticle component
                var uiParticle = particle.GetComponent<UIParticle>();
                uiParticle.scale = scale;

                // Random delay before starting animation
                float delay = Random.Range(0.01f, 0.02f) + (i * 0.06f);
                DOVirtual.DelayedCall(delay, () => AnimateParticle(particleRect, uiParticle, targetPos, scale, pressurePerParticle));
            }
        }

        private void AnimateParticle(RectTransform particleRect, UIParticle uiParticle, Vector2 targetPos, float initialScale, float pressurePerParticle)
        {
            particleRect.gameObject.SetActive(true);

            Vector2 startPos = particleRect.localPosition;

            // Create sequence
            Sequence sequence = DOTween.Sequence();

            // Phase 1: Move up with some random X
            float upDistance = Random.Range(300, 350);
            float xAmount = Random.Range(-50f, 50f); // Random left/right
            Vector2 upPos = startPos + Vector2.up * upDistance + Vector2.right * xAmount;
            float upDuration = _animationDuration * 0.6f;

            sequence.Append(
                particleRect.DOLocalMove(upPos, upDuration).SetEase(Ease.OutQuad)
            );

            // Phase 2: Instant curve to target
            Vector2 controlPoint = (upPos + targetPos) / 2 + Vector2.up * 100f;
            float curveDuration = _animationDuration * 0.4f;

            sequence.Append(
                particleRect.DOLocalPath(
                    new Vector3[] { upPos, controlPoint, targetPos },
                    curveDuration,
                    PathType.CatmullRom
                ).SetEase(Ease.InOutQuad)
            );

            // Animate scale to grow bigger over total duration
            sequence.Insert(0,
                DOTween.To(() => uiParticle.scale, x => uiParticle.scale = x, initialScale * 2f, _animationDuration)
                    .SetEase(Ease.InOutQuad)
            );

            // On complete
            sequence.OnComplete(() =>
            {
                _audioManager.PlaySound(SoundType.PressureCollectParticle);
                _commandPublisher.PublishAsync(new PressureParticleCollectedCommand(pressurePerParticle));
                Destroy(particleRect.gameObject);
            });

            sequence.Play();
        }
    }
}