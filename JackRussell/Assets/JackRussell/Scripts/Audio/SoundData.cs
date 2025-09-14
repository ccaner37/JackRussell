using UnityEngine;

namespace JackRussell.Audio
{
    [CreateAssetMenu(fileName = "SoundData", menuName = "Game/Audio/Sound Data")]
    public class SoundData : ScriptableObject
    {
        public SoundType SoundType;
        public AudioClip[] AudioClips;
        [Range(0f, 1f)]
        public float Volume = 1f;
        public bool Loop;

        public bool IsRandomized;
        public bool RandomizePitch;
        [Range(0f, 1f)] public float PitchRandomizationRange; // e.g., 0.1 for +/- 10%

        public AudioClip GetClipToPlay()
        {
            if (AudioClips == null || AudioClips.Length == 0) return null;
            if (IsRandomized && AudioClips.Length > 1)
            {
                return AudioClips[Random.Range(0, AudioClips.Length)];
            }
            return AudioClips[0]; // Always return the first clip if not randomized or only one clip
        }
        // Add other properties like pitch, spatial blend, etc. as needed
    }
}