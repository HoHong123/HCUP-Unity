#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 대상 오브젝트를 From 지점과 To 지점 사이에서 이동시키는 UI 엔티티입니다.
 *
 * 특징 / 지원기능 ::
 * + 두 지점은 좌표값이 아니라 Transform 으로 지정하고 월드 좌표로 이동합니다.
 * + fromPoint 가 비어 있으면 첫 이동 직전의 부모와 위치를 원점으로 기억해 From 으로 사용합니다.
 * + reparentOnArrive 가 켜지면 도착 지점 Transform 의 자식으로 부모를 옮깁니다.
 *
 * 주의사항 ::
 * target / toPoint 는 반드시 할당해야 합니다. toPoint 가 비어 있으면 이동 시 InvalidOperationException 을 던집니다.
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
        [Tooltip("Optional. When empty, the position before the first transit is used as origin.")]
        [SerializeField]
        Transform fromPoint;
        [HRequired("Assign the destination transform.")]
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

        bool hasCapturedOrigin;
        Transform capturedOriginParent;
        Vector3 capturedOriginLocalPosition;
        #endregion

        #region Public - Transit
        public void MoveToDestination(bool immediate = false) {
            if (toPoint == null) {
                throw new InvalidOperationException(
                    "[TransitUiEntity] toPoint is not assigned. Assign the destination Transform in the Inspector.");
            }

            _CaptureOriginOnce();
            _ApplyTransit(toPoint, toPoint.position, immediate);
        }

        public void MoveToOrigin(bool immediate = false) {
            if (fromPoint != null) {
                _ApplyTransit(fromPoint, fromPoint.position, immediate);
                return;
            }

            _CaptureOriginOnce();
            _ApplyTransit(capturedOriginParent, _GetCapturedOriginWorldPosition(), immediate);
        }
        #endregion

        #region Private
        private Vector3 _GetCapturedOriginWorldPosition() {
            if (capturedOriginParent == null) return capturedOriginLocalPosition;
            return capturedOriginParent.TransformPoint(capturedOriginLocalPosition);
        }

        private void _CaptureOriginOnce() {
            if (fromPoint != null || hasCapturedOrigin) return;

            capturedOriginParent = target.parent;
            capturedOriginLocalPosition = target.localPosition;
            hasCapturedOrigin = true;
        }

        private bool _CanAnimate() {
            if (!useAnimation) return false;
            return target.gameObject.activeInHierarchy;
        }

        private void _ApplyTransit(Transform arrivalParent, Vector3 worldPosition, bool immediate) {
            target.DOKill();

            if (reparentOnArrive) {
                target.SetParent(arrivalParent, worldPositionStays: true);
            }

            if (_CanAnimate() && !immediate) {
                target.DOMove(worldPosition, animationDuration).SetUpdate(true);
                return;
            }

            target.position = worldPosition;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.09.15 fromPoint 선택화, toPoint 필수화
 *
 * # 변경
 * - fromPoint 가 null 이면 첫 이동 직전의 부모와 localPosition 을 원점으로 기억하고, MoveToOrigin 은 그 원점으로 돌아간다.
 * - toPoint 에 [HRequired] 를 붙이고, null 인 채 MoveToDestination 을 호출하면 InvalidOperationException 을 던진다.
 * - _ApplyTransit 이 Transform 대신 (도착 부모, 월드 좌표) 를 받도록 바꿨다. 기억한 원점은 Transform 이 아니기 때문이다.
 *
 * # 설계 결정
 * - 원점은 1회만 기억한다. 매 MoveToDestination 마다 기억하면 To 에 있는 상태에서 다시 호출될 때(Toggle 재활성 동기화 등) 원점이 To 로 덮인다.
 * - 원점을 월드 좌표가 아니라 (부모, localPosition) 으로 기억한다. 레이아웃 변화로 부모가 움직여도 복귀 좌표가 따라가고, reparentOnArrive 시 복귀할 부모도 같은 값으로 얻는다.
 * - MoveToOrigin 이 먼저 불려도(Toggle 이 isOn=false 로 활성화) 그 시점 위치를 원점으로 기억하므로 제자리 이동이 되고 이후 동작이 일관된다.
 * - 원래 부모가 null(루트)이면 SetParent(null) 로 루트에 복귀하고 localPosition 을 월드 좌표로 그대로 쓴다.
 *
 * # 주의
 * - 원점은 인스턴스 수명 동안 고정된다. 이동 전에 대상을 다른 곳으로 옮겨도 이미 기억한 원점은 갱신되지 않는다.
 *
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
