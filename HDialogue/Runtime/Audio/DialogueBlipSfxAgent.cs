#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- HCUP-2.2.0 글자 효과음 에이전트 (Phase 4).
 *
 * 특징 / 지원기능 ::
 * + PlayBlip()               : currentBlipToken → IBlipSfxService.PlayBlip 위임
 * + SetVoice(string token)   : VoiceSet 태그 수신 → 현재 토큰 교체
 * + ResetVoice(string token) : PlayLine 시 라인 오버라이드 또는 기본 토큰으로 리셋
 *
 * 주의사항 ::
 * blipServiceSource는 IBlipSfxService를 구현한 MonoBehaviour를 Inspector에서 연결.
 * defaultBlipToken 미설정 시 무음으로 동작. AudioClip 직접 참조 없음.
 * DialogueTextController와 동일 GameObject에 부착.
 * =========================================================
 */
#endif

using UnityEngine;
using HInspector;
using HDiagnosis.Logger;

namespace HDialogue {
    public sealed class DialogueBlipSfxAgent : MonoBehaviour {
        #region 변수
        [HTitle("Blip")]
        [SerializeField]
        string defaultBlipToken;
        [SerializeField]
        MonoBehaviour blipServiceSource;

        IBlipSfxService blipService;
        string currentBlipToken;
        #endregion

        #region Unity Life Cycle
        private void Awake() {
            blipService = blipServiceSource as IBlipSfxService;
            if (blipService == null && blipServiceSource != null)
                HLogger.Error("[DialogueBlipSfxAgent] blipServiceSource does not implement IBlipSfxService.");
            currentBlipToken = defaultBlipToken;
        }
        #endregion

        #region Public
        public void ResetVoice(string lineOverrideToken) {
            currentBlipToken = !string.IsNullOrEmpty(lineOverrideToken)
                ? lineOverrideToken
                : defaultBlipToken;
        }

        public void SetVoice(string token) {
            if (!string.IsNullOrEmpty(token)) currentBlipToken = token;
        }

        public void PlayBlip() {
            if (string.IsNullOrEmpty(currentBlipToken)) return;
            blipService?.PlayBlip(currentBlipToken);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 Debug.LogError → HLogger.Error 교체
 *
 * # 변경
 * - using HDiagnosis.Logger 추가
 * - Awake(): Debug.LogError → HLogger.Error (IBlipSfxService 캐스팅 실패 경고)
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 HUI.TextUI → HDialogue 패키지 이관
 *
 * # 변경
 * - namespace HUI.TextUI → HDialogue
 * - HUI/Runtime/HUI/Text/Dialogue/ → HDialogue/Runtime/ 이관
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueBlipSfxAgent Phase 4 B안 — DI 래퍼 구현
 *
 * # 변경
 * - AudioSource 풀(POOL_SIZE 3) 전면 제거
 * - IBlipSfxService DI 래퍼로 교체: blipServiceSource(Inspector) + blipService(캐스팅)
 * - PlayBlip(AudioClip) → PlayBlip() (token 내부 관리, clip 참조 없음)
 * - LookupVoice(key) 제거 → SetVoice(token) / ResetVoice(token) 으로 대체
 *
 * # 이유
 * - HUI → HAudio 직접 참조 불가(패키지 경계). token 문자열로 AudioManager 파이프라인 위임.
 *
 * =============================================================================
 */
#endif
