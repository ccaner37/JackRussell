using UnityEngine;
using Unity.Cinemachine;

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
        /// Gets a string representation of this camera definition
        /// </summary>
        public override string ToString()
        {
            return $"{Type} Camera (Active: {IsActive}, Valid: {IsValid})";
        }
    }
}