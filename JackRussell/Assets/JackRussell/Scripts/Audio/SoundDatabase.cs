using System.Collections.Generic;
using UnityEngine;

namespace JackRussell.Audio
{
    [CreateAssetMenu(fileName = "SoundDatabase", menuName = "Game/Audio/Sound Database")]
    public class SoundDatabase : ScriptableObject
    {
        [SerializeField]
        private SoundData[] _allSoundData;

        private Dictionary<SoundType, SoundData> _soundDict;

        private void OnEnable()
        {
            _soundDict = new Dictionary<SoundType, SoundData>();
            foreach (var soundData in _allSoundData)
            {
                if (_soundDict.ContainsKey(soundData.SoundType))
                {
                    Debug.LogWarning($"Duplicate SoundType {soundData.SoundType} found in SoundDatabase. Only the first instance will be used.");
                    continue;
                }
                _soundDict[soundData.SoundType] = soundData;
            }
        }


        public SoundData GetSoundData(SoundType soundType)
        {
            if (_soundDict.TryGetValue(soundType, out SoundData data))
            {
                return data;
            }
            Debug.LogWarning($"SoundData for SoundType {soundType} not found in SoundDatabase.");
            return null;
        }
    }
}