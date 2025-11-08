using UnityEngine;
using VitalRouter;

namespace JackRussell.CameraController
{
    /// <summary>
    /// Command to switch to a specific camera type
    /// </summary>
    public struct CameraSwitchCommand : ICommand
    {
        public CameraType TargetCamera { get; }
        public float TransitionDuration { get; }

        public CameraSwitchCommand(CameraType targetCamera, float transitionDuration = 0.3f)
        {
            TargetCamera = targetCamera;
            TransitionDuration = transitionDuration;
        }
    }

    /// <summary>
    /// Command to shake the camera
    /// </summary>
    public struct CameraShakeCommand : ICommand
    {
        public float Intensity { get; }
        public float Duration { get; }

        public CameraShakeCommand(float intensity, float duration)
        {
            Intensity = intensity;
            Duration = duration;
        }

        public static CameraShakeCommand WithDefault()
        {
            return new CameraShakeCommand(0.1f, 0.5f);
        }
    }
}