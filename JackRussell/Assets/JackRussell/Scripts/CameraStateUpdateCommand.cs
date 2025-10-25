using VitalRouter;

namespace JackRussell.CameraController
{
    public struct CameraStateUpdateCommand : ICommand
    {
        public float TargetDistance { get; }
        public float TargetFOV { get; }
        public float TransitionDuration { get; }

        public CameraStateUpdateCommand(float targetDistance, float targetFOV, float transitionDuration = 0.5f)
        {
            TargetDistance = targetDistance;
            TargetFOV = targetFOV;
            TransitionDuration = transitionDuration;
        }
    }
}