using VitalRouter;
using UnityEngine;

namespace JackRussell.CameraController
{
    public struct CameraStateUpdateCommand : ICommand
    {
        public float? TargetDistance { get; }
        public float? TargetFOV { get; }
        public float TransitionDuration { get; }
        public Vector3? TargetOffset { get; }

        public CameraStateUpdateCommand(float? targetDistance = null, float? targetFOV = null, float transitionDuration = 0.5f, Vector3? targetOffset = null)
        {
            TargetDistance = targetDistance;
            TargetFOV = targetFOV;
            TransitionDuration = transitionDuration;
            TargetOffset = targetOffset;
        }

        /// <summary>
        /// Creates a command with only target offset changes, keeping distance and FOV unchanged.
        /// </summary>
        public static CameraStateUpdateCommand WithTargetOffset(Vector3? targetOffset, float transitionDuration = 0.5f)
        {
            return new CameraStateUpdateCommand(null, null, transitionDuration, targetOffset);
        }

        /// <summary>
        /// Creates a command with only distance and FOV changes, keeping offset unchanged.
        /// </summary>
        public static CameraStateUpdateCommand WithDistanceAndFOV(float? targetDistance, float? targetFOV, float transitionDuration = 0.5f)
        {
            return new CameraStateUpdateCommand(targetDistance, targetFOV, transitionDuration, null);
        }
    }
}