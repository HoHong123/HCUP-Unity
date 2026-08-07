using UnityEngine;
using HInspector;
using HAudio.Core;

namespace HAudio.AddOn {
    public abstract class BaseSfxAddon : MonoBehaviour {
        #region Field
        // 0 = 오버라이드 없음. 별도의 useOverride bool 을 두지 않는 이유는 상태가 둘이면
        // "켰는데 값이 비었다" 는 모순 상태가 생기기 때문이다. 값 하나가 곧 의사표시다.
        [HTitle("Sound Policy")]
        [HDropdown(AudioCatalogSO.DROPDOWN_SOURCE_ID)]
        [SerializeField]
        protected int overrideClickUid;
        #endregion

        #region Protected - Handler
        // UI 재생축(PlayUI)을 기본 구현으로 두지 않는다 — 3D 월드 오브젝트 등
        // 비-UI 상속자는 Play/Play3D 로 응답해야 하므로, 어떤 재생 축을 쓸지는
        // 파생 클래스가 스스로 결정한다.
        protected abstract void _HandleClick();
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.08.07 _HandleClick 을 protected abstract 로 전환
 *
 * # 변경
 * - `_HandleClick` 기본 구현(PlayUI/PlayClick 재생 로직)을 제거하고 `protected abstract void`
 *   로 선언. 재생 로직은 `ButtonSfxAddon`/`ToggleSfxAddon` 각 파생 클래스로 이전.
 *
 * # 이유
 * - 기존 기본 구현은 `AudioManager.PlayUI`/`PlayClick` 을 호출하는 UI 전용 재생축을
 *   가정했다. `BaseSfxAddon` 이 UI 가 아닌 위치(3D 월드 오브젝트 등)에서도 상속될 수
 *   있다는 전제가 생기면서, 어떤 재생축(`PlayUI` vs `Play`/`Play3D`)을 쓸지는 파생 클래스가
 *   스스로 결정해야 하는 문제가 되었다 — base 가 UI 전용 구현을 강제하면 비-UI 상속자가
 *   잘못된 축으로 재생하거나 base 를 오버라이드로 통째로 덮어써야 한다.
 *
 * # 결과
 * - `ButtonSfxAddon`/`ToggleSfxAddon` 은 기존과 동일하게 `PlayUI`/`PlayClick` 을 호출하도록
 *   각자 `_HandleClick` 을 구현(동작 변경 없음, 위치만 이전).
 *
 * # 주의
 * - `BaseSfxAddon` 은 이제 추상 클래스라 직접 부착 불가. 신규 비-UI 파생 클래스는
 *   `Play`/`Play3D` 등 알맞은 재생축으로 `_HandleClick` 을 구현할 것.
 *
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
