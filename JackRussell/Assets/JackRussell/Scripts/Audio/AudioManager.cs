using UnityEngine;
using VContainer;
using VContainer.Unity;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;

namespace JackRussell.Audio
{
    public class AudioManager : MonoBehaviour, IInitializable
    {
        [Inject] private readonly SoundDatabase _soundDatabase;
        private GameObject _audioContainer;
        private Dictionary<SoundType, AudioSource> _activeLoopedSounds;
        
        // Object pool for temporary one-shot sounds
        private const int POOL_SIZE = 8;
        private Queue<AudioSource> _audioSourcePool;
        private HashSet<AudioSource> _inUseSources;

        public void Initialize()
        {
            _audioSourcePool = new Queue<AudioSource>();
            _inUseSources = new HashSet<AudioSource>();
            _activeLoopedSounds = new Dictionary<SoundType, AudioSource>();

            _audioContainer = new GameObject("AudioManager_Container");
            Object.DontDestroyOnLoad(_audioContainer);

            // Initialize the audio source pool
            for (int i = 0; i < POOL_SIZE; i++)
            {
                GameObject audioGO = new GameObject($"PooledAudioSource_{i}");
                audioGO.transform.SetParent(_audioContainer.transform);
                AudioSource source = audioGO.AddComponent<AudioSource>();
                source.spatialBlend = 0f; // 2D sound by default
                source.playOnAwake = false;
                _audioSourcePool.Enqueue(source);
            }

            Debug.Log($"AudioManager Initialized with {POOL_SIZE} pooled AudioSources.");
        }

        /// <summary>
        /// Plays a one-shot sound using the provided AudioSource.
        /// </summary>
        public void PlaySound(SoundType soundType, AudioSource audioSource, float volumeMultiplier = 1f)
        {
            if (audioSource == null)
            {
                Debug.LogWarning($"AudioManager: AudioSource is null for SoundType {soundType}. Use PlaySound(soundType) for automatic source management.");
                return;
            }

            SoundData data = _soundDatabase.GetSoundData(soundType);
            if (data == null || data.AudioClips == null || data.AudioClips.Length == 0)
            {
                Debug.LogWarning($"AudioManager: No sound data found for {soundType}");
                return;
            }

            AudioClip clipToPlay = data.GetClipToPlay();
            if (clipToPlay == null)
            {
                Debug.LogWarning($"AudioManager: No valid clip found for {soundType}");
                return;
            }

            // Apply pitch randomization
            if (data.RandomizePitch)
            {
                audioSource.pitch = 1f + Random.Range(-data.PitchRandomizationRange, data.PitchRandomizationRange);
            }
            else
            {
                audioSource.pitch = 1f;
            }

            audioSource.PlayOneShot(clipToPlay, data.Volume * volumeMultiplier);
        }

        /// <summary>
        /// Plays a one-shot sound using a pooled AudioSource. Suitable for UI sounds and effects.
        /// </summary>
        public void PlaySound(SoundType soundType, float volumeMultiplier = 1f)
        {
            SoundData data = _soundDatabase.GetSoundData(soundType);
            if (data == null || data.AudioClips == null || data.AudioClips.Length == 0)
            {
                Debug.LogWarning($"AudioManager: No sound data found for {soundType}");
                return;
            }

            AudioClip clipToPlay = data.GetClipToPlay();
            if (clipToPlay == null)
            {
                Debug.LogWarning($"AudioManager: No valid clip found for {soundType}");
                return;
            }

            AudioSource tempSource = GetPooledAudioSource();
            if (tempSource == null)
            {
                Debug.LogError("AudioManager: Unable to get pooled AudioSource");
                return;
            }

            // Configure the source
            if (data.RandomizePitch)
            {
                tempSource.pitch = 1f + Random.Range(-data.PitchRandomizationRange, data.PitchRandomizationRange);
            }
            else
            {
                tempSource.pitch = 1f;
            }
            
            tempSource.PlayOneShot(clipToPlay, data.Volume * volumeMultiplier);
            
            // Return to pool after clip finishes
            StartCoroutine(ReturnToPoolAfterDelay(tempSource, clipToPlay.length));
        }

        /// <summary>
        /// Plays a 3D sound at the specified position using AudioSource.PlayClipAtPoint.
        /// </summary>
        public void PlaySoundAtPoint(SoundType soundType, Vector3 position, float volumeMultiplier = 1f)
        {
            SoundData data = _soundDatabase.GetSoundData(soundType);
            if (data == null || data.AudioClips == null || data.AudioClips.Length == 0)
            {
                Debug.LogWarning($"AudioManager: No sound data found for {soundType}");
                return;
            }

            AudioClip clipToPlay = data.GetClipToPlay();
            if (clipToPlay == null)
            {
                Debug.LogWarning($"AudioManager: No valid clip found for {soundType}");
                return;
            }

            // For 3D sounds, we can't easily apply pitch randomization with PlayClipAtPoint
            // If pitch randomization is needed, use PlaySound with a temporary source
            AudioSource.PlayClipAtPoint(clipToPlay, position, data.Volume * volumeMultiplier);
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

            // Create new AudioSource for looped sound
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

        /// <summary>
        /// Stops all looped sounds immediately.
        /// </summary>
        public void StopAllLoopedSounds()
        {
            foreach (var kvp in _activeLoopedSounds)
            {
                if (kvp.Value != null && kvp.Value.gameObject != null)
                {
                    kvp.Value.DOKill();
                    kvp.Value.Stop();
                    Object.Destroy(kvp.Value.gameObject);
                }
            }
            _activeLoopedSounds.Clear();
        }

        private AudioSource GetPooledAudioSource()
        {
            if (_audioSourcePool.Count > 0)
            {
                AudioSource source = _audioSourcePool.Dequeue();
                _inUseSources.Add(source);
                return source;
            }
            
            // If pool is empty, create a new one (fallback)
            Debug.LogWarning("AudioSource pool exhausted, creating temporary source");
            GameObject audioGO = new GameObject("TempAudioSource");
            audioGO.transform.SetParent(_audioContainer.transform);
            AudioSource tempSource = audioGO.AddComponent<AudioSource>();
            tempSource.spatialBlend = 0f;
            tempSource.playOnAwake = false;
            _inUseSources.Add(tempSource);
            return tempSource;
        }

        private IEnumerator ReturnToPoolAfterDelay(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay + 0.1f); // Small buffer
            
            if (source != null && _inUseSources.Contains(source))
            {
                source.Stop();
                source.pitch = 1f; // Reset pitch
                _inUseSources.Remove(source);
                _audioSourcePool.Enqueue(source);
            }
        }
    }
}