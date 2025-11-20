using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

[RequireComponent(typeof(SplineContainer))]
public class TentacleSplineController : MonoBehaviour
{
    [Header("Simulation Settings")]
    public int boneCount = 14;
    public float restLength = 4.0f;
    [Tooltip("Higher = Stiffer rope, less jitter. Lower = Faster but unstable.")]
    public int physicsIterations = 10; 

    [Header("Physics Feel")]
    [Range(0, 1)] public float damping = 0.2f;   
    public Vector3 gravity = new Vector3(0, -9.81f, 0);

    [Header("Stiffness Gradient")]
    [Range(0, 1)] public float baseStiffness = 0.85f; 
    [Range(0, 1)] public float tipStiffness = 0.05f;

    [Header("Shape Control")]
    public Vector3 exitDirection = Vector3.back; 
    public Vector3 idleRestOffset = new Vector3(0, -2f, -1.5f); 
    
    [Space]
    public float maxCurveMultiplier = 0.6f;
    public float minCurveMultiplier = 0.1f;

    [Header("Grapple Settings")]
    public bool isGrappling = false;
    public Transform originTransform; 
    public Transform targetTransform; 

    // Inner class for Verlet
    private class Particle
    {
        public Vector3 position;
        public Vector3 prevPosition;
    }

    private SplineContainer splineContainer;
    private TentacleMesher mesher; // Reference to mesher
    private List<Particle> particles = new List<Particle>();
    private bool isInitialized = false;

    void Start()
    {
        InitParticles();
        
        // Disable auto-update on mesher so we can drive it manually
        mesher = GetComponent<TentacleMesher>();
        if (mesher != null) mesher.autoUpdate = false;
    }

    void InitParticles()
    {
        splineContainer = GetComponent<SplineContainer>();
        particles.Clear();

        Vector3 startPos = originTransform != null ? originTransform.position : transform.position;
        Vector3 dir = originTransform != null ? originTransform.TransformDirection(exitDirection) : -transform.forward;
        float segmentLen = restLength / (boneCount - 1);

        for (int i = 0; i < boneCount; i++)
        {
            Particle p = new Particle();
            p.position = startPos + (dir * segmentLen * i);
            p.prevPosition = p.position;
            particles.Add(p);
        }

        isInitialized = true;
    }

    void LateUpdate()
    {
        if (!isInitialized || originTransform == null) return;

        float dt = Time.deltaTime;
        // Prevent explosion if frame rate drops too low
        if (dt > 0.05f) dt = 0.05f; 

        // 1. CALCULATE IDEAL CURVE
        Vector3 startPoint = originTransform.position;
        Vector3 endPoint = (isGrappling && targetTransform != null) 
            ? targetTransform.position 
            : originTransform.TransformPoint(idleRestOffset);

        Vector3 worldExitDir = originTransform.TransformDirection(exitDirection);
        Vector3 targetDir = (endPoint - startPoint).normalized;
        float distToTarget = Vector3.Distance(startPoint, endPoint);
        
        float alignment = Vector3.Dot(worldExitDir, targetDir);
        float blend = Mathf.InverseLerp(1.0f, -1.0f, alignment);
        float curveHeight = Mathf.Lerp(minCurveMultiplier, maxCurveMultiplier, blend);
        
        Vector3 midBase = Vector3.Lerp(startPoint, endPoint, 0.5f);
        Vector3 midPoint = midBase + (worldExitDir * (distToTarget * curveHeight));

        // 2. PHYSICS LOOP
        
        // A. Lock Root (Essential for "Skin Connection")
        particles[0].position = startPoint;
        particles[0].prevPosition = startPoint; // Kill root velocity

        float currentSegmentLen = isGrappling && distToTarget > restLength 
            ? distToTarget / (boneCount - 1) 
            : restLength / (boneCount - 1);

        for (int i = 1; i < particles.Count; i++)
        {
            Particle p = particles[i];

            // Verlet Integration
            Vector3 velocity = p.position - p.prevPosition;
            p.prevPosition = p.position;
            
            // Apply Damping
            p.position += velocity * (1.0f - damping);
            // Apply Gravity
            p.position += gravity * (dt * dt);

            // Apply Pose Stiffness Force
            float t = (float)i / (boneCount - 1);
            Vector3 idealPos = GetQuadraticBezier(startPoint, midPoint, endPoint, t);
            float poseStrength = Mathf.Lerp(baseStiffness, tipStiffness, t * t);
            
            // We clamp the force to avoid jitter if it pulls too hard
            Vector3 pull = idealPos - p.position;
            p.position += pull * (poseStrength * dt * 10f); 
        }

        // B. Lock Tip (Grapple)
        if (isGrappling && targetTransform != null)
        {
            Particle tip = particles[particles.Count - 1];
            tip.position = endPoint;
            tip.prevPosition = endPoint; 
        }

        // C. Constraints (Iterate multiple times for stability)
        for (int k = 0; k < physicsIterations; k++)
        {
            // Forward Pass
            for (int i = 1; i < particles.Count; i++)
                ConstraintParticles(particles[i - 1], particles[i], currentSegmentLen);
            
            // Backward Pass (Helps propagate tension from tip)
            if (isGrappling)
            {
                for (int i = particles.Count - 1; i > 0; i--)
                    ConstraintParticles(particles[i - 1], particles[i], currentSegmentLen);
            }
            
            // Re-Lock Root (Crucial: Constraints might have pulled it away)
            particles[0].position = startPoint;
            if (isGrappling) particles[particles.Count - 1].position = endPoint;
        }

        // 3. RENDER
        UpdateSplineToParticles(startPoint, worldExitDir, currentSegmentLen);
        
        // 4. FORCE MESH UPDATE NOW (Fixes Lag)
        if (mesher != null) mesher.GenerateMesh();
    }

    Vector3 GetQuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1 - t;
        return (u * u * p0) + (2 * u * t * p1) + (t * t * p2);
    }

    void ConstraintParticles(Particle p1, Particle p2, float segmentLen)
    {
        Vector3 direction = p2.position - p1.position;
        float currentDist = direction.magnitude;
        if (currentDist < 0.001f) return;
        
        float difference = (currentDist - segmentLen) / currentDist;
        Vector3 correction = direction * difference * 0.5f;
        
        // We don't move p1 if it's the root (index check implied by loop order, but safer to assume p1 is closer to root)
        // However, the 'Lock Root' step handles the root explicitly.
        p1.position += correction;
        p2.position -= correction;
    }

    void UpdateSplineToParticles(Vector3 startPos, Vector3 startDir, float segmentLen)
    {
        Spline spline = splineContainer.Spline;
        spline.Clear();

        for (int i = 0; i < particles.Count; i++)
        {
            Vector3 localPos = transform.InverseTransformPoint(particles[i].position);
            BezierKnot knot = new BezierKnot(localPos);

            if (i == 0)
            {
                // Fix Tangent: Use Segment Length, not Total Length
                Quaternion localRot = Quaternion.Inverse(transform.rotation) * originTransform.rotation;
                knot.Rotation = localRot;
                
                Vector3 localExitDir = transform.InverseTransformDirection(startDir);
                // This prevents the "Broken Mesh" / Huge Loop issue
                knot.TangentOut = localExitDir * (segmentLen * 1.5f); 
                knot.TangentIn = Vector3.zero;
                
                spline.Add(knot);
                spline.SetTangentMode(0, TangentMode.Broken); 
            }
            else
            {
                spline.Add(knot);
                spline.SetTangentMode(i, TangentMode.AutoSmooth);
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (particles == null || particles.Count == 0) return;
        Gizmos.color = Color.green;
        for (int i = 0; i < particles.Count - 1; i++)
            Gizmos.DrawLine(particles[i].position, particles[i + 1].position);
    }
}