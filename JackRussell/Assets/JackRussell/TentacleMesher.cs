using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(SplineContainer))]
public class TentacleMesher : MonoBehaviour
{
    [Header("Mesh Settings")]
    public float radius = 0.2f;
    public int radialSegments = 8;      // 8 is efficient, 12+ is smoother
    public int lengthSegmentsPerUnit = 4; 
    public bool taperTip = true;        // Pointy end?

    [Header("Texture Settings")]
    public float textureTiling = 1.0f;  // Higher = more repeats

    private SplineContainer splineContainer;
    private MeshFilter meshFilter;
    private Mesh mesh;

    // Cache lists to avoid Garbage Collection
    private List<Vector3> verts = new List<Vector3>();
    private List<Vector3> normals = new List<Vector3>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<int> tris = new List<int>();

    void Awake()
    {
        splineContainer = GetComponent<SplineContainer>();
        meshFilter = GetComponent<MeshFilter>();
        
        mesh = new Mesh();
        mesh.name = "TentacleProceduralMesh";
        mesh.MarkDynamic(); // Important for performance when updating every frame
        meshFilter.mesh = mesh;
    }

    void LateUpdate()
    {
        GenerateMesh();
    }

    private void GenerateMesh()
    {
        Spline spline = splineContainer.Spline;
        float splineLength = spline.GetLength();

        // Avoid errors if length is zero
        if (splineLength < 0.01f) return;

        verts.Clear();
        normals.Clear();
        uvs.Clear();
        tris.Clear();

        // Determine how many rings we need
        int rings = Mathf.Max(2, Mathf.CeilToInt(splineLength * lengthSegmentsPerUnit));

        for (int i = 0; i <= rings; i++)
        {
            float t = (float)i / rings; // 0.0 to 1.0
            
            // Get data from Spline
            Vector3 pos = spline.EvaluatePosition(t);
            Vector3 tan = spline.EvaluateTangent(t);
            Vector3 up = spline.EvaluateUpVector(t);

            // Calculate Rotation
            Quaternion rot = (tan != Vector3.zero && up != Vector3.zero) 
                ? Quaternion.LookRotation(tan, up) 
                : Quaternion.identity;

            // Calculate Radius (Tapering)
            float currentRadius = radius;
            if (taperTip) currentRadius *= Mathf.Lerp(1f, 0.2f, t);

            // Generate Circle Ring
            for (int j = 0; j <= radialSegments; j++)
            {
                float angle = (float)j / radialSegments * Mathf.PI * 2;
                
                float x = Mathf.Cos(angle) * currentRadius;
                float y = Mathf.Sin(angle) * currentRadius;

                Vector3 localCircleVert = new Vector3(x, y, 0);
                
                // Apply rotation and position
                Vector3 finalVert = pos + (rot * localCircleVert);

                verts.Add(finalVert);
                normals.Add((finalVert - pos).normalized);

                // UVs: X wraps around, Y tiles based on real-world length
                uvs.Add(new Vector2((float)j / radialSegments, t * splineLength * textureTiling));
            }
        }

        // Generate Triangles
        for (int i = 0; i < rings; i++)
        {
            for (int j = 0; j < radialSegments; j++)
            {
                int currentRing = i * (radialSegments + 1);
                int nextRing = (i + 1) * (radialSegments + 1);

                int a = currentRing + j;
                int b = nextRing + j;
                int c = nextRing + j + 1;
                int d = currentRing + j + 1;

                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(a); tris.Add(d); tris.Add(c);
            }
        }

        // Assign to Unity Mesh
        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
    }
}