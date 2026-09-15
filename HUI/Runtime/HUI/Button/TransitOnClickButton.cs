#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 버튼 클릭마다 대상 오브젝트를 From 지점과 To 지점 사이에서 왕복시키는 컴포넌트입니다.
 *
 * 특징 / 지원기능 ::
 * + 첫 클릭은 To 지점, 다음 클릭은 From 지점으로 번갈아 이동합니다.
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

        bool isAtDestination;
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
            isAtDestination = !isAtDestination;

            foreach (var target in targets) {
                if (isAtDestination) target.MoveToDestination();
                else target.MoveToOrigin();
            }
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
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
