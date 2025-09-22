using System.Collections.Generic;
using UnityEngine;

namespace JackRussell
{
    /// <summary>
    /// Configuration for HomingExit animations, injected via VContainer.
    /// Contains data for exit animations and jump down animation.
    /// </summary>
    [CreateAssetMenu(fileName = "HomingExitAnimationConfig", menuName = "Game/Animation/Homing ExitAnimation Config")]
    public class HomingExitAnimationConfig : ScriptableObject
    {
        [System.Serializable]
        public class AnimationData
        {
            public string animationName;
            public float enterOffset;
            public float exitNormalizedTime;
            public float transitionDuration;
        }

        public List<AnimationData> exitAnimations = new();
        public AnimationData jumpDownAnimation;
    }
}