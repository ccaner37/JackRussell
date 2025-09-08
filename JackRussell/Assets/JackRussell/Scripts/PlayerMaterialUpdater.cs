using UnityEngine;

namespace JackRussell
{
    [DisallowMultipleComponent]
    public class PlayerMaterialUpdater : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Player _player;
        [SerializeField] private Renderer[] _renderers;

        [Header("Scaling")]
        [Tooltip("If > 0 this value is used as the max speed for normalization. Otherwise player speed fields are used.")]
        [SerializeField] private float _explicitMaxSpeed = 0f;

        [Header("Shader Ranges")]
        [SerializeField] private float _maxSmoke = 3f;
        [SerializeField] private float _maxFlow = 5f;

        [Header("Smoothing")]
        [SerializeField, Range(0f, 50f)] private float _smoothing = 8f;

        [Header("Options")]
        [SerializeField] private bool _useAnimatorSpeed = false;
        [SerializeField] private string _smokeProp = "_SmokeIntensity";
        [SerializeField] private string _flowProp = "_FlowSpeed";

        // Runtime
        private MaterialPropertyBlock _mpb;
        private float _currentSmoke = 0f;
        private float _currentFlow = 0f;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (_player == null || _renderers == null || _renderers.Length == 0) return;

            float horizontalSpeed = 0f;

            if (_useAnimatorSpeed)
            {
                horizontalSpeed = _player.AnimatorSpeed;
            }
            else
            {
                var rb = _player.Rigidbody;
                if (rb != null)
                {
                    Vector3 hvel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                    horizontalSpeed = hvel.magnitude;
                }
            }

            float maxSpeed = _explicitMaxSpeed > 0f
                ? _explicitMaxSpeed
                : Mathf.Max(_player.WalkSpeed, _player.RunSpeed, _player.BoostSpeed, _player.DashSpeed);

            float normalized = (maxSpeed > 0f) ? Mathf.Clamp01(horizontalSpeed / maxSpeed) : 0f;

            float targetSmoke = Mathf.Lerp(0f, _maxSmoke, normalized);
            float targetFlow = Mathf.Lerp(0f, _maxFlow, normalized);

            // Exponential smoothing factor for frame-rate independent smoothing
            float t = 1f - Mathf.Exp(-_smoothing * Time.deltaTime);
            _currentSmoke = Mathf.Lerp(_currentSmoke, targetSmoke, t);
            _currentFlow = Mathf.Lerp(_currentFlow, targetFlow, t);

            // Apply via MaterialPropertyBlock to avoid creating material instances
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat(_smokeProp, _currentSmoke);
                _mpb.SetFloat(_flowProp, _currentFlow);
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
