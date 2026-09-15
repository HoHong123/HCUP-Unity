#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 대상 오브젝트를 From 지점과 To 지점 사이에서 이동시키는 UI 엔티티입니다.
 *
 * 특징 / 지원기능 ::
 * + 두 지점은 좌표값이 아니라 Transform 으로 지정하고 월드 좌표로 이동합니다.
 * + reparentOnArrive 가 켜지면 도착 지점 Transform 의 자식으로 부모를 옮깁니다.
 *
 * 주의사항 ::
 * target / fromPoint / toPoint 는 모두 Inspector 에서 할당되어 있어야 합니다.
 * =========================================================
 */
#endif

using System;
using UnityEngine;
using DG.Tweening;
using HInspector;

namespace HUI.Entity {
    [Serializable]
    public class TransitUiEntity {
        #region Const
        const float DEFAULT_ANIMATION_DURATION = 0.2f;
        #endregion

        #region Fields
        [HTitle("Target")]
        [SerializeField]
        Transform target;

        [HTitle("Points")]
        [SerializeField]
        Transform fromPoint;
        [SerializeField]
        Transform toPoint;

        [HTitle("Option")]
        [Tooltip("Move the target under the arrival point transform.")]
        [SerializeField]
        bool reparentOnArrive = false;
        [SerializeField]
        bool useAnimation = false;
        [HShowIf(nameof(useAnimation))]
        [SerializeField]
        float animationDuration = DEFAULT_ANIMATION_DURATION;
        #endregion

        #region Public - Transit
        public void MoveToDestination(bool immediate = false) => _ApplyTransit(toPoint, immediate);
        public void MoveToOrigin(bool immediate = false) => _ApplyTransit(fromPoint, immediate);
        #endregion

        #region Private
        private bool _CanAnimate() {
            if (!useAnimation) return false;
            return target.gameObject.activeInHierarchy;
        }

        private void _ApplyTransit(Transform point, bool immediate) {
            target.DOKill();

            if (reparentOnArrive) {
                target.SetParent(point, worldPositionStays: true);
            }

            if (_CanAnimate() && !immediate) {
                target.DOMove(point.position, animationDuration).SetUpdate(true);
                return;
            }

            target.position = point.position;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.09.15 TransitUiEntity 베이스 코드 생성
 *
 * # 목적
 * - MovingUiEntity 는 원점 + 이동량(또는 절대 좌표)의 local 좌표 기반이라 서로 다른 부모 아래의 두 지점 사이 이동을 표현하지 못한다.
 * - 두 지점을 Transform 으로 받아 월드 좌표로 이동하고, 선택적으로 도착 지점으로 부모를 옮긴다.
 *
 * # 사용 흐름
 * - TransitOnClickButton / TransitOnSelectToggle 이 targets 배열로 보유한다.
 * - MoveToDestination 은 toPoint 로, MoveToOrigin 은 fromPoint 로 이동한다.
 *
 * # 설계 결정
 * - 부모 이전은 SetParent(worldPositionStays: true) 로 이동 시작 시점에 수행한다. 현재 월드 위치를 보존한 채 새 부모 기준으로 트윈하므로 시작 순간 튀지 않는다.
 * - 트윈은 DOMove(월드) 를 쓴다. 부모 이전 여부와 무관하게 같은 목표 좌표를 쓰기 위함이다.
 * - 도착 후 localPosition 은 0 으로 보정하지 않는다. 지점의 월드 위치로 이동하므로 결과상 동일하고, 트윈 중 부모가 움직이는 경우의 보정은 범위 밖이다.
 *
 * =============================================================================
 */
#endif
