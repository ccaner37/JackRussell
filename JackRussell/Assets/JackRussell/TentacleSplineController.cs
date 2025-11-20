using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
public class TentacleSplineController : MonoBehaviour
{
    [Header("Assignments")]
    public Transform originTransform; // Assign Player's back/shoulder here
    public Transform targetTransform; // Assign the object you want to follow

    [Header("Curve Settings")]
    public float curveOffset = 1.5f; // How much the tentacle "hangs" or curves in the middle

    private SplineContainer splineContainer;

    void Start()
    {
        splineContainer = GetComponent<SplineContainer>();
        
        // Initialize Spline with exactly 3 Knots (Start, Mid, End)
        Spline spline = splineContainer.Spline;
        spline.Clear();
        spline.Add(new BezierKnot(Vector3.zero));
        spline.Add(new BezierKnot(Vector3.zero));
        spline.Add(new BezierKnot(Vector3.zero));
    }

    void Update()
    {
        if (originTransform == null || targetTransform == null) return;

        UpdateSplinePoints();
    }

    void UpdateSplinePoints()
    {
        Spline spline = splineContainer.Spline;

        // 1. Convert World positions to Local Space of this container
        // This ensures the math works even if the container object itself moves or rotates
        Vector3 localStart = transform.InverseTransformPoint(originTransform.position);
        Vector3 localEnd = transform.InverseTransformPoint(targetTransform.position);

        // 2. Calculate Middle Point
        // Simple math: Halfway between start and end
        Vector3 localMid = Vector3.Lerp(localStart, localEnd, 0.5f);
        
        // Add "gravity" or curve offset to the middle point
        // We use Vector3.down relative to world space, converted to local
        Vector3 gravityDir = transform.InverseTransformDirection(Vector3.down);
        localMid += gravityDir * curveOffset;

        // 3. Update the Knots
        
        // START
        BezierKnot startKnot = spline[0];
        startKnot.Position = localStart;
        spline[0] = startKnot;

        // MID
        BezierKnot midKnot = spline[1];
        midKnot.Position = localMid;
        spline[1] = midKnot;

        // END
        BezierKnot endKnot = spline[2];
        endKnot.Position = localEnd;
        spline[2] = endKnot;

        // 4. Smoothing
        // AutoSmooth calculates the handles for us so it looks like a rope, not a zigzag
        spline.SetTangentMode(TangentMode.AutoSmooth);
    }
}