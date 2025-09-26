using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;

namespace JackRussell.Rails
{
    /// <summary>
    /// Generates a visual mesh for spline rails in the editor and at runtime.
    /// Creates a tube-like mesh that follows the spline path.
    /// </summary>
    [RequireComponent(typeof(SplineContainer))]
    [ExecuteInEditMode]
    public class RailMeshGenerator : MonoBehaviour
    {
        [Header("Mesh Settings")]
        [SerializeField] private float _radius = 0.1f;
        [SerializeField] private int _radialSegments = 8;
        [SerializeField] private int _lengthSegments = 20;
        [SerializeField] private Material _railMaterial;
        [SerializeField] private bool _generateMesh = true;
        [SerializeField] private bool _updateInPlayMode = false;

        [Header("Mesh Optimization")]
        [SerializeField] private bool _generateCollider = true;
        [SerializeField] private PhysicsMaterial _physicsMaterial;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private SplineContainer _splineContainer;
        private Mesh _generatedMesh;


        private void InitializeComponents()
        {
            _splineContainer = GetComponent<SplineContainer>();
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();

            if (_meshFilter == null)
            {
                _meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            if (_meshRenderer == null)
            {
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();
                if (_railMaterial != null)
                {
                    _meshRenderer.material = _railMaterial;
                }
            }

            if (_generateCollider && _meshCollider == null)
            {
                _meshCollider = gameObject.AddComponent<MeshCollider>();
                if (_physicsMaterial != null)
                {
                    _meshCollider.material = _physicsMaterial;
                }
            }
        }

        /// <summary>
        /// Generate the rail mesh by creating a tube along the spline
        /// </summary>
        [ContextMenu("Generate Rail Mesh")]
        public void GenerateRailMesh()
        {
            InitializeComponents();
            if (!_generateMesh || _splineContainer == null || _splineContainer.Spline == null) return;

            var spline = _splineContainer.Spline;
            if (spline.Count < 2) return;

            if (_generatedMesh == null)
            {
                _generatedMesh = new Mesh();
                _generatedMesh.name = "RailMesh";
            }
            else
            {
                _generatedMesh.Clear();
            }

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            // Generate vertices along the spline
            for (int i = 0; i <= _lengthSegments; i++)
            {
                float t = (float)i / _lengthSegments;

                // Get position and tangent from spline
                float3 upVecFloat3;
                spline.Evaluate(t, out float3 position, out float3 tangent, out upVecFloat3);

                Vector3 pos = position;
                Vector3 tan = tangent;
                Vector3 upVec = upVecFloat3;

                // Create a coordinate system perpendicular to the tangent
                Vector3 right = Vector3.Cross(tan, upVec).normalized;
                Vector3 actualUp = Vector3.Cross(right, tan).normalized;

                // Generate vertices around the circumference
                for (int j = 0; j < _radialSegments; j++)
                {
                    float angle = (float)j / _radialSegments * Mathf.PI * 2f;
                    Vector3 offset = (Mathf.Cos(angle) * right + Mathf.Sin(angle) * actualUp) * _radius;

                    vertices.Add(pos + offset);
                    normals.Add(offset.normalized);
                    uvs.Add(new Vector2(t, (float)j / _radialSegments));
                }
            }

            // Generate triangles
            for (int i = 0; i < _lengthSegments; i++)
            {
                for (int j = 0; j < _radialSegments; j++)
                {
                    int current = i * _radialSegments + j;
                    int next = i * _radialSegments + (j + 1) % _radialSegments;
                    int nextRing = (i + 1) * _radialSegments + j;
                    int nextRingNext = (i + 1) * _radialSegments + (j + 1) % _radialSegments;

                    // First triangle
                    triangles.Add(current);
                    triangles.Add(next);
                    triangles.Add(nextRing);

                    // Second triangle
                    triangles.Add(next);
                    triangles.Add(nextRingNext);
                    triangles.Add(nextRing);
                }
            }

            // Assign mesh data
            _generatedMesh.SetVertices(vertices);
            _generatedMesh.SetTriangles(triangles, 0);
            _generatedMesh.SetNormals(normals);
            _generatedMesh.SetUVs(0, uvs);

            _generatedMesh.RecalculateBounds();

            if (_meshFilter != null)
            {
                _meshFilter.mesh = _generatedMesh;
            }

            if (_generateCollider && _meshCollider != null)
            {
                _meshCollider.sharedMesh = _generatedMesh;
            }
        }

        /// <summary>
        /// Clean up generated mesh
        /// </summary>
        private void OnDestroy()
        {
            if (_generatedMesh != null && !Application.isPlaying)
            {
                DestroyImmediate(_generatedMesh);
            }
        }

        /// <summary>
        /// Draw debug information in editor
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (_splineContainer == null || _splineContainer.Spline == null) return;

            var spline = _splineContainer.Spline;
            if (spline.Count < 2) return;

            Gizmos.color = Color.blue;

            // Draw the rail path
            for (int i = 0; i < _lengthSegments; i++)
            {
                float t1 = (float)i / _lengthSegments;
                float t2 = (float)(i + 1) / _lengthSegments;

                float3 up1, up2;
                spline.Evaluate(t1, out float3 p1, out float3 _, out up1);
                spline.Evaluate(t2, out float3 p2, out float3 _, out up2);

                Vector3 worldP1 = transform.TransformPoint(p1);
                Vector3 worldP2 = transform.TransformPoint(p2);

                Gizmos.DrawLine(worldP1, worldP2);
            }

            // Draw radius indicators at key points
            Gizmos.color = Color.green;
            for (int i = 0; i <= 4; i++)
            {
                float t = (float)i / 4f;
                spline.Evaluate(t, out float3 pos, out float3 _, out float3 _);
                Vector3 worldPos = transform.TransformPoint(pos);
                Gizmos.DrawWireSphere(worldPos, _radius);
            }
        }
    }
}