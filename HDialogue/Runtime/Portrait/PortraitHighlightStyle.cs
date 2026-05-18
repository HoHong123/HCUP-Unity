#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 화자/비화자 하이라이트 스타일 데이터 구조체.
 *
 * 특징 / 지원기능 ::
 * + SpeakerTint / NonSpeakerTint       - 색상 틴트 (white = 원색)
 * + SpeakerScale / NonSpeakerScale     - 크기 배율 (1.0 = 기본)
 * + TransitionDuration                 - 하이라이트 전환 시간(초)
 *
 * 주의사항 ::
 * StageLayoutSO.highlightStyle 필드로 보유.
 * CharacterPortraitController.SetHighlight() 에서 소비.
 * =========================================================
 */
#endif

using System;
using HInspector;
using UnityEngine;

namespace HDialogue {
    [Serializable]
    public struct PortraitHighlightStyle {
        [HTitle("Color")]
        public Color SpeakerTint;
        public Color NonSpeakerTint;

        [HTitle("Scale")]
        public float SpeakerScale;
        public float NonSpeakerScale;

        [HTitle("Timing")]
        public float TransitionDuration;

        public static PortraitHighlightStyle Default => new() {
            SpeakerTint = Color.white,
            NonSpeakerTint = new Color(0.6f, 0.6f, 0.6f, 1f),
            SpeakerScale = 1.0f,
            NonSpeakerScale = 0.95f,
            TransitionDuration = 0.15f
        };
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: HInspector HTitle 그룹 추가 (Color / Scale / Timing)
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 PortraitHighlightStyle 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 4-A — StageLayoutSO 내 하이라이트 스타일 계약
 *
 * =============================================================================
 */
#endif
