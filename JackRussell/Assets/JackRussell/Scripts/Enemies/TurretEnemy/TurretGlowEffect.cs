using UnityEngine;
using DG.Tweening;

namespace JackRussell.Enemies
{
    /// <summary>
    /// Handles the glow effect for turret enemies during the charging phase.
    /// Manipulates material properties to create a visual charging indicator.
    /// </summary>
    public class TurretGlowEffect : MonoBehaviour
    {
        [Header("Glow Settings")]
        [SerializeField] private Material _glowMaterial;
        [SerializeField] private string _glowPropertyName = "_Glow";
        [SerializeField] private AnimationCurve _glowCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float _maxGlowValue = 1f;
        
        [Header("Renderer References")]
        [SerializeField] private Renderer[] _affectedRenderers;
        
        private Material[] _originalMaterials;
        private bool _isGlowing = false;
        private float _preparationTime;
        private float _parryTime; // Store parry time for phase calculations
        private float _elapsedTime;
        private Tween _glowTween;
        
        public bool IsGlowing => _isGlowing;
        
        private void Awake()
        {
            // Store original materials
            if (_affectedRenderers != null && _affectedRenderers.Length > 0)
            {
                _originalMaterials = new Material[_affectedRenderers.Length];
                for (int i = 0; i < _affectedRenderers.Length; i++)
                {
                    if (_affectedRenderers[i] != null)
                    {
                        _originalMaterials[i] = _affectedRenderers[i].material;
                    }
                }
            }
        }
        
        /// <summary>
        /// Start the glow effect for the specified preparation time
        /// </summary>
        /// <param name="preparationTime">Time to reach maximum glow</param>
        public void StartGlow(float preparationTime, float parryTime = 0.65f)
        {
            if (_isGlowing || preparationTime <= 0f) return;
            
            _preparationTime = preparationTime;
            _parryTime = parryTime; // Store parry time for phase calculations
            _elapsedTime = 0f;
            _isGlowing = true;
            
            // Apply glow material to renderers
            ApplyGlowMaterial();
            
            // Start the two-phase glow animation
            StartTwoPhaseGlowAnimation();
        }
        
        /// <summary>
        /// Stop the glow effect and reset materials
        /// </summary>
        public void StopGlow()
        {
            if (!_isGlowing) return;
            
            _isGlowing = false;
            
            // Kill any ongoing tween
            if (_glowTween != null)
            {
                _glowTween.Kill();
                _glowTween = null;
            }
            
            // Reset glow value
            SetGlowValue(0f);
            
            // Restore original materials
            RestoreOriginalMaterials();
        }
        
        /// <summary>
        /// Set the glow value directly (for manual control)
        /// </summary>
        /// <param name="value">Glow value (0-1)</param>
        public void SetGlowValue(float value)
        {
            if (_glowMaterial != null)
            {
                float clampedValue = Mathf.Clamp01(value);
                _glowMaterial.SetFloat(_glowPropertyName, clampedValue * _maxGlowValue);
            }
        }
        
        private void StartTwoPhaseGlowAnimation()
        {
            if (_glowMaterial == null) return;
            
            // Kill any existing tween
            if (_glowTween != null)
            {
                _glowTween.Kill();
            }
            
            // Calculate timing based on actual parry time
            // Phase 1: Slow glow to 8% during 
            // Phase 2: Fast glow to 100% during last 0.1f seconds
            float earlyGlowTime = _preparationTime - _parryTime - 0.1f;
            float rapidGlowTime = 0.1f;
            
            // Ensure we don't have negative or zero time for phase 1
            earlyGlowTime = Mathf.Max(earlyGlowTime, 0.1f); // Minimum 0.1s for phase 1
            
            // Phase 1: Slow increase to 8%
            _glowTween = DOTween.To(() => 0f, SetGlowValue, 0.1f, earlyGlowTime)
                .SetEase(Ease.InSine)
                .OnComplete(() =>
                {
                    // Phase 2: Fast increase to 100% in remaining time
                    _glowTween = DOTween.To(() => 0.1f, SetGlowValue, 1f, rapidGlowTime)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            // Keep glowing at max value until stopped
                            SetGlowValue(1f);
                        });
                });
        }
        
        private void ApplyGlowMaterial()
        {
            if (_affectedRenderers == null || _glowMaterial == null) return;
            
            foreach (var renderer in _affectedRenderers)
            {
                if (renderer != null)
                {
                    renderer.material = _glowMaterial;
                }
            }
        }
        
        private void RestoreOriginalMaterials()
        {
            if (_affectedRenderers == null || _originalMaterials == null) return;
            
            for (int i = 0; i < _affectedRenderers.Length; i++)
            {
                if (_affectedRenderers[i] != null && _originalMaterials[i] != null)
                {
                    _affectedRenderers[i].material = _originalMaterials[i];
                }
            }
        }
        
        private void OnDestroy()
        {
            // Clean up tween
            if (_glowTween != null)
            {
                _glowTween.Kill();
            }
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure we have valid references
            if (_affectedRenderers == null || _affectedRenderers.Length == 0)
            {
                // Try to find renderers on this GameObject or children
                _affectedRenderers = GetComponentsInChildren<Renderer>();
            }
        }
        
        private void Reset()
        {
            // Auto-find renderers when component is added
            _affectedRenderers = GetComponentsInChildren<Renderer>();
        }
#endif
    }
}