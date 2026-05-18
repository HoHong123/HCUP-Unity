#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대기 노드. 조건 충족 또는 시간 경과 후 다음 노드로 진행.
 *
 * 특징 / 지원기능 ::
 * + WaitMode.Time      - seconds 초 대기 후 자동 진행
 * + WaitMode.Condition - DialogueDirector.NotifyWaitConditionMet() 신호 대기
 * + WaitMode.UserInput - OnAdvanceRequested 1회 수신 후 진행
 *
 * 주의사항 ::
 * Condition 모드: 외부에서 NotifyWaitConditionMet()를 호출하지 않으면 영구 블로킹.
 * =========================================================
 */
#endif

using HInspector;
using HWindows.NodeWindow;
using UnityEngine;

namespace HDialogue {
    public sealed class DialogueWaitNode : BaseNode {
        #region Fields
        [HTitle("Wait")]
        [SerializeField]
        WaitMode mode;
        [SerializeField]
        float seconds = 1f;
        [SerializeField]
        string conditionKey;
        #endregion

        #region Properties
        public WaitMode Mode => mode;
        public float Seconds => seconds;
        public string ConditionKey => conditionKey;
        #endregion

        public override string ClipboardMagic => "HGRAPH_DIALOGUE_WAIT_NODE_V1";
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueWaitNode 베이스 코드 생성
 *
 * # 목적
 * - 그래프 흐름 일시 정지 노드 — HCUP-2.3.0 Phase 1
 *
 * # 설계 결정
 * - seconds 기본값 1f: Time 모드 즉시 사용 가능
 * - conditionKey: Condition 모드 전용. 다른 모드에서는 무시
 *
 * # 사용 흐름
 * - DialogueDirector._ProcessWaitNode
 *   Time: UniTask.Delay(TimeSpan.FromSeconds(seconds), ct)
 *   Condition: TaskCompletionSource await + NotifyWaitConditionMet()로 해제
 *   UserInput: OnAdvanceRequested 1회 수신 대기
 *
 * =============================================================================
 */
#endif
