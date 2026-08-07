#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueLineNode 전용 GraphView 시각 노드.
 *
 * 특징 / 지원기능 ::
 * + hdialogue-line-node CSS (파랑 테두리)
 * + 바디: 화자키 + 방향 화살표 + 텍스트 미리보기 + 슬롯/포즈 + 포트레이트 스트립
 *
 * 주의사항 ::
 * DialogueLineNodePreviewDrawer.Build — registry null 시 포트레이트 스트립 생략.
 * registry는 노드가 속한 DialogueCatalogSO 에셋에서 조회한다 (_ResolveRegistry).
 * =========================================================
 */
#endif

using UnityEditor;
using UnityEngine.UIElements;
using HWindows.Editor.NodeWindow;
using HWindows.NodeWindow;

namespace HDialogue.Editor {
    public sealed class HGraphDialogueLineNode : HGraphNode {
        public HGraphDialogueLineNode(DialogueLineNode data, bool isRoot = false) : base(data, isRoot) {
            AddToClassList("hdialogue-line-node");
            _AddDialogueStyleSheet();
            bodyArea.Clear();
            CharacterRegistrySO registry = _ResolveRegistry(data);
            DialogueLineNodePreviewDrawer.Build(data, bodyArea, registry);
        }

        private void _AddDialogueStyleSheet() {
            StyleSheet sheet = DialogueStyleSheetLoader.Get();
            if (sheet != null) styleSheets.Add(sheet);
        }

        // registry 없이 호출하면 DialogueLinePortraitTimelineBuilder 전체와 포트레이트 스트립이
        // registry != null 가드에 막혀 도달 불가 코드가 된다 — 노드가 속한 카탈로그에서 조회한다.
        private static CharacterRegistrySO _ResolveRegistry(DialogueLineNode node) {
            if (node == null) return null;
            string assetPath = AssetDatabase.GetAssetPath(node);
            if (string.IsNullOrEmpty(assetPath)) return null;
            DialogueCatalogSO catalog = AssetDatabase.LoadAssetAtPath<DialogueCatalogSO>(assetPath);
            return catalog != null ? catalog.Registry : null;
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.08.07 (수정) :: 생성자에서 카탈로그 Registry 조회 후 Build 에 전달
 *
 * # 변경
 * - `_ResolveRegistry(DialogueLineNode)` 헬퍼 추가 — AssetDatabase 로 노드가 속한
 *   `DialogueCatalogSO` 를 찾아 `Registry` 프로퍼티를 반환.
 * - `DialogueLineNodePreviewDrawer.Build(data, bodyArea)` (2인자) →
 *   `Build(data, bodyArea, registry)` (3인자)로 호출 변경.
 *
 * # 이유
 * - 유일한 호출처가 registry 를 넘기지 않아 `DialogueLineNodePreviewDrawer.Build` 의
 *   `registry != null` 가드에 막혀 `DialogueLinePortraitTimelineBuilder` 전체와
 *   `_BuildPortraitStrip` 이 도달 불가 코드였다.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 HGraphDialogueLineNode 베이스 코드 생성
 * - HCUP-2.3.0 Phase 5 — DialogueLineNode 전용 시각 노드
 * =============================================================================
 */
#endif
