#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 버튼 클릭마다 대상 오브젝트를 From 지점과 To 지점 사이에서 왕복시키는 컴포넌트입니다.
 *
 * 특징 / 지원기능 ::
 * + 타겟마다 첫 클릭은 To 지점, 다음 클릭은 From 지점으로 번갈아 이동합니다.
 * + 타겟의 returnOnReclick 을 끄면 그 타겟은 To 도착 후의 클릭을 무시하고 그 자리에 머뭅니다.
 * + 부모 이전 여부는 TransitUiEntity 의 reparentOnArrive 로 지정합니다.
 *
 * 주의사항 ::
 * 트리거는 Pointer Down / Up 이 아니라 Button.onClick 입니다. 누른 채 버튼 밖에서 떼면 이동하지 않습니다.
 * =========================================================
 */
#endif

using UnityEngine;
using HUI.Entity;
using HInspector;

namespace HUI.ButtonUI {
    public class TransitOnClickButton : BaseOnPressButton {
        #region Fields
        [HTitle("Targets")]
        [SerializeField]
        TransitUiEntity[] targets;
        #endregion

        #region Unity Life Cycle
        private void OnEnable() {
            Button.onClick.AddListener(_OnClick);
        }

        private void OnDisable() {
            Button.onClick.RemoveListener(_OnClick);
        }
        #endregion

        #region Public - UI Events
        public override void OnPointDown() { }
        public override void OnPointUp() { }
        #endregion

        #region Private
        private void _OnClick() {
            foreach (var target in targets) {
                if (!target.IsAtDestination) target.MoveToDestination();
                else if (target.ReturnOnReclick) target.MoveToOrigin();
            }
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.09.16 재클릭 복귀 옵션을 타겟 단위로 이관
 *
 * # 변경
 * - 버튼의 returnOnReclick 필드와 isAtDestination 필드를 제거하고 TransitUiEntity 로 옮겼다.
 * - _OnClick 은 타겟마다 IsAtDestination 을 보고, 도착 전이면 MoveToDestination, 도착 후면 ReturnOnReclick 일 때만 MoveToOrigin 을 호출한다.
 *
 * # 이유
 * - 사용자 요청으로 옵션을 타겟마다 지정한다. 타겟별 옵션이면 클릭 후 타겟마다 위치가 갈리므로 버튼 단일 상태로는 다음 클릭의 방향을 정할 수 없다.
 *
 * # 주의
 * - 버튼에 저장돼 있던 returnOnReclick 값은 이관되지 않는다. 기존 배치는 타겟 기본값 true 로 동작한다.
 *
 * =============================================================================
 * @Jason - PKH 2026.09.16 재클릭 복귀 옵션 추가
 *
 * # 추가
 * - returnOnReclick (기본 true). false 면 To 에 도착한 상태의 클릭을 무시해 대상이 그 자리에 머문다.
 *
 * # 설계 결정
 * - 기본값 true 는 기존 왕복 동작을 유지해 이미 배치된 컴포넌트의 동작이 바뀌지 않게 한다.
 * - "도착" 판정은 트윈 완료가 아니라 isAtDestination 상태 기준이다. 이동 중 재클릭도 도착으로 본다.
 * - TransitOnSelectToggle 에는 추가하지 않는다. 위치가 isOn 상태로 결정되므로 재클릭 개념이 없다.
 *
 * =============================================================================
 * @Jason - PKH 2026.09.15 TransitOnClickButton 베이스 코드 생성
 *
 * # 목적
 * - MoveOnPressButton 은 누르는 동안만 이동하고 떼면 원위치한다. 클릭 단위로 두 지점 사이를 오가는 버튼이 필요했다.
 *
 * # 사용 흐름
 * - DelegateButton 과 같은 GameObject 에 붙이고 targets 에 TransitUiEntity 를 등록한다.
 * - 클릭 → _OnClick → isAtDestination 반전 → MoveToDestination / MoveToOrigin.
 *
 * # 설계 결정
 * - BaseOnPressButton 을 상속해 DelegateButton 획득(Awake) 과 RequireComponent 를 재사용한다. OnPointDown / OnPointUp 은 사용하지 않으므로 빈 구현이다.
 * - onClick 구독은 OnEnable / OnDisable 쌍으로 둔다. base 의 Awake 가 Button 을 먼저 채우므로 OnEnable 시점에 null 이 아니다.
 * - isAtDestination 은 컴포넌트 단위 상태다. 초기 위치를 From 으로 강제하지 않으므로, 씬 배치 시 대상을 From 위치에 두어야 첫 클릭이 의도대로 보인다.
 *
 * =============================================================================
 */
#endif
