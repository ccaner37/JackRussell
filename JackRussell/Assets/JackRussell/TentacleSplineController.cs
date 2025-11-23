using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

[RequireComponent(typeof(SplineContainer))]
public class TentacleSplineController : MonoBehaviour
{
    [Header("Simulation Settings")]
    public int boneCount = 14;
    public float idleLength = 4.0f; 
    public int physicsIterations = 10; 

    [Header("Attachments")]
    public Transform tipTracker;

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
    [Tooltip("Time in seconds to reach the target.")]
    public float shootDuration = 0.15f; 
    public Transform originTransform; 
    public Transform targetTransform; 

    private class Particle
    {
        public Vector3 position;
        public Vector3 prevPosition;
    }

    private SplineContainer splineContainer;
    private TentacleMesher mesher; 
    private List<Particle> particles = new List<Particle>();
    private bool isInitialized = false;

    // Grapple State Internals
    private bool wasGrappling = false;
    private bool isShooting = false;
    
    private Vector3 shootTipPos; 
    private Vector3 shootStartPos; 
    private float shootTimer = 0f; 

    void Start()
    {
        InitParticles();
        
        mesher = GetComponent<TentacleMesher>();
        if (mesher != null) mesher.autoUpdate = false;
    }

    void InitParticles()
    {
        splineContainer = GetComponent<SplineContainer>();
        particles.Clear();

        Vector3 startPos = originTransform != null ? originTransform.position : transform.position;
        Vector3 dir = originTransform != null ? originTransform.TransformDirection(exitDirection) : -transform.forward;
        float segmentLen = idleLength / (boneCount - 1);

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
        if (dt > 0.05f) dt = 0.05f; 

        // --- STATE MACHINE ---

        // 1. DETECT GRAPPLE TRIGGER
        if (isGrappling && !wasGrappling)
        {
            // Start Shooting!
            isShooting = true;
            shootTimer = 0f;
            shootStartPos = originTransform.position; 
            shootTipPos = shootStartPos;
        }
        else if (!isGrappling && wasGrappling)
        {
            // Stop Shooting
            isShooting = false;
            shootTimer = 0f;
        }
        wasGrappling = isGrappling;

        // 2. DETERMINE TARGET POINTS & LENGTH
        Vector3 startPoint = originTransform.position;
        Vector3 endPoint;
        float targetTotalLength;

        if (isShooting && targetTransform != null)
        {
            // SHOOTING STATE
            shootTimer += dt;
            float t = Mathf.Clamp01(shootTimer / shootDuration);
            
            shootTipPos = Vector3.Lerp(shootStartPos, targetTransform.position, t);
            endPoint = shootTipPos;
            
            targetTotalLength = Vector3.Distance(startPoint, endPoint);
            
            if (t >= 1.0f) isShooting = false;
        }
        else if (isGrappling && targetTransform != null)
        {
            // GRAPPLED STATE
            endPoint = targetTransform.position;
            targetTotalLength = Vector3.Distance(startPoint, endPoint);
        }
        else
        {
            // IDLE STATE
            endPoint = originTransform.TransformPoint(idleRestOffset);
            targetTotalLength = idleLength; 
        }

        if (targetTotalLength < 0.1f) targetTotalLength = 0.1f;
        float currentSegmentLen = targetTotalLength / (boneCount - 1);


        // 3. CURVE CALCULATION
        Vector3 worldExitDir = originTransform.TransformDirection(exitDirection);
        Vector3 targetDir = (endPoint - startPoint).normalized;
        float distToTarget = Vector3.Distance(startPoint, endPoint);
        
        float alignment = Vector3.Dot(worldExitDir, targetDir);
        float blend = Mathf.InverseLerp(1.0f, -1.0f, alignment);
        float curveHeight = Mathf.Lerp(minCurveMultiplier, maxCurveMultiplier, blend);
        
        if (isGrappling || isShooting) curveHeight *= 0.2f; 

        Vector3 midBase = Vector3.Lerp(startPoint, endPoint, 0.5f);
        Vector3 midPoint = midBase + (worldExitDir * (distToTarget * curveHeight));


        // 4. PHYSICS LOOP
        particles[0].position = startPoint;
        particles[0].prevPosition = startPoint;

        for (int i = 1; i < particles.Count; i++)
        {
            Particle p = particles[i];

            Vector3 velocity = p.position - p.prevPosition;
            p.prevPosition = p.position;
            
            p.position += velocity * (1.0f - damping);
            p.position += gravity * (dt * dt);

            // Pose Matching
            float t = (float)i / (boneCount - 1);
            Vector3 idealPos = GetQuadraticBezier(startPoint, midPoint, endPoint, t);
            float poseStrength = Mathf.Lerp(baseStiffness, tipStiffness, t * t);
            
            Vector3 pull = idealPos - p.position;
            p.position += pull * (poseStrength * dt * 10f); 
        }

        // Tip Locking
        if (isGrappling || isShooting)
        {
            Particle tip = particles[particles.Count - 1];
            tip.position = endPoint;
            tip.prevPosition = endPoint; 
        }

        // Constraints
        for (int k = 0; k < physicsIterations; k++)
        {
            // Forward
            for (int i = 1; i < particles.Count; i++)
                ConstraintParticles(particles[i - 1], particles[i], currentSegmentLen);
            
            // Backward & Grapple Locking
            if (isGrappling || isShooting)
            {
                for (int i = particles.Count - 1; i > 0; i--)
                    ConstraintParticles(particles[i - 1], particles[i], currentSegmentLen);
                
                // Lock Tip
                particles[particles.Count - 1].position = endPoint;
            }
            
            // FIX: Lock Root (MUST happen every iteration, regardless of state)
            particles[0].position = startPoint;
        }


        // 5. RENDER & UPDATE
        UpdateSplineToParticles(startPoint, worldExitDir, currentSegmentLen);
        
        if (mesher != null) mesher.GenerateMesh();

        if (tipTracker != null)
        {
            tipTracker.position = particles[particles.Count - 1].position;
            Vector3 tipTangent = splineContainer.Spline.EvaluateTangent(1f);
            if (tipTangent != Vector3.zero)
                tipTracker.rotation = Quaternion.LookRotation(tipTangent, Vector3.up);
        }
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
                Quaternion localRot = Quaternion.Inverse(transform.rotation) * originTransform.rotation;
                knot.Rotation = localRot;
                
                Vector3 localExitDir = transform.InverseTransformDirection(startDir);
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