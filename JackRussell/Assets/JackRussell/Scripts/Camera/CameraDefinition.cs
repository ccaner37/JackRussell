using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

namespace JackRussell.CameraController
{
    /// <summary>
    /// Defines a camera with its type and corresponding CinemachineCamera component.
    /// This class is serializable and can be configured in the inspector for easy camera management.
    /// </summary>
    [System.Serializable]
    public class CameraDefinition
    {
        [Header("Camera Type")]
        [Tooltip("The type of this camera for identification and management")]
        public CameraType Type;

        [Header("Cinemachine Camera")]
        [Tooltip("The CinemachineCamera component for this camera type")]
        public CinemachineCamera CinemachineCamera;

        [Header("Default Settings")]
        [Tooltip("Whether this camera is enabled at start")]
        public bool EnabledAtStart = false;

        [Header("Position Composer")]
        [Tooltip("CinemachinePositionComposer for camera offset tweening")]
        public CinemachinePositionComposer PositionComposer;

        [Header("Tween Settings")]
        [Tooltip("Vector3 offset to tween to when this camera is activated (Vector3.zero to disable)")]
        public Vector3 TweenOffset = Vector3.zero;

        [Tooltip("Duration of the tween animation")]
        public float TweenDuration = 0.3f;

        [Tooltip("Delay before starting the tween")]
        public float TweenDelay = 0.2f;

        // Runtime state
        private Vector3 _defaultComposerOffset;
        private bool _hasDefaultOffset;
        private Coroutine _tweenCoroutine;

        /// <summary>
        /// Gets whether this camera definition is valid
        /// </summary>
        public bool IsValid => CinemachineCamera != null && Type != CameraType.Custom;

        /// <summary>
        /// Sets the active state of this camera (true = priority 10, false = priority 0)
        /// </summary>
        /// <param name="isActive">True to activate camera, false to deactivate</param>
        public void SetActive(bool isActive)
        {
            if (CinemachineCamera == null) return;

            CinemachineCamera.Priority = isActive ? 10 : 0;
        }

        /// <summary>
        /// Gets whether this camera is currently active (priority > 0)
        /// </summary>
        public bool IsActive => CinemachineCamera != null && CinemachineCamera.Priority > 0;

        /// <summary>
        /// Gets the default position composer offset
        /// </summary>
        public Vector3 GetDefaultComposerOffset()
        {
            if (PositionComposer != null && !_hasDefaultOffset)
            {
                _defaultComposerOffset = PositionComposer.TargetOffset;
                _hasDefaultOffset = true;
            }
            return _defaultComposerOffset;
        }

        /// <summary>
        /// Resets the position composer offset to default value
        /// </summary>
        public void ResetComposerOffset()
        {
            if (PositionComposer != null && _hasDefaultOffset)
            {
                PositionComposer.TargetOffset = _defaultComposerOffset;
            }
        }

        /// <summary>
        /// Stops any ongoing tween
        /// </summary>
        public void StopTween()
        {
            if (_tweenCoroutine != null)
            {
                PositionComposer.StopCoroutine(_tweenCoroutine);
                _tweenCoroutine = null;
            }
        }

        /// <summary>
        /// Tweens the position composer offset to the configured values
        /// </summary>
        public void TweenComposerOffset()
        {
            // If TweenOffset is zero, don't tween
            if (TweenOffset == Vector3.zero) return;
            if (PositionComposer == null) return;

            // Stop any existing tween
            StopTween();

            GetDefaultComposerOffset(); // Ensure default is stored
            
            // Start tweening coroutine
            _tweenCoroutine = PositionComposer.StartCoroutine(TweenOffsetCoroutine());
        }

        /// <summary>
        /// Tweens to a specific Vector3 offset
        /// </summary>
        /// <param name="targetOffset">Target offset</param>
        /// <param name="duration">Tween duration</param>
        /// <param name="delay">Tween delay</param>
        public void TweenToOffset(Vector3 targetOffset, float duration = 0.3f, float delay = 0f)
        {
            if (PositionComposer == null) return;

            // Stop any existing tween
            StopTween();

            GetDefaultComposerOffset(); // Ensure default is stored
            
            // Start tweening coroutine
            _tweenCoroutine = PositionComposer.StartCoroutine(TweenToSpecificOffsetCoroutine(targetOffset, duration, delay));
        }

        /// <summary>
        /// Coroutine for tweening to the configured offset
        /// </summary>
        private IEnumerator TweenOffsetCoroutine()
        {
            // Wait for delay
            if (TweenDelay > 0f)
            {
                yield return new WaitForSeconds(TweenDelay);
            }

            Vector3 startOffset = PositionComposer.TargetOffset;
            Vector3 targetOffset = GetDefaultComposerOffset() + TweenOffset;

            float elapsedTime = 0f;
            
            while (elapsedTime < TweenDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / TweenDuration);
                
                // Simple ease in-out using sine curve
                float easeT = Mathf.Sin(t * Mathf.PI * 0.5f);
                
                PositionComposer.TargetOffset = Vector3.Lerp(startOffset, targetOffset, easeT);
                yield return null;
            }
            
            // Ensure final value is set
            PositionComposer.TargetOffset = targetOffset;
            _tweenCoroutine = null;
        }

        /// <summary>
        /// Coroutine for tweening to a specific offset
        /// </summary>
        private IEnumerator TweenToSpecificOffsetCoroutine(Vector3 targetOffset, float duration, float delay)
        {
            // Wait for delay
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            Vector3 startOffset = PositionComposer.TargetOffset;

            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                
                // Simple ease in-out using sine curve
                float easeT = Mathf.Sin(t * Mathf.PI * 0.5f);
                
                PositionComposer.TargetOffset = Vector3.Lerp(startOffset, targetOffset, easeT);
                yield return null;
            }
            
            // Ensure final value is set
            PositionComposer.TargetOffset = targetOffset;
            _tweenCoroutine = null;
        }

        /// <summary>
        /// Gets a string representation of this camera definition
        /// </summary>
        public override string ToString()
        {
            return $"{Type} Camera (Active: {IsActive}, Valid: {IsValid})";
        }
    }
}