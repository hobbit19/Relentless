using System.Collections.Generic;
using Loom.ZombieBattleground.Common;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public interface ISoundManager
    {
        bool SfxMuted { get; set; }

        bool MusicMuted { get; set; }

        float SoundVolume { get; }
        float MusicVolume { get; }

        float GetSoundLength(Enumerators.SoundType soundType, string namePattern);

        float GetSoundLength(Enumerators.SoundType soundType);

        void SetSoundPaused(int identificator, bool pause);

        void PlaySound(
            Enumerators.SoundType soundType,
            string clipTitle,
            float volume = -1f,
            Enumerators.CardSoundType cardSoundType = Enumerators.CardSoundType.NONE);

        void PlaySound(
            Enumerators.SoundType soundType,
            string clipTitle,
            float fadeOutAfterTime,
            float volume = -1f,
            Enumerators.CardSoundType cardSoundType = Enumerators.CardSoundType.NONE);

        void PlaySound(
            Enumerators.SoundType soundType,
            int priority = 128,
            float volume = -1f,
            Transform parent = null,
            bool isLoop = false,
            bool isPlaylist = false,
            bool dropOldBackgroundMusic = true,
            bool isInQueue = false);

        void PlaySound(
            Enumerators.SoundType soundType,
            float volume = -1f,
            bool isLoop = false,
            bool dropOldBackgroundMusic = false,
            bool isInQueue = false);

        int PlaySound(
            Enumerators.SoundType soundType,
            string clipTitle,
            float volume = -1f,
            bool isLoop = false,
            bool isInQueue = false);

        void PlaySound(
            Enumerators.SoundType soundType,
            int priority = 128,
            string clipTitle = "",
            float volume = -1f,
            bool isLoop = false,
            bool isInQueue = false);

        void PlaySound(
            Enumerators.SoundType soundType,
            int clipIndex,
            float volume = -1f,
            bool isLoop = false,
            bool isInQueue = false);

        void CrossfaidSound(Enumerators.SoundType soundType, Transform parent = null, bool isLoop = false);

        void SetMusicVolume(float value, bool withSaving = true);

        void SetSoundVolume(float value, bool withSaving = true);

        void TurnOffSound();

        void StopPlaying(Enumerators.SoundType soundType, int id = 0);

        void StopPlaying(List<AudioClip> clips, int id = 0);

        void SetSoundMuted(bool status, bool withSaving = true);

        void SetMusicMuted(bool status, bool withSaving = true);

        void ApplySoundData();
    }
}
