using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace JackRussell.Rails
{
    /// <summary>
    /// Generic spline path component using Unity Splines.
    /// Provides core functionality for path following without rail-specific features.
    /// </summary>
    [RequireComponent(typeof(SplineContainer))]
    public class SplinePath : MonoBehaviour
    {
        // Cached references
        private SplineContainer _splineContainer;
        private Spline _spline;

        // Runtime data
        private float _totalLength;
        private bool _isInitialized;

        public Spline Spline => _spline;
        public float TotalLength => GetTotalLength();

        /// <summary>
        /// Get the total length of the spline, initializing if necessary
        /// </summary>
        private float GetTotalLength()
        {
            if (!_isInitialized)
            {
                InitializeSpline();
            }
            return _totalLength;
        }

        private void Awake()
        {
            InitializeSpline();
        }

        private void InitializeSpline()
        {
            _splineContainer = GetComponent<SplineContainer>();
            if (_splineContainer == null)
            {
                Debug.LogError("SplinePath requires a SplineContainer component!", this);
                return;
            }

            _spline = _splineContainer.Spline;
            if (_spline == null || _spline.Count < 2)
            {
                Debug.LogError("SplinePath requires a valid spline with at least 2 knots!", this);
                return;
            }

            // Calculate total length for parameterization
            _totalLength = _spline.GetLength();
            _isInitialized = true;
        }

        /// <summary>
        /// Get position and tangent at a specific distance along the path
        /// </summary>
        public bool GetPositionAndTangent(float distance, out Vector3 position, out Vector3 tangent)
        {
            Vector3 up;
            return GetPositionAndTangent(distance, out position, out tangent, out up);
        }

        /// <summary>
        /// Get position, tangent, and up vector at a specific distance along the path
        /// </summary>
        public bool GetPositionAndTangent(float distance, out Vector3 position, out Vector3 tangent, out Vector3 up)
        {
            position = Vector3.zero;
            tangent = Vector3.forward;
            up = Vector3.up;

            if (!_isInitialized) return false;

            // Clamp distance to valid range
            distance = Mathf.Clamp(distance, 0f, _totalLength);

            // Convert distance to normalized parameter (0-1)
            float t = distance / _totalLength;

            // Evaluate spline at parameter t
            float3 upVector;
            _spline.Evaluate(t, out float3 pos, out float3 tan, out upVector);
            position = pos;
            tangent = tan;
            up = upVector;

            // Transform to world space
            position = _splineContainer.transform.TransformPoint(position);
            tangent = _splineContainer.transform.TransformDirection(tangent);
            up = _splineContainer.transform.TransformDirection(up);

            return true;
        }

        /// <summary>
        /// Find the closest point on the path to a world position
        /// </summary>
        public float FindClosestDistance(Vector3 worldPosition)
        {
            if (!_isInitialized) return 0f;

            // Convert world position to local space for spline calculation
            Vector3 localPosition = _splineContainer.transform.InverseTransformPoint(worldPosition);

            // Use Unity's optimized GetNearestPoint method
            SplineUtility.GetNearestPoint(_spline, localPosition, out float3 nearest, out float t);

            // Convert parameter t (0-1) to distance along spline
            return t * _totalLength;
        }

        private void OnDrawGizmosSelected()
        {
            if (_splineContainer == null || _splineContainer.Spline == null) return;

            // Draw spline path
            Gizmos.color = Color.green;
            const int segments = 50;
            for (int i = 0; i < segments; i++)
            {
                float t1 = (float)i / segments;
                float t2 = (float)(i + 1) / segments;

                float3 up1, up2;
                _splineContainer.Spline.Evaluate(t1, out float3 p1, out float3 _, out up1);
                _splineContainer.Spline.Evaluate(t2, out float3 p2, out float3 _, out up2);
                Vector3 p1Vec = p1;
                Vector3 p2Vec = p2;

                p1Vec = _splineContainer.transform.TransformPoint(p1Vec);
                p2Vec = _splineContainer.transform.TransformPoint(p2Vec);

                Gizmos.DrawLine(p1Vec, p2Vec);
            }
        }
    }
}