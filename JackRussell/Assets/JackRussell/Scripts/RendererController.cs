using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace JackRussell
{
    public class RendererController : MonoBehaviour
    {
        [SerializeField] private SpeedLinesRendererFeature _speedLinesFeature;

        public void SetSpeedLinesIntensity(float intensity)
        {
            if (_speedLinesFeature != null)
            {
                _speedLinesFeature.intensity = intensity;
            }
        }
    }
}