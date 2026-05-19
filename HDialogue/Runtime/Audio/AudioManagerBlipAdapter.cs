#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- IBlipSfxService → AudioManager 연결 어댑터 (HDialogue 패키지).
 *
 * 특징 / 지원기능 ::
 * + IBlipSfxService 구현 — PlayBlip(string token) → AudioManager.Instance.Play(token)
 * + HUI 패키지가 HAudio를 모른 채 블립 재생 가능하도록 의존성 역전
 *
 * 주의사항 ::
 * 재생 전 해당 token을 AudioManager.PrewarmToken으로 미리 로드해야 한다.
 * DialogueBlipSfxAgent.blipServiceSource 슬롯에 Inspector 연결 필요.
 * AudioClips enum은 레거시 SoundManager 전용 — 이 어댑터는 string 토큰만 사용.
 * =========================================================
 */
#endif

using HAudio;
using UnityEngine;

namespace HDialogue {
    public sealed class AudioManagerBlipAdapter : MonoBehaviour, IBlipSfxService {
        #region Public
        public void PlayBlip(string token) {
            if (string.IsNullOrEmpty(token)) return;
            if (AudioManager.Instance == null) return;
            AudioManager.Instance.Play(token);
        }
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
 * - PlayBlip(AudioClips audioClips) 오버로드 제거.
 * - PlayBlip(string token) 단일 구현 복원.
 *
 * # 이유
 * - AudioManager.Instance.Play는 string 토큰만 수락. AudioClips enum 불필요.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: PlayBlip — AudioManager.Instance null guard 추가
 *
 * # 변경
 * - `if (AudioManager.Instance == null) return;` 추가.
 *
 * # 이유
 * - 씬 전환 중이거나 싱글톤 미초기화 시 NullReferenceException 방지.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 AudioManagerBlipAdapter HDialogue 패키지 이관
 *
 * # 변경
 * - 05_Study/Dialogue/Dialogue_Scripts/ → HDialogue/Runtime/Audio/ 이관
 * - namespace SWU.Dialogue → HDialogue
 *
 * # 이유
 * - AudioManagerBlipAdapter는 HUI(IBlipSfxService) + HAudio(AudioManager) 양쪽을
 *   모두 참조하므로 05_Study 학습 계층이 아닌 전용 브릿지 패키지(HDialogue)에 배치.
 * - HDialogue asmdef가 HCUP.HUI + HCUP.HAudio 둘 다 참조 → 순환 참조 없이 연결.
 *
 * # 사용 흐름
 * - DialogueBlipSfxAgent.blipServiceSource 슬롯에 Inspector 연결.
 * - PlayBlip(token) → AudioManager.Instance.Play(token).
 * - 씬 시작 시 AudioManager.PrewarmToken(token) 선행 호출 필요.
 *
 * =============================================================================
 */
#endif
