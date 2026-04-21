using HDiagnosis.Logger;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace HWindows.Editor.NodeWindow {
    public sealed class HGraphCanvas : GraphView {
        private const string USS_ASSET_NAME = "HGraphWindow";

        public HGraphCanvas() {
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            GridBackground grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            _LoadStyleSheet();

            this.StretchToParentSize();
        }

        private void _LoadStyleSheet() {
            string[] guids = AssetDatabase.FindAssets($"t:StyleSheet {USS_ASSET_NAME}");
            if (guids.Length == 0) {
                HLogger.Warning(
                    $"[HWindows.NodeWindow] StyleSheet '{USS_ASSET_NAME}' not found in project. " +
                    "Grid/style will fall back to GraphView defaults. " +
                    "Verify USS file presence or asset name.");
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (sheet == null) {
                HLogger.Warning(
                    $"[HWindows.NodeWindow] StyleSheet at '{path}' failed to load. " +
                    "Grid/style will fall back to GraphView defaults.");
                return;
            }

            styleSheets.Add(sheet);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Dev Log
// ─────────────────────────────────────────────────────────────────────────────
// 2026-04-21 · USS 로드 전략 메모
//
//   [현재 방식] AssetDatabase.FindAssets($"t:StyleSheet {USS_ASSET_NAME}")
//              · 이름 기반 검색. 경로/리네임/UPM 이전에 전부 생존.
//              · L1 베이스 수준에서 충분히 견고함.
//
//   [추후 전환 요청] Option 3 — 고정 GUID 상수 방식
//              · 왜 전환 필요:
//                 1. HWindows 내 USS 자산이 2개 이상으로 늘어날 때 동명 충돌 방지 필요
//                 2. 자산 참조 계약을 엄격화(리뷰 게이트, 계약적 참조)해야 할 때
//                 3. 프로덕션 품질 수준에서 Unity 자산 시스템과 정합(GUID는 일등 시민)
//              · 전환 절차:
//                 (a) HGraphWindow.uss.meta 에서 guid 값 확인
//                 (b) private const string USS_GUID = "<해당 guid>"; 로 교체
//                 (c) _LoadStyleSheet 내부 FindAssets 호출을
//                     AssetDatabase.GUIDToAssetPath(USS_GUID) 로 치환
//                 (d) USS_ASSET_NAME 상수 제거
//              · 장점: 동명 자산 애매성 0, 리네임·이동 완전 무관, 계약 명시적.
//              · 단점: .meta 재생성 등 드문 상황에서 GUID 수동 업데이트 필요.
// ─────────────────────────────────────────────────────────────────────────────
