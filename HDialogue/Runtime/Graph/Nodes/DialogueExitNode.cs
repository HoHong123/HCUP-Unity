#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대화 그래프 종료 노드.
 *
 * 특징 / 지원기능 ::
 * + ExitKey          - OnCatalogExit(catalog, exitKey) 발행용 식별자
 * + ClearStageOnExit - true이면 CharacterStageDirector.ClearAll 호출
 *
 * 주의사항 ::
 * 하나의 카탈로그에 ExitNode가 여러 개 있어도 무방. exitKey로 구분.
 * ClearAll 호출은 DialogueDirector가 StageDirector에 위임 (null-safe).
 * =========================================================
 */
#endif

using HInspector;
using HWindows.NodeWindow;
using UnityEngine;

namespace HDialogue {
    public sealed class DialogueExitNode : BaseNode {
        #region Fields
        [HTitle("Exit")]
        [SerializeField]
        string exitKey;
        [SerializeField]
        bool clearStageOnExit;
        #endregion

        #region Properties
        public string ExitKey => exitKey;
        public bool ClearStageOnExit => clearStageOnExit;
        #endregion

        public override string ClipboardMagic => "HGRAPH_DIALOGUE_EXIT_NODE_V1";
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueExitNode 베이스 코드 생성
 *
 * # 목적
 * - 대화 그래프 종료점 — HCUP-2.3.0 Phase 1
 *
 * # 사용 흐름
 * - DialogueDirector._ProcessExitNode → OnCatalogExit 발행 → Finished 전이
 * - ClearStageOnExit이면 stageDirector?.ClearAll(PortraitTransition.Fade(0.3f))
 *
 * =============================================================================
 */
#endif
