#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 글자 효과음 서비스 계약 인터페이스 (Phase 4).
 *
 * 특징 / 지원기능 ::
 * + PlayBlip(string token) : string token 기반 블립 사운드 재생 요청
 *
 * 주의사항 ::
 * HUI → HAudio 직접 참조 금지를 위해 설계된 DI 계약.
 * AudioManagerBlipAdapter가 같은 HDialogue 패키지 내에서 구현.
 * DialogueBlipSfxAgent.blipServiceSource 슬롯에 MonoBehaviour로 배선.
 * AudioClips enum은 레거시 SoundManager 전용 — 이 인터페이스는 string 토큰만 사용.
 * =========================================================
 */
#endif

namespace HDialogue {
    public interface IBlipSfxService {
        #region Public
        void PlayBlip(string token);
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: PlayBlip(AudioClips) 오버로드 제거 롤백
 *
 * # 변경
 * - using HAudio 제거.
 * - void PlayBlip(AudioClips audioClips) 오버로드 제거.
 * - PlayBlip(string token) 단일 메서드로 복원.
 *
 * # 이유
 * - AudioClips enum은 레거시 SoundManager 전용. HDialogue DI 계약은 string 토큰만 사용.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 HUI.TextUI → HDialogue 패키지 이관
 *
 * # 변경
 * - namespace HUI.TextUI → HDialogue
 * - HUI/Runtime/HUI/Text/Dialogue/ → HDialogue/Runtime/ 이관
 * - AudioManagerBlipAdapter가 동일 패키지(HDialogue) 내에 위치하므로
 *   헤더 주의사항 문구 갱신 (어댑터 외부 계층 배치 불필요 → HDialogue 내 구현)
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 IBlipSfxService Phase 4 베이스 코드 생성
 *
 * # 목적
 * - HUI 레이어가 HAudio 패키지를 직접 참조하지 않고 블립 재생을 위임하는 DI 계약.
 *
 * # 사용 흐름
 * - DialogueBlipSfxAgent.blipServiceSource → IBlipSfxService 캐스팅
 * - PlayBlip(token) → AudioManagerBlipAdapter → AudioManager.Instance.Play(token)
 *
 * =============================================================================
 */
#endif
