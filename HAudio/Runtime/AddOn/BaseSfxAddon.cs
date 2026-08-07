using UnityEngine;
using HInspector;
using HAudio.Core;

namespace HAudio.AddOn {
    public class BaseSfxAddon : MonoBehaviour {
        #region Field
        // 0 = 오버라이드 없음. 별도의 useOverride bool 을 두지 않는 이유는 상태가 둘이면
        // "켰는데 값이 비었다" 는 모순 상태가 생기기 때문이다. 값 하나가 곧 의사표시다.
        [HTitle("Sound Policy")]
        [HDropdown(AudioCatalogSO.DROPDOWN_SOURCE_ID)]
        [SerializeField]
        protected int overrideClickUid;
        #endregion

        #region Protected - Handler
        protected virtual void _HandleClick() {
            if (!AudioManager.HasInstance) return;

            if (overrideClickUid != 0) {
                AudioManager.Instance.PlayUI(overrideClickUid);
                return;
            }

            AudioManager.Instance.PlayClick();
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.08.06 overrideClickToken(string) → overrideClickUid(int) 전환
 *
 * # 변경
 * - overrideClickToken(string) 제거, overrideClickUid(int) + [HDropdown] 신설.
 * - useOverride(bool) 와 [HShowIf] 제거 — uid == 0 이 "오버라이드 없음" 을 뜻한다.
 *
 * # 이유
 * - 기존 필드는 인스펙터에서 자유 문자열을 손으로 타이핑하는 구조였다. 오타는 런타임 무음이고
 *   검출 수단이 없었다. FormerlySerializedAs("overrideClickUid") 가 남아 있던 것에서 보듯
 *   원래 uid 였고, 58fc15a 의 int 식별자 전면 제거로 문자열이 된 것이다.
 * - enum 으로 바꿀 수 없다. AudioClips 는 프로젝트마다 생성되는 게임 어셈블리 타입이라
 *   HCUP 이 참조할 수 없다 (ADR-0001 §4.2). 그리고 직렬화 필드의 값은 YAML 에 들어가므로
 *   enum 으로 선언해도 컴파일러가 검증하지 않는다 — enum 은 여기서 안전을 주지 못한다.
 *   필요한 것은 제한된 편집 UI + 참조 검증이고, [HDropdown] 이 그 앞단이다.
 *
 * # 마이그레이션
 * - 전환 시점 기준 이 컴포넌트의 씬·프리팹 부착은 0건이었다. 값 이관 코드가 필요 없어
 *   FormerlySerializedAs 없이 교체했다 (타입이 달라 어차피 쓸 수 없다).
 *
 * =============================================================================
 */
#endif
