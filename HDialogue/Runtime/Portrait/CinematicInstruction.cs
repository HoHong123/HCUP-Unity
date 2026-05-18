#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueCinematicNode 직렬화용 단일 연출 지시 구조체.
 *
 * 특징 / 지원기능 ::
 * + Inspector 편집 가능 (Serializable struct)
 * + ToEventInstruction() — PortraitEventInstruction 변환 후 CharacterStageDirector 전달
 *
 * 주의사항 ::
 * PortraitEventInstruction 과 구분. 이 구조체는 SO 직렬화 전용.
 * arg 는 단일 문자열 — Pose/Face/Slot 동사에서 각 1개 인자(포즈키·방향·슬롯키).
 * Show / Hide / Shake / Bounce 는 arg 없음.
 * =========================================================
 */
#endif

using System;
using UnityEngine;

namespace HDialogue {
    [Serializable]
    public struct CinematicInstruction {
        [SerializeField]
        PortraitVerb verb;
        [SerializeField]
        string targetCharacterKey;
        [SerializeField]
        string arg;

        public PortraitVerb Verb => verb;
        public string TargetCharacterKey => targetCharacterKey;
        public string Arg => arg;

        public PortraitEventInstruction ToEventInstruction() {
            string[] args = string.IsNullOrEmpty(arg)
                ? Array.Empty<string>()
                : new[] { arg };
            return new PortraitEventInstruction(verb, targetCharacterKey, args);
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.16 CinematicInstruction 신규 생성
 *
 * # 목적
 * - DialogueCinematicNode의 연출 지시 목록 항목. Inspector 직렬화 전용.
 *
 * # 설계 결정
 * - PortraitEventInstruction 과 별도 struct: readonly struct는 [Serializable] 불가.
 * - arg 단일 문자열: portrait.* 프로토콜의 모든 동사는 인자 0~1개.
 * - ToEventInstruction(): 기존 CharacterStageDirector._Apply 경로 재사용.
 *
 * =============================================================================
 */
#endif
