using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace JackRussell.Audio
{
    public class AudioManager : IInitializable
    {
        private readonly SoundDatabase _soundDatabase;

        [Inject]
        public AudioManager(SoundDatabase soundDatabase)
        {
            _soundDatabase = soundDatabase;
        }

        public void Initialize()
        {
            Debug.Log("AudioManager Initialized.");
        }

        public void PlaySound(SoundType soundType, AudioSource audioSource, float volumeMultiplier = 1f)
        {
            SoundData data = _soundDatabase.GetSoundData(soundType);
            if (data != null && data.AudioClips != null && data.AudioClips.Length > 0)
            {
                AudioClip clipToPlay = data.GetClipToPlay(); // Use the new method

                if (clipToPlay != null)
                {
                    float originalPitch = audioSource != null ? audioSource.pitch : 1f; // Store original pitch

                    if (audioSource != null)
                    {
                        if (data.RandomizePitch)
                        {
                            audioSource.pitch = 1f + Random.Range(-data.PitchRandomizationRange, data.PitchRandomizationRange);
                        }
                        else
                        {
                            audioSource.pitch = 1f; // Reset to default if not randomized
                        }
                        audioSource.PlayOneShot(clipToPlay, data.Volume * volumeMultiplier);
                        audioSource.pitch = originalPitch; // Restore original pitch after playing
                    }
                    else
                    {
                        Debug.LogWarning($"AudioManager: AudioSource is null for SoundType {soundType}. Playing at point instead.");
                        // For PlayClipAtPoint, pitch randomization is applied directly to the temporary source
                        //float pitch = data.RandomizePitch ? (1f + Random.Range(-data.PitchRandomizationRange, data.PitchRandomizationRange)) : 1f;
                        //AudioSource.PlayClipAtPoint(clipToPlay, Vector3.zero, data.Volume * volumeMultiplier);
                    }
                }
            }
        }

        // Future methods for playing music, dialogue, etc. could go here.
        // public void PlayMusic(SoundType musicType) { ... }
        // public void PlayDialogue(SoundType dialogueType) { ... }
    }
}