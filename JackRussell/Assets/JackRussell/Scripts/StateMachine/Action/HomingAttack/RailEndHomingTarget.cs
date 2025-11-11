using UnityEngine;
using JackRussell;
using JackRussell.Rails;

namespace JackRussell.States.Action
{
    /// <summary>
    /// Represents the end point (start or end) of a rail that can be targeted by homing attacks.
    /// When hit, this target will attach the player to the rail and start grinding.
    /// </summary>
    public class RailEndHomingTarget : MonoBehaviour, IHomingTarget
    {
        [Header("Rail Reference")]
        [SerializeField] private SplineRail _targetRail;
        
        [Header("Target Settings")]
        [SerializeField] private bool _isEndTarget = true; // true = end of rail, false = start of rail
        
        [Header("Audio")]
        [SerializeField] private Audio.SoundType _attachSound = Audio.SoundType.Kick;
        
        // Runtime data
        private bool _isActive = true;
        
        public Transform TargetTransform => transform;
        
        public bool IsActive 
        { 
            get => _isActive && _targetRail != null && _targetRail.IsGrindable;
            private set => _isActive = value;
        }
        
        public SplineRail TargetRail => _targetRail;
        
        public bool IsEndTarget => _isEndTarget;
        
        public void OnHomingHit(Player player)
        {
            if (_targetRail == null || !IsActive) return;
            
            Debug.Log($"[RailEndHomingTarget] Homing hit on rail end target - Rail: {_targetRail.gameObject.name}, IsEnd: {_isEndTarget}");
            
            // Play attach sound
            if (_attachSound != Audio.SoundType.None)
            {
                player.PlaySound(_attachSound);
            }
        }
        
        public void OnHitStopEnd(Player player)
        {
            if (_targetRail == null) return;
            
            Debug.Log($"[RailEndHomingTarget] Hit stop ended, attaching to rail: {_targetRail.gameObject.name}");
            
            // Get the rail detector from the player
            var railDetector = player.GetComponent<RailDetector>();
            if (railDetector == null)
            {
                Debug.LogError("[RailEndHomingTarget] No RailDetector found on player!");
                return;
            }
            
            // Force attach to the rail at the end point
            bool attached = railDetector.TryAttachToRail(_targetRail);
            if (attached)
            {
                // Set the distance to the appropriate end point
                float targetDistance = _isEndTarget ? _targetRail.TotalLength - 0.01f : 0.01f; // Small offset to avoid exact endpoints
                railDetector.UpdateRailPosition(targetDistance - railDetector.CurrentDistance);
                
                // Transition to grind state
                ChangeToGrindState(player);
            }
            else
            {
                Debug.LogWarning($"[RailEndHomingTarget] Failed to attach to rail: {_targetRail.gameObject.name}");
            }
        }
        
        /// <summary>
        /// Change the player state to grind state
        /// </summary>
        private void ChangeToGrindState(Player player)
        {
            // Find the state machine
            var stateMachine = player.GetComponent<StateMachine>();
            if (stateMachine != null)
            {
                // Transition directly to grind state
                stateMachine.ChangeState(new States.Locomotion.GrindState(player, stateMachine));
            }
        }
        
        /// <summary>
        /// Set the rail reference and update position
        /// </summary>
        public void SetRail(SplineRail rail, bool isEndTarget)
        {
            _targetRail = rail;
            _isEndTarget = isEndTarget;
        }
        
        /// <summary>
        /// Activate or deactivate this target
        /// </summary>
        public void SetActive(bool active)
        {
            IsActive = active;
        }
    }
}