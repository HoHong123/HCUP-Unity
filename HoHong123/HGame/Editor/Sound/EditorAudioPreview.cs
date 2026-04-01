#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HGame.Editor.Sound {
    internal static class EditorAudioPreview {
        static Type audioUtilType;
        static MethodInfo playPreviewClip;
        static MethodInfo stopAllPreviewClips;
        static MethodInfo stopPreviewClip;

        static EditorAudioPreview() {
            audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtilType == null) return;

            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            // Unity 버전별 오버로드 차이 대응
            playPreviewClip =
                audioUtilType.GetMethod("PlayPreviewClip", flags, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null) ??
                audioUtilType.GetMethod("PlayPreviewClip", flags, null, new[] { typeof(AudioClip), typeof(int), typeof(bool), typeof(float) }, null);

            stopAllPreviewClips = audioUtilType.GetMethod("StopAllPreviewClips", flags);

            // 버전에 따라 있을 수도/없을 수도 있음
            stopPreviewClip =
                audioUtilType.GetMethod("StopPreviewClip", flags, null, new[] { typeof(AudioClip) }, null) ??
                audioUtilType.GetMethod("StopPreviewClip", flags, null, new[] { typeof(AudioClip), typeof(bool) }, null);
        }

        public static bool CanUse => audioUtilType != null && playPreviewClip != null && stopAllPreviewClips != null;

        /// <summary>기본값 single=true :: 기존 프리뷰를 모두 멈춘 뒤 재생(중복 방지).</summary>
        public static void Play(AudioClip clip, bool loop = false, bool single = true) {
            if (!clip || playPreviewClip == null) return;
            if (single) StopAll();

            var play = playPreviewClip.GetParameters();
            if (play.Length == 3) playPreviewClip.Invoke(null, new object[] { clip, 0, loop });
            else if (play.Length == 4) playPreviewClip.Invoke(null, new object[] { clip, 0, loop, 1f });
        }

        public static void StopAll() {
            stopAllPreviewClips?.Invoke(null, null);
        }

        public static void Stop(AudioClip clip) {
            if (!clip) return;

            if (stopPreviewClip == null) {
                StopAll();
                return;
            }

            var play = stopPreviewClip.GetParameters();
            if (play.Length == 1) stopPreviewClip.Invoke(null, new object[] { clip });
            else if (play.Length == 2) stopPreviewClip.Invoke(null, new object[] { clip, true });
        }
    }
}
#endif
