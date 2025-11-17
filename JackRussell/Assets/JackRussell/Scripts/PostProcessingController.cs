using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

namespace JackRussell.GamePostProcessing
{
    public class PostProcessingController : MonoBehaviour
    {
        [SerializeField] private SpeedLinesRendererFeature _speedLinesFeature;
        [SerializeField] private RadialBlurRendererFeature _radialBlurFeature;
        [SerializeField] private RadialMotionBlurRendererFeature _radialMotionBlurFeature;
        [SerializeField] private Volume _volume;
        private ColorAdjustments _colorAdjustments;
        
        // Default values for restoration
        private float _defaultPostExposure;
        private float _defaultContrast;
        private Color _defaultColorFilter;
        private float _defaultSaturation;
        private bool _defaultValuesStored = false;

        public Volume Volume => _volume;

        void Awake()
        {
            _volume.profile.TryGet<ColorAdjustments>(out _colorAdjustments);
        }

        public void SetSpeedLinesIntensity(float intensity)
        {
            if (_speedLinesFeature != null)
            {
                _speedLinesFeature.intensity = intensity;
            }
        }

        public void SetRadialBlurIntensity(float intensity)
        {
            if (_radialBlurFeature != null)
            {
                _radialBlurFeature.effectIntensity = intensity;
            }
        }

        public void SetRadialMotionBlurIntensity(float intensity)
        {
            if (_radialBlurFeature != null)
            {
                _radialMotionBlurFeature.settings.blurStrength = intensity;
            }
        }

        public void ParryAttackEffect()
        {
            if (_colorAdjustments == null) return;

            // Store default values on first call
            if (!_defaultValuesStored)
            {
                _defaultPostExposure = _colorAdjustments.postExposure.value;
                _defaultContrast = _colorAdjustments.contrast.value;
                _defaultColorFilter = _colorAdjustments.colorFilter.value;
                _defaultSaturation = _colorAdjustments.saturation.value;
                _defaultValuesStored = true;
            }

            // Animate to parry attack values over 0.15 seconds
            DOTween.To(() => _colorAdjustments.postExposure.value,
                      x => _colorAdjustments.postExposure.value = x,
                      -0.1f, 0.15f);
                      
            DOTween.To(() => _colorAdjustments.contrast.value,
                      x => _colorAdjustments.contrast.value = x,
                      24f, 0.15f);
                      
            DOTween.To(() => _colorAdjustments.colorFilter.value,
                      x => _colorAdjustments.colorFilter.value = x,
                      new Color(223f/255f, 224f/255f, 255f/255f), 0.15f);
                      
            DOTween.To(() => _colorAdjustments.saturation.value,
                      x => _colorAdjustments.saturation.value = x,
                      25f, 0.15f);
        }

        public void RestoreDefaultValues()
        {
            if (_colorAdjustments == null || !_defaultValuesStored) return;

            // Animate back to default values over 0.15 seconds
            DOTween.To(() => _colorAdjustments.postExposure.value,
                      x => _colorAdjustments.postExposure.value = x,
                      _defaultPostExposure, 0.15f);
                      
            DOTween.To(() => _colorAdjustments.contrast.value,
                      x => _colorAdjustments.contrast.value = x,
                      _defaultContrast, 0.15f);
                      
            DOTween.To(() => _colorAdjustments.colorFilter.value,
                      x => _colorAdjustments.colorFilter.value = x,
                      _defaultColorFilter, 0.15f);
                      
            DOTween.To(() => _colorAdjustments.saturation.value,
                      x => _colorAdjustments.saturation.value = x,
                      _defaultSaturation, 0.15f)
                      .OnComplete(() =>
                      {
                          // Clear default values after restoration
                          _defaultValuesStored = false;
                      });
        }
    }
}