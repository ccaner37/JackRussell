using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
public class TentacleSplineController : MonoBehaviour
{
    [Header("Assignments")]
    public Transform originTransform; // The exact anchor point on the back/neck
    public Transform targetTransform; // The object to reach

    [Header("Physics & Feel")]
    public Vector3 exitDirection = Vector3.back; // Direction "out" of the skin (usually -Z for back)
    
    [Tooltip("Stiffness of the base. 0.05 allows it to bend immediately.")]
    public float startTangentStrength = 0.05f;   

    [Header("Dynamic Arching")]
    [Tooltip("Curve when target is aligned with exit (e.g. Target is Behind).")]
    public float minCurveMultiplier = 0.05f;
    
    [Tooltip("Curve when target is opposite to exit (e.g. Target is Forward).")]
    public float maxCurveMultiplier = 0.5f;

    private SplineContainer splineContainer;

    void Start()
    {
        splineContainer = GetComponent<SplineContainer>();
        Spline spline = splineContainer.Spline;
        
        spline.Clear();
        spline.Add(new BezierKnot(Vector3.zero));
        spline.Add(new BezierKnot(Vector3.zero));
        spline.Add(new BezierKnot(Vector3.zero));
    }

    void LateUpdate()
    {
        if (originTransform == null || targetTransform == null) return;

        UpdateSplinePoints();
    }

    void UpdateSplinePoints()
    {
        Spline spline = splineContainer.Spline;

        // 1. Coordinate Space Setup
        Vector3 localStartPos = transform.InverseTransformPoint(originTransform.position);
        Vector3 localEndPos = transform.InverseTransformPoint(targetTransform.position);
        Quaternion localStartRot = Quaternion.Inverse(transform.rotation) * originTransform.rotation;

        // Calculate Vectors needed for Math
        Vector3 worldExitDir = originTransform.TransformDirection(exitDirection);
        Vector3 localExitDir = transform.InverseTransformDirection(worldExitDir);
        
        // Vector from Origin to Target
        Vector3 targetDir = (targetTransform.position - originTransform.position).normalized;
        float totalDistance = Vector3.Distance(localStartPos, localEndPos);


        // 2. DYNAMIC CURVE CALCULATION
        // Dot Product compares alignment. 
        // 1.0 = Target is exactly where Exit points (Straight shot)
        // -1.0 = Target is exactly opposite (Needs to wrap around)
        float alignmentDot = Vector3.Dot(worldExitDir, targetDir);
        
        // Map the Dot (-1 to 1) to a blend value (1 to 0)
        // If Dot is 1 (Aligned), t becomes 0. If Dot is -1 (Opposite), t becomes 1.
        float blendT = Mathf.InverseLerp(1.0f, -1.0f, alignmentDot);
        
        // Calculate the final multiplier based on the angle
        float dynamicCurveMultiplier = Mathf.Lerp(minCurveMultiplier, maxCurveMultiplier, blendT);


        // 3. Update Knots
        
        // --- KNOT 0: ANCHOR ---
        BezierKnot startKnot = spline[0];
        startKnot.Position = localStartPos;
        startKnot.Rotation = localStartRot;
        // Apply the tangent strength (0.05f as requested) scaled by distance
        startKnot.TangentOut = localExitDir * (startTangentStrength + (totalDistance * 0.1f)); 
        startKnot.TangentIn = Vector3.zero;
        spline.SetKnot(0, startKnot);
        spline.SetTangentMode(0, TangentMode.Broken);

        // --- KNOT 2: TARGET ---
        BezierKnot endKnot = spline[2];
        endKnot.Position = localEndPos;
        spline.SetKnot(2, endKnot);
        spline.SetTangentMode(2, TangentMode.AutoSmooth);

        // --- KNOT 1: DYNAMIC ELBOW ---
        Vector3 midBase = Vector3.Lerp(localStartPos, localEndPos, 0.5f);
        
        // Push the elbow out based on our new Dynamic Multiplier
        Vector3 dynamicOffset = localExitDir * (totalDistance * dynamicCurveMultiplier);
        
        BezierKnot midKnot = spline[1];
        midKnot.Position = midBase + dynamicOffset;
        spline.SetKnot(1, midKnot);
        spline.SetTangentMode(1, TangentMode.AutoSmooth);
    }
}