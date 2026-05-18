#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 외부 이벤트 발행 노드.
 *
 * 특징 / 지원기능 ::
 * + EventKey / EventArg — DialogueDirector.OnEventFired(key, arg) 발행
 * + 즉시 통과 (1프레임 yield) — 대기 없음
 * + portrait.* 인라인 이벤트는 <event=...> 태그 경유, 이 노드는 그래프 레벨 이벤트
 *
 * 주의사항 ::
 * portrait.* 포트레이트 제어는 DialogueTextController.OnEventTagFired로 처리.
 * 이 노드의 EventKey는 그 외 커스텀 게임 이벤트에 사용할 것.
 * =========================================================
 */
#endif

using HInspector;
using HWindows.NodeWindow;
using UnityEngine;

namespace HDialogue {
    public sealed class DialogueEventNode : BaseNode {
        #region Fields
        [HTitle("Event")]
        [SerializeField]
        string eventKey;
        [SerializeField]
        string eventArg;
        #endregion

        #region Properties
        public string EventKey => eventKey;
        public string EventArg => eventArg;
        #endregion

        public override string ClipboardMagic => "HGRAPH_DIALOGUE_EVENT_NODE_V1";
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueEventNode 베이스 코드 생성
 *
 * # 목적
 * - 그래프 레벨 커스텀 이벤트 발행 — HCUP-2.3.0 Phase 1
 *
 * # 사용 흐름
 * - DialogueDirector._ProcessEventNode → OnEventFired(EventKey, EventArg) → 즉시 통과
 * - 외부 시스템이 OnEventFired 구독해 반응 (씬 전환, 퀘스트 완료 등)
 *
 * =============================================================================
 */
#endif
