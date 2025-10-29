using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace JackRussell
{
    public class RendererController : MonoBehaviour
    {
        [SerializeField] private SpeedLinesRendererFeature _speedLinesFeature;
        [SerializeField] private RadialBlurRendererFeature _radialBlurFeature;

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
    }
}