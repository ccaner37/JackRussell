using UnityEngine;
using UnityEditor;
using UnityEngine.Splines;
using Unity.Mathematics;
using JackRussell.Rails;
using JackRussell.States.Action;

namespace JackRussell.Editor
{
    /// <summary>
    /// Editor script that automatically creates rail end homing targets for SplineRail components.
    /// Adds two GameObjects at the start and end points of each rail with RailEndHomingTarget components.
    /// </summary>
    [CustomEditor(typeof(SplineRail))]
    public class RailEndHomingTargetCreator : UnityEditor.Editor
    {
        private const string StartTargetName = "RailStartHomingTarget";
        private const string EndTargetName = "RailEndHomingTarget";
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            SplineRail rail = (SplineRail)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rail End Homing Targets", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox("This will create two homing attack targets at the start and end of the rail.", MessageType.Info);
            
            if (GUILayout.Button("Create Rail End Homing Targets", GUILayout.Height(30)))
            {
                CreateRailEndTargets(rail);
            }
            
            if (GUILayout.Button("Remove Rail End Homing Targets", GUILayout.Height(25)))
            {
                RemoveRailEndTargets(rail);
            }
            
            if (GUILayout.Button("Update Target Positions", GUILayout.Height(25)))
            {
                UpdateTargetPositions(rail);
            }
        }
        
        /// <summary>
        /// Create start and end homing targets for the rail
        /// </summary>
        private void CreateRailEndTargets(SplineRail rail)
        {
            if (rail == null) return;
            
            // Check if targets already exist
            Transform startTarget = rail.transform.Find(StartTargetName);
            Transform endTarget = rail.transform.Find(EndTargetName);
            
            if (startTarget != null && endTarget != null)
            {
                Debug.Log($"[RailEndHomingTargetCreator] Targets already exist for rail: {rail.gameObject.name}");
                return;
            }
            
            // Create start target if it doesn't exist
            if (startTarget == null)
            {
                startTarget = CreateRailEndTarget(rail, StartTargetName, false);
                Debug.Log($"[RailEndHomingTargetCreator] Created start target for rail: {rail.gameObject.name}");
            }
            
            // Create end target if it doesn't exist
            if (endTarget == null)
            {
                endTarget = CreateRailEndTarget(rail, EndTargetName, true);
                Debug.Log($"[RailEndHomingTargetCreator] Created end target for rail: {rail.gameObject.name}");
            }
            
            // Update positions
            UpdateTargetPositions(rail);
            
            // Mark the rail as dirty for saving
            EditorUtility.SetDirty(rail);
        }
        
        /// <summary>
        /// Remove the start and end homing targets for the rail
        /// </summary>
        private void RemoveRailEndTargets(SplineRail rail)
        {
            if (rail == null) return;
            
            // Find and remove start target
            Transform startTarget = rail.transform.Find(StartTargetName);
            if (startTarget != null)
            {
                DestroyImmediate(startTarget.gameObject);
                Debug.Log($"[RailEndHomingTargetCreator] Removed start target from rail: {rail.gameObject.name}");
            }
            
            // Find and remove end target
            Transform endTarget = rail.transform.Find(EndTargetName);
            if (endTarget != null)
            {
                DestroyImmediate(endTarget.gameObject);
                Debug.Log($"[RailEndHomingTargetCreator] Removed end target from rail: {rail.gameObject.name}");
            }
            
            // Mark the rail as dirty for saving
            EditorUtility.SetDirty(rail);
        }
        
        /// <summary>
        /// Update the positions of existing targets
        /// </summary>
        private void UpdateTargetPositions(SplineRail rail)
        {
            if (rail == null) return;
            
            // Update start target
            Transform startTarget = rail.transform.Find(StartTargetName);
            if (startTarget != null)
            {
                UpdateTargetPosition(startTarget, rail, false);
            }
            
            // Update end target
            Transform endTarget = rail.transform.Find(EndTargetName);
            if (endTarget != null)
            {
                UpdateTargetPosition(endTarget, rail, true);
            }
        }
        
        /// <summary>
        /// Create a single rail end target
        /// </summary>
        private Transform CreateRailEndTarget(SplineRail rail, string targetName, bool isEndTarget)
        {
            // Create target GameObject
            GameObject targetObject = new GameObject(targetName);
            targetObject.transform.SetParent(rail.transform);
            targetObject.transform.localPosition = Vector3.zero;
            targetObject.transform.localRotation = Quaternion.identity;
            targetObject.transform.localScale = Vector3.one;
            
            // Add a collider for homing attack detection
            SphereCollider collider = targetObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.3f;
            
            // Add the RailEndHomingTarget component
            RailEndHomingTarget target = targetObject.AddComponent<RailEndHomingTarget>();
            target.SetRail(rail, isEndTarget);
            
            return targetObject.transform;
        }
        
        /// <summary>
        /// Update a specific target's position and rotation
        /// </summary>
        private void UpdateTargetPosition(Transform targetTransform, SplineRail rail, bool isEndTarget)
        {
            if (targetTransform == null || rail == null) return;
            
            // Get the total length (this will initialize the spline if needed)
            float totalLength = rail.TotalLength;
            
            float targetDistance = isEndTarget ? totalLength : 0f;
            
            // Try to get position and tangent from the rail
            if (rail.GetPositionAndTangent(targetDistance, out Vector3 worldPosition, out Vector3 worldTangent))
            {
                // Convert world position to local position relative to rail transform
                Vector3 localPosition = rail.transform.InverseTransformPoint(worldPosition);
                targetTransform.localPosition = localPosition;
                
                // Orient the target to face in the grinding direction
                // Convert world tangent to local space for rotation calculation
                Vector3 localTangent = rail.transform.InverseTransformDirection(worldTangent).normalized;
                Vector3 lookDirection = isEndTarget ? localTangent : -localTangent;
                if (lookDirection.sqrMagnitude > 0.001f)
                {
                    targetTransform.localRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
                }
            }
            else
            {
                // Fallback for editor mode: manually calculate end position using the last knot of the spline
                if (isEndTarget)
                {
                    CalculateEndPositionFallback(targetTransform, rail);
                }
                else
                {
                    CalculateStartPositionFallback(targetTransform, rail);
                }
            }
        }
        
        /// <summary>
        /// Fallback method to calculate start position using the first knot of the spline
        /// </summary>
        private void CalculateStartPositionFallback(Transform targetTransform, SplineRail rail)
        {
            SplineContainer container = rail.GetComponent<SplineContainer>();
            if (container != null && container.Spline != null && container.Spline.Count > 0)
            {
                // Get the first knot position
                float3 firstKnot = container.Spline[0].Position;
                Vector3 worldPosition = container.transform.TransformPoint(firstKnot);
                Vector3 localPosition = rail.transform.InverseTransformPoint(worldPosition);
                targetTransform.localPosition = localPosition;
                
                // Set rotation to face along the spline direction
                if (container.Spline.Count > 1)
                {
                    float3 secondKnot = container.Spline[1].Position;
                    Vector3 direction = (container.transform.TransformPoint(secondKnot) - worldPosition).normalized;
                    Vector3 localDirection = rail.transform.InverseTransformDirection(direction);
                    if (localDirection.sqrMagnitude > 0.001f)
                    {
                        targetTransform.localRotation = Quaternion.LookRotation(-localDirection, Vector3.up);
                    }
                }
            }
        }
        
        /// <summary>
        /// Fallback method to calculate end position using the last knot of the spline
        /// </summary>
        private void CalculateEndPositionFallback(Transform targetTransform, SplineRail rail)
        {
            SplineContainer container = rail.GetComponent<SplineContainer>();
            if (container != null && container.Spline != null && container.Spline.Count > 0)
            {
                // Get the last knot position
                int lastIndex = container.Spline.Count - 1;
                float3 lastKnot = container.Spline[lastIndex].Position;
                Vector3 worldPosition = container.transform.TransformPoint(lastKnot);
                Vector3 localPosition = rail.transform.InverseTransformPoint(worldPosition);
                targetTransform.localPosition = localPosition;
                
                // Set rotation to face along the spline direction
                if (container.Spline.Count > 1)
                {
                    float3 secondToLastKnot = container.Spline[lastIndex - 1].Position;
                    Vector3 direction = (worldPosition - container.transform.TransformPoint(secondToLastKnot)).normalized;
                    Vector3 localDirection = rail.transform.InverseTransformDirection(direction);
                    if (localDirection.sqrMagnitude > 0.001f)
                    {
                        targetTransform.localRotation = Quaternion.LookRotation(localDirection, Vector3.up);
                    }
                }
            }
        }
        
    }
}