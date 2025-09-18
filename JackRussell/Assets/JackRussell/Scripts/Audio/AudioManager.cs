using UnityEngine;
using VContainer;
using VContainer.Unity;
using DG.Tweening;
using System.Collections.Generic;

namespace JackRussell.Audio
{
    public class AudioManager : IInitializable
    {
        private readonly SoundDatabase _soundDatabase;
        private GameObject _audioContainer;
        private Dictionary<SoundType, AudioSource> _activeLoopedSounds = new();

        [Inject]
        public AudioManager(SoundDatabase soundDatabase)
        {
            _soundDatabase = soundDatabase;
        }

        public void Initialize()
        {
            _audioContainer = new GameObject("AudioManager_LoopedSounds");
            Object.DontDestroyOnLoad(_audioContainer);
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

        public void StartLoopedSound(SoundType soundType, float fadeInDuration = 0.5f)
        {
            SoundData data = _soundDatabase.GetSoundData(soundType);
            if (data == null || !data.Loop || data.AudioClips == null || data.AudioClips.Length == 0)
            {
                Debug.LogWarning($"Cannot start looped sound for {soundType}: invalid data or not set to loop.");
                return;
            }

            AudioClip clip = data.GetClipToPlay();
            if (clip == null) return;

            if (_activeLoopedSounds.TryGetValue(soundType, out AudioSource existingSource) && existingSource != null)
            {
                // Already playing, kill any existing tween and fade to full volume
                existingSource.DOKill();
                existingSource.DOFade(data.Volume, fadeInDuration);
                return;
            }

            // Create new AudioSource
            GameObject audioGO = new GameObject($"Looped_{soundType}");
            audioGO.transform.SetParent(_audioContainer.transform);
            AudioSource audioSource = audioGO.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.volume = 0f; // Start silent
            audioSource.Play();

            // Fade in
            audioSource.DOFade(data.Volume, fadeInDuration);

            _activeLoopedSounds[soundType] = audioSource;
        }

        public void StopLoopedSound(SoundType soundType, float fadeOutDuration = 0.5f)
        {
            if (!_activeLoopedSounds.TryGetValue(soundType, out AudioSource audioSource) || audioSource == null)
            {
                return;
            }

            // Kill any existing tween
            audioSource.DOKill();

            // Fade out and then destroy
            audioSource.DOFade(0f, fadeOutDuration).OnComplete(() =>
            {
                if (audioSource != null && audioSource.gameObject != null)
                {
                    audioSource.Stop();
                    Object.Destroy(audioSource.gameObject);
                }
                _activeLoopedSounds.Remove(soundType);
            });
        }

        // Future methods for playing music, dialogue, etc. could go here.
        // public void PlayMusic(SoundType musicType) { ... }
        // public void PlayDialogue(SoundType dialogueType) { ... }
    }
}