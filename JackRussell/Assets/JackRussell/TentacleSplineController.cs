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
    public float startTangentStrength = 2.0f;    // How stiff the base is (higher = straighter at start)
    public float curveMultiplier = 0.5f;         // How wide the arc swings based on distance

    private SplineContainer splineContainer;

    void Start()
    {
        splineContainer = GetComponent<SplineContainer>();
        Spline spline = splineContainer.Spline;
        
        spline.Clear();
        // We need 3 knots: Start (Anchor), Mid (Curve Control), End (Target)
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

        // 1. Space Conversion
        // Convert everything to the SplineContainer's local space so the math holds up if the player moves/rotates.
        Vector3 localStartPos = transform.InverseTransformPoint(originTransform.position);
        Vector3 localEndPos = transform.InverseTransformPoint(targetTransform.position);
        
        // Get the Local Rotation of the origin to lock the mesh orientation
        Quaternion localStartRot = Quaternion.Inverse(transform.rotation) * originTransform.rotation;

        // Calculate the local "Exit Vector" (The direction the tentacle shoots out from the skin)
        // We transform the logic vector (e.g., Back) by the origin's rotation
        Vector3 worldExitDir = originTransform.TransformDirection(exitDirection);
        Vector3 localExitDir = transform.InverseTransformDirection(worldExitDir);

        float distance = Vector3.Distance(localStartPos, localEndPos);

        // --- KNOT 0: ANCHOR ---
        BezierKnot startKnot = spline[0];
        startKnot.Position = localStartPos;
        startKnot.Rotation = localStartRot; // LOCK ROTATION to skin
        
        // Force the tangent to shoot straight out
        // TangentOut controls the shape leaving the knot. 
        // We multiply by distance/strength to make it scale with the tentacle length.
        startKnot.TangentOut = localExitDir * (startTangentStrength + (distance * 0.3f));
        startKnot.TangentIn = Vector3.zero; // Nothing comes before the start
        spline.SetKnot(0, startKnot);
        spline.SetTangentMode(0, TangentMode.Broken); // 'Broken' allows us to set Out without In affecting it


        // --- KNOT 2: TARGET ---
        BezierKnot endKnot = spline[2];
        endKnot.Position = localEndPos;
        // Optional: Orient the tip to face the impact? 
        // For now, we leave rotation auto or zero to let the tube end naturally.
        spline.SetKnot(2, endKnot);
        spline.SetTangentMode(2, TangentMode.AutoSmooth); // Let the end be smooth


        // --- KNOT 1: DYNAMIC ELBOW ---
        // Calculate a natural "mid point" that creates an arc
        Vector3 midBase = Vector3.Lerp(localStartPos, localEndPos, 0.5f);
        
        // The Magic: Push the mid-point OUT in the direction of the exit vector.
        // This creates the "Arch" that prevents it from looking like a straight line.
        Vector3 dynamicOffset = localExitDir * (distance * curveMultiplier);
        
        BezierKnot midKnot = spline[1];
        midKnot.Position = midBase + dynamicOffset;
        spline.SetKnot(1, midKnot);
        spline.SetTangentMode(1, TangentMode.AutoSmooth); // Smooth the curve through the elbow
    }
}