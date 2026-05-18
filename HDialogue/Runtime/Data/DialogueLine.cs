#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 상위 DialogueDirector가 DialogueTextController에 넘기는 1라인 단위 데이터.
 *
 * 특징 / 지원기능 ::
 * + SpeakerKey      - 캐릭터 식별자 (블립 사운드 매핑용, 표정/초상화 무관)
 * + RawText         - 태그 포함 원본 텍스트 (이미 번역된 결과)
 * + SpeedMultiplier - 라인 전체 속도 배수 (1.0 = 기본)
 * + OverrideBlipToken - 라인 전용 블립 토큰 (null이면 defaultBlipToken 사용)
 *
 * 주의사항 ::
 * SpeakerKey는 블립 사운드 매핑에만 사용. 캐릭터 정체 표시는 상위 시스템 책임.
 * RawText는 로컬리제이션 후 번역 완료된 문자열이어야 한다.
 * =========================================================
 */
#endif

namespace HDialogue {
    public struct DialogueLine {
        #region Fields
        public string SpeakerKey;
        public string RawText;
        public float SpeedMultiplier;
        public HAudio.AudioClips OverrideBlipToken;
        #endregion

        #region Static Factory
        /// <summary>기본 속도(1.0), 기본 블립으로 단일 텍스트 라인 생성.</summary>
        public static DialogueLine Simple(string rawText, string speakerKey = "") =>
            new DialogueLine {
                SpeakerKey = speakerKey,
                RawText = rawText,
                SpeedMultiplier = 1f,
            };
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 HUI.TextUI → HDialogue 패키지 이관
 *
 * # 변경
 * - namespace HUI.TextUI → HDialogue
 * - HUI/Runtime/HUI/Text/Dialogue/ → HDialogue/Runtime/ 이관
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 Phase 4 — OverrideBlip → OverrideBlipToken
 *
 * # 변경
 * - AudioClip OverrideBlip 필드를 string OverrideBlipToken으로 교체
 * - using UnityEngine 제거 (AudioClip 의존 해소)
 *
 * # 이유
 * - Phase 4 B안: IBlipSfxService(DI) 설계에서 클립 직접 참조 불필요.
 *   token 문자열로 AudioManager 파이프라인에 위임하는 구조로 전환.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.14 DialogueLine 베이스 코드 생성
 *
 * # 목적
 * - DialogueDirector → DialogueTextController 1라인 단위 데이터 계약
 * - HCUP-2.2.0 Custom TMP_Text Phase 0
 *
 * # 설계 결정
 * - struct 선택 이유: PlayLine 호출당 1회 생성, 단순 값 전달. heap 불필요.
 * - SpeedMultiplier 기본값 1.0 — 라인별 속도 튜닝 가능, 미설정 시 정상 속도.
 *
 * =============================================================================
 */
#endif
