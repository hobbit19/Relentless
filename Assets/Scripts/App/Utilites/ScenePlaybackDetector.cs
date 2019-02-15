#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Callbacks;

namespace Loom.ZombieBattleground
{
    public class ScenePlaybackDetector
    {
        public static bool IsPlaying { get; private set; }

        // This callback is notified just before building the scene, before Start().
        [PostProcessScene]
        public static void OnPostprocessScene()
        {
            IsPlaying = true;
        }

        static ScenePlaybackDetector()
        {
            // This callback comes after Start(), it's too late. But it's useful for detecting playback stop.
            EditorApplication.playModeStateChanged += _ =>
            {
                IsPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
            };
        }
    }
}

#endif
