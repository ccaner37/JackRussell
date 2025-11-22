using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(SplineContainer))]
public class TentacleMesher : MonoBehaviour
{
    [Header("Mesh Settings")]
    public float radius = 0.2f;
    public int radialSegments = 8;
    public int lengthSegmentsPerUnit = 4; 
    public bool taperTip = true;

    [Header("Animation (Talking)")]
    [Range(0, 1)] 
    [Tooltip("0 = Closed Point, 1 = Wide Open Mouth")]
    public float mouthOpen = 0f;

    [Header("Texture Settings")]
    public float textureTiling = 1.0f;

    private SplineContainer splineContainer;
    private MeshFilter meshFilter;
    private Mesh mesh;

    // Cache lists to avoid Garbage Collection
    private List<Vector3> verts = new List<Vector3>();
    private List<Vector3> normals = new List<Vector3>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<int> tris = new List<int>();

    // Allow external control (prevents double updates)
    public bool autoUpdate = true; 

    void Awake()
    {
        splineContainer = GetComponent<SplineContainer>();
        meshFilter = GetComponent<MeshFilter>();
        
        mesh = new Mesh();
        mesh.name = "TentacleProceduralMesh";
        mesh.MarkDynamic();
        meshFilter.mesh = mesh;
    }

    void LateUpdate()
    {
        if (autoUpdate) GenerateMesh();
    }

    public void GenerateMesh()
    {
        if (splineContainer == null) splineContainer = GetComponent<SplineContainer>();
        
        Spline spline = splineContainer.Spline;
        float splineLength = spline.GetLength();

        if (splineLength < 0.01f) return;

        verts.Clear();
        normals.Clear();
        uvs.Clear();
        tris.Clear();

        int rings = Mathf.Max(2, Mathf.CeilToInt(splineLength * lengthSegmentsPerUnit));

        for (int i = 0; i <= rings; i++)
        {
            float t = (float)i / rings; // 0.0 (Base) to 1.0 (Tip)
            
            Vector3 pos = spline.EvaluatePosition(t);
            Vector3 tan = spline.EvaluateTangent(t);
            Vector3 up = spline.EvaluateUpVector(t);

            Quaternion rot = (tan != Vector3.zero && up != Vector3.zero) 
                ? Quaternion.LookRotation(tan, up) 
                : Quaternion.identity;

            // --- VISEME / MOUTH LOGIC ---
            float currentRadius = radius;

            if (taperTip) 
            {
                // Define Tip Shape
                float closedScale = 0.0f; // Sharp point
                float openScale = 1.5f;   // Flared mouth (larger than body)

                // Blend based on mouthOpen parameter
                float tipTargetScale = Mathf.Lerp(closedScale, openScale, mouthOpen);

                // Cubic Lerp (t*t*t) keeps the body thick and affects only the very end
                float taperFactor = Mathf.Lerp(1f, tipTargetScale, t * t * t);
                currentRadius *= taperFactor;
            }

            for (int j = 0; j <= radialSegments; j++)
            {
                float angle = (float)j / radialSegments * Mathf.PI * 2;
                float x = Mathf.Cos(angle) * currentRadius;
                float y = Mathf.Sin(angle) * currentRadius;

                // Optional: If you want "Lip" shapes later, modify X and Y differently here
                // e.g. if (mouthOpen > 0.5) y *= 0.5f; // Oval shape

                Vector3 localCircleVert = new Vector3(x, y, 0);
                Vector3 finalVert = pos + (rot * localCircleVert);

                verts.Add(finalVert);
                normals.Add((finalVert - pos).normalized);
                uvs.Add(new Vector2((float)j / radialSegments, t * splineLength * textureTiling));
            }
        }

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

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
    }
}