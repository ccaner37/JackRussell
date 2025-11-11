using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;

namespace JackRussell.Rails
{
    /// <summary>
    /// Deforms a custom mesh along a spline path while preserving texture coordinates.
    /// The source mesh should be oriented along the Z-axis (forward) and centered on the origin.
    /// </summary>
    [RequireComponent(typeof(SplineContainer))]
    [ExecuteInEditMode]
    public class RailMeshGenerator : MonoBehaviour
    {
        [Header("Source Mesh")]
        [Tooltip("The straight rail mesh to deform along the spline (should be oriented along Z-axis)")]
        [SerializeField] private Mesh _sourceMesh;
        [SerializeField] private Material _railMaterial;
        
        [Header("Deformation Settings")]
        [Tooltip("Number of times to repeat the mesh along the spline")]
        [SerializeField] private int _repetitions = 1;
        [Tooltip("Scale factor applied to the mesh")]
        [SerializeField] private Vector3 _meshScale = Vector3.one;
        [SerializeField] private bool _generateMesh = true;
        [SerializeField] private bool _updateInPlayMode = false;

        [Header("Rendering")]
        [Tooltip("Generate backfaces to make mesh visible from inside")]
        [SerializeField] private bool _doubleSided = true;
        
        [Header("Mesh Optimization")]
        [SerializeField] private bool _generateCollider = true;
        [SerializeField] private PhysicsMaterial _physicsMaterial;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private SplineContainer _splineContainer;
        private Mesh _generatedMesh;

        // Cache for source mesh data
        private Vector3[] _sourceVertices;
        private int[] _sourceTriangles;
        private Vector3[] _sourceNormals;
        private Vector2[] _sourceUVs;
        private float _sourceMeshLength;

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

        private void CacheSourceMesh()
        {
            if (_sourceMesh == null) return;

            _sourceVertices = _sourceMesh.vertices;
            _sourceTriangles = _sourceMesh.triangles;
            _sourceNormals = _sourceMesh.normals;
            _sourceUVs = _sourceMesh.uv;

            // Calculate source mesh length (assumes mesh is oriented along Z-axis)
            Bounds bounds = _sourceMesh.bounds;
            _sourceMeshLength = bounds.size.z;
        }

        /// <summary>
        /// Generate the deformed rail mesh along the spline
        /// </summary>
        [ContextMenu("Generate Deformed Rail Mesh")]
        public void GenerateDeformedMesh()
        {
            InitializeComponents();
            
            if (!_generateMesh || _sourceMesh == null || _splineContainer == null || _splineContainer.Spline == null)
            {
                Debug.LogWarning("Cannot generate mesh: missing source mesh or spline");
                return;
            }

            var spline = _splineContainer.Spline;
            if (spline.Count < 2) return;

            CacheSourceMesh();

            if (_generatedMesh == null)
            {
                _generatedMesh = new Mesh();
                _generatedMesh.name = "DeformedRailMesh";
            }
            else
            {
                _generatedMesh.Clear();
            }

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            float splineLength = spline.GetLength();
            Bounds sourceBounds = _sourceMesh.bounds;
            
            // Deform each vertex of the source mesh
            for (int rep = 0; rep < _repetitions; rep++)
            {
                int vertexOffset = vertices.Count;

                for (int i = 0; i < _sourceVertices.Length; i++)
                {
                    Vector3 sourceVert = _sourceVertices[i];
                    
                    // Apply mesh scale
                    sourceVert.x *= _meshScale.x;
                    sourceVert.y *= _meshScale.y;
                    sourceVert.z *= _meshScale.z;

                    // Calculate normalized position along the mesh (0 to 1)
                    float localT = (sourceVert.z - sourceBounds.min.z) / _sourceMeshLength;
                    
                    // Map to spline position accounting for repetitions
                    float splineT = (rep + localT) / _repetitions;
                    splineT = Mathf.Clamp01(splineT);

                    // Get spline data at this position
                    spline.Evaluate(splineT, out float3 position, out float3 tangent, out float3 upVector);

                    Vector3 pos = position;
                    Vector3 tan = math.normalize(tangent);
                    Vector3 up = math.normalize(upVector);

                    // Create coordinate system at this point on spline
                    Vector3 right = Vector3.Cross(tan, up).normalized;
                    Vector3 actualUp = Vector3.Cross(right, tan).normalized;

                    // Transform the vertex to spline space
                    Vector3 offset = right * sourceVert.x + actualUp * sourceVert.y;
                    Vector3 deformedVertex = pos + offset;

                    vertices.Add(deformedVertex);

                    // Transform normal
                    Vector3 sourceNormal = _sourceNormals[i];
                    Vector3 transformedNormal = (right * sourceNormal.x + actualUp * sourceNormal.y + tan * sourceNormal.z).normalized;
                    normals.Add(transformedNormal);

                    // Preserve UVs
                    uvs.Add(_sourceUVs[i]);
                }

                // Add triangles with offset
                for (int i = 0; i < _sourceTriangles.Length; i++)
                {
                    triangles.Add(_sourceTriangles[i] + vertexOffset);
                }

                // Add double-sided faces if enabled
                if (_doubleSided)
                {
                    for (int i = 0; i < _sourceTriangles.Length; i += 3)
                    {
                        // Add reversed triangles for backfaces
                        triangles.Add(_sourceTriangles[i + 2] + vertexOffset);
                        triangles.Add(_sourceTriangles[i + 1] + vertexOffset);
                        triangles.Add(_sourceTriangles[i] + vertexOffset);
                    }
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

            Debug.Log($"Generated deformed mesh with {vertices.Count} vertices");
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

            // Draw the spline path
            int segments = 50;
            for (int i = 0; i < segments; i++)
            {
                float t1 = (float)i / segments;
                float t2 = (float)(i + 1) / segments;

                spline.Evaluate(t1, out float3 p1, out float3 _, out float3 _);
                spline.Evaluate(t2, out float3 p2, out float3 _, out float3 _);

                Vector3 worldP1 = transform.TransformPoint(p1);
                Vector3 worldP2 = transform.TransformPoint(p2);

                Gizmos.DrawLine(worldP1, worldP2);
            }

            // Draw coordinate systems at key points
            Gizmos.color = Color.red;
            for (int i = 0; i <= 4; i++)
            {
                float t = (float)i / 4f;
                spline.Evaluate(t, out float3 pos, out float3 tangent, out float3 upVec);
                
                Vector3 worldPos = transform.TransformPoint(pos);
                Vector3 tan = math.normalize(tangent);
                Vector3 up = math.normalize(upVec);
                Vector3 right = Vector3.Cross(tan, up).normalized;

                Gizmos.color = Color.red;
                Gizmos.DrawRay(worldPos, right * 0.2f);
                Gizmos.color = Color.green;
                Gizmos.DrawRay(worldPos, up * 0.2f);
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(worldPos, tan * 0.2f);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_generateMesh && _sourceMesh != null && Application.isPlaying == _updateInPlayMode)
            {
                GenerateDeformedMesh();
            }
        }
#endif
    }
}