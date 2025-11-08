using VitalRouter;
using UnityEngine;

namespace JackRussell.CameraController
{
    public struct CameraStateUpdateCommand : ICommand
    {
        public float? TargetDistance { get; }
        public float? TargetFOV { get; }
        public float TransitionDuration { get; }

        public CameraStateUpdateCommand(float? targetDistance = null, float? targetFOV = null, float transitionDuration = 0.5f)
        {
            TargetDistance = targetDistance;
            TargetFOV = targetFOV;
            TransitionDuration = transitionDuration;
        }

        /// <summary>
        /// Creates a command with only distance and FOV changes.
        /// </summary>
        public static CameraStateUpdateCommand WithDistanceAndFOV(float? targetDistance, float? targetFOV, float transitionDuration = 0.5f)
        {
            return new CameraStateUpdateCommand(targetDistance, targetFOV, transitionDuration);
        }
    }
}