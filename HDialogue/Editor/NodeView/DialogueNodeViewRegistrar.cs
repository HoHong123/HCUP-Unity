#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- HDialogue 노드 타입별 시각 팩토리 + 헤더 색 등록.
 *
 * 특징 / 지원기능 ::
 * + [InitializeOnLoadMethod] — 도메인 리로드 후 자동 재등록
 * + 9종 DialogueNode → 대응 HGraphNode 파생 클래스 팩토리
 * + DialogueStyleSheetLoader — HDialogueNode.uss 지연 로드·캐시
 *
 * 주의사항 ::
 * _Register 정적 메서드만 있는 파일. 다른 로직 추가 금지.
 * DialogueStyleSheetLoader._sheet 는 도메인 리로드 시 null 재설정됨 — 자동 복원.
 * =========================================================
 */
#endif

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using HWindows.Editor.NodeWindow;

namespace HDialogue.Editor {
    static class DialogueNodeViewRegistrar {
        [InitializeOnLoadMethod]
        static void _Register() {
            HGraphNodeStyles.RegisterHeaderColor(typeof(DialogueEntryNode),    new Color(0.35f, 0.35f, 0.35f));
            HGraphNodeStyles.RegisterHeaderColor(typeof(DialogueExitNode),     new Color(0.25f, 0.25f, 0.25f));
            HGraphNodeStyles.RegisterHeaderColor(typeof(DialogueLineNode),     new Color(0.18f, 0.40f, 0.72f));
            HGraphNodeStyles.RegisterHeaderColor(typeof(DialogueChoiceNode),   new Color(0.60f, 0.50f, 0.08f));
            HGraphNodeStyles.RegisterHeaderColor(typeof(DialogueBranchNode),   new Color(0.42f, 0.15f, 0.70f));
            HGraphNodeStyles.RegisterHeaderColor(typeof(DialogueEventNode),    new Color(0.15f, 0.52f, 0.18f));
            HGraphNodeStyles.RegisterHeaderColor(typeof(DialogueVariableNode), new Color(0.12f, 0.48f, 0.35f));
            HGraphNodeStyles.RegisterHeaderColor(typeof(DialogueWaitNode),     new Color(0.65f, 0.38f, 0.08f));
            HGraphNodeStyles.RegisterHeaderColor(typeof(DialogueCinematicNode), new Color(0.70f, 0.15f, 0.28f));

            HGraphCanvas.RegisterNodeViewFactory(typeof(DialogueEntryNode),
                (n, r) => new HGraphDialogueEntryNode((DialogueEntryNode)n, r));
            HGraphCanvas.RegisterNodeViewFactory(typeof(DialogueExitNode),
                (n, r) => new HGraphDialogueExitNode((DialogueExitNode)n, r));
            HGraphCanvas.RegisterNodeViewFactory(typeof(DialogueLineNode),
                (n, r) => new HGraphDialogueLineNode((DialogueLineNode)n, r));
            HGraphCanvas.RegisterNodeViewFactory(typeof(DialogueChoiceNode),
                (n, r) => new HGraphDialogueChoiceNode((DialogueChoiceNode)n, r));
            HGraphCanvas.RegisterNodeViewFactory(typeof(DialogueBranchNode),
                (n, r) => new HGraphDialogueBranchNode((DialogueBranchNode)n, r));
            HGraphCanvas.RegisterNodeViewFactory(typeof(DialogueEventNode),
                (n, r) => new HGraphDialogueEventNode((DialogueEventNode)n, r));
            HGraphCanvas.RegisterNodeViewFactory(typeof(DialogueVariableNode),
                (n, r) => new HGraphDialogueVariableNode((DialogueVariableNode)n, r));
            HGraphCanvas.RegisterNodeViewFactory(typeof(DialogueWaitNode),
                (n, r) => new HGraphDialogueWaitNode((DialogueWaitNode)n, r));
            HGraphCanvas.RegisterNodeViewFactory(typeof(DialogueCinematicNode),
                (n, r) => new HGraphDialogueCinematicNode((DialogueCinematicNode)n, r));
        }
    }

    internal static class DialogueStyleSheetLoader {
        static StyleSheet _sheet;

        internal static StyleSheet Get() {
            if (_sheet != null) return _sheet;
            string[] guids = AssetDatabase.FindAssets("t:StyleSheet HDialogueNode");
            if (guids.Length == 0) return null;
            _sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(guids[0]));
            return _sheet;
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.16 DialogueCinematicNode 팩토리 + 헤더 색 등록
 *
 * # 변경
 * - RegisterHeaderColor(DialogueCinematicNode): 딥 로즈 (0.70, 0.15, 0.28)
 * - RegisterNodeViewFactory(DialogueCinematicNode): HGraphDialogueCinematicNode
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueNodeViewRegistrar 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 5 — HDialogue 노드 타입별 시각 팩토리 + 헤더 색 HWindows에 주입
 *
 * # 설계 결정
 * - [InitializeOnLoadMethod]: 도메인 리로드마다 재등록. _externalFactories가 static 초기화라서 필수.
 * - DialogueStyleSheetLoader 내부 캐시: AssetDatabase.FindAssets는 호출 비용 있음 → _sheet null 확인 후 1회만 검색.
 * - internal 클래스: HGraphDialogue*Node 파일들이 같은 어셈블리 내에서 호출하는 전용 로더.
 *
 * =============================================================================
 */
#endif
