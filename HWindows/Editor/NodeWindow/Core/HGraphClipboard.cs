using System;
using System.Collections.Generic;
using HWindows.NodeWindow;
using UnityEngine;

namespace HWindows.Editor.NodeWindow {
    public static class HGraphClipboard {
        #region Const
        // 도메인별 magic — wrapper 의 magic 은 BaseNode.ClipboardMagic 에서 주입.
        // prefix + version sep 로 우리 형식 식별 (다른 도구의 우연 schema 충돌 차단).
        // 명명 규칙: "HGRAPH_<DOMAIN>_NODE_V<N>" — 예: HGRAPH_SIMPLE_NODE_V1.
        const string MAGIC_PREFIX = "HGRAPH_";
        const string MAGIC_VERSION_SEP = "_V";
        const int VERSION = 1;
        #endregion

        #region Types
        // 단일 노드의 직렬 데이터 묶음. typeName 으로 도메인 타입 동적 생성, nodeJson 으로 필드 복원.
        // layout / foldoutOpen / openSize 는 catalog 의 Editor-only 보조 맵 데이터.
        [Serializable]
        public struct Entry {
            public string typeName;
            public string nodeJson;
            public Vector2 layout;
            public bool foldoutOpen;
            public Vector2 openSize;
        }

        // 클립보드 wrapper. magic + version 으로 다른 형식 클립보드 거부.
        [Serializable]
        public struct Payload {
            public string magic;
            public int version;
            public Entry[] entries;
        }
        #endregion

        #region Public - Serialize
        // 노드 리스트 → JSON wrapper. catalog 가 null 이면 보조 맵 (layout/foldout/openSize) 은 default.
        // selection 의 ClipboardMagic 일관성 검사 — mixed 도메인 시 null 반환 (caller Warning 책임).
        public static string Serialize(NodeCatalogSO catalog, IReadOnlyList<BaseNode> nodes) {
            if (nodes == null || nodes.Count == 0) return null;

            List<BaseNode> validNodes = new List<BaseNode>(nodes.Count);
            for (int k = 0; k < nodes.Count; k++) {
                if (nodes[k] != null) validNodes.Add(nodes[k]);
            }
            if (validNodes.Count == 0) return null;

            // 도메인 magic 일관성 검사. 다 같은 ClipboardMagic 이어야 wrapper 한 개로 직렬화 가능.
            string magic = validNodes[0].ClipboardMagic;
            for (int k = 1; k < validNodes.Count; k++) {
                if (validNodes[k].ClipboardMagic != magic) return null;
            }

            Entry[] entries = new Entry[validNodes.Count];
            for (int k = 0; k < validNodes.Count; k++) {
                BaseNode n = validNodes[k];
                // Phase 1-F: 에디터 상태(layout/foldout/openSize)는 node 자체 필드에서 읽음.
                // nodeJson(JsonUtility.ToJson) 에 이미 포함되어 _RestoreFromEntry 의
                // FromJsonOverwrite 가 자동 복원. entry 필드는 명시적 접근용으로 유지.
                Entry e = new Entry {
                    typeName = n.GetType().AssemblyQualifiedName,
                    nodeJson = JsonUtility.ToJson(n),
                    layout = n.EditorPosition,
                    foldoutOpen = n.EditorFoldoutOpen,
                    openSize = n.EditorOpenSize,
                };
                entries[k] = e;
            }

            Payload payload = new Payload {
                magic = magic,
                version = VERSION,
                entries = entries,
            };
            return JsonUtility.ToJson(payload);
        }
        #endregion

        #region Public - Parse / Validate
        // JSON 파싱 + magic 패턴 (prefix HGRAPH_ + suffix _V<숫자>) + version + entries 검증.
        // 어떤 도메인 magic 이든 패턴 정합 시 통과 — 도메인 호환성 검증은 PasteNodes 의 typeName 단계.
        public static bool TryParse(string json, out Payload payload) {
            payload = default;
            if (string.IsNullOrEmpty(json)) return false;

            try {
                payload = JsonUtility.FromJson<Payload>(json);
            }
            catch {
                return false;
            }

            if (string.IsNullOrEmpty(payload.magic)) return false;
            if (!payload.magic.StartsWith(MAGIC_PREFIX)) return false;
            int sepIdx = payload.magic.LastIndexOf(MAGIC_VERSION_SEP);
            if (sepIdx < 0) return false;
            string versionPart = payload.magic.Substring(sepIdx + MAGIC_VERSION_SEP.Length);
            if (!int.TryParse(versionPart, out _)) return false;

            if (payload.version != VERSION) return false;
            if (payload.entries == null) return false;
            return true;
        }

        // 메뉴 활성/비활성 검사용. TryParse 호출로 비용 균등 — 일반 텍스트 클립보드는 빠른 실패.
        public static bool IsValid(string json) {
            return TryParse(json, out _);
        }
        #endregion
    }
}

#if UNITY_EDITOR
// =============================================================================
// Dev Log
// =============================================================================
// @Jason - PKH 2026.05.09 Phase 1-F — Serialize 에디터 상태 읽기 이관 (catalog → BaseNode)
//
// # 변경
// - Serialize: catalog.EditorNodeLayouts / FoldoutOpen / OpenSizes dict 읽기
//   → n.EditorPosition / n.EditorFoldoutOpen / n.EditorOpenSize 직접 읽기
// - catalog 파라미터: 보조 맵 읽기 제거 후 미사용 상태이나 미래 확장용으로 시그니처 유지
//
// # 이유
// - NodeCatalogSO Phase 1-F 에서 editor HDictionary 3개 제거에 따른 소비처 갱신.
//
// =============================================================================
// @Jason - PKH 2026-05-07 HGraphClipboard 의 역할 - Cut/Paste 직렬화 + 검증 단일 게이트
//
//   [역할]
//   - 노드(들) ↔ 클립보드 JSON 변환 + magic/version 검증 단일 진입점.
//   - GUIUtility.systemCopyBuffer 입출력은 호출자(NodeCatalogAuthor 또는 HGraphNode/Canvas) 책임.
//
//   [JSON 형식 - 도메인별 magic (HGRAPH_<DOMAIN>_NODE_V1)]
//   {
//     "magic": "HGRAPH_<DOMAIN>_NODE_V1",
//     "version": 1,
//     "entries": [
//       { "typeName": "...AssemblyQualifiedName...",
//         "nodeJson": "{...JsonUtility.ToJson result...}",
//         "layout": {"x":..,"y":..},
//         "foldoutOpen": bool,
//         "openSize": {"x":..,"y":..} }
//     ]
//   }
//
//   [어댑터 경계 (P1-3 / P1B-3)]
//   - GraphView (UnityEditor.Experimental.GraphView) 미참조. HGraphCanvas + HGraphNode 2 파일 한정 유지.
//   - 본 파일은 BaseNode + NodeCatalogSO + JsonUtility 만 의존. Editor asmdef 안에서 단순 유틸.
//
//   [JsonUtility 직렬화 보장]
//   - BaseNode 의 [SerializeField] uid / title 자동 직렬화 (Phase 0 P0-c).
//   - 도메인 서브의 추가 [SerializeField] 필드도 자동 포함.
//   - SerializeReference 필드는 JsonUtility 미지원 — BaseNode 에 그런 필드 없음.
//   - typeName = AssemblyQualifiedName (Q5-A) → HCUP 패키지 분리 환경에서도 Type.GetType 정확 동작.
//
//   [도메인별 magic 정책 - 사용자 결정 (2026-05-08)]
//   - "노드는 추후 다양한 형태로 확장될 것이기에 개별 매직 스트링이 필요" - 사용자 spec.
//   - SimpleNode → "HGRAPH_SIMPLE_NODE_V1", DialogueNode → "HGRAPH_DIALOGUE_NODE_V1" 등.
//   - Wrapper magic = BaseNode.ClipboardMagic 의 값 (도메인 서브 override).
//   - Mixed 도메인 selection 거부 — Serialize 가 ClipboardMagic 일관성 검사 후 null 반환.
//   - TryParse 의 magic 검증은 prefix HGRAPH_ + suffix _V<숫자> 패턴 — 어떤 도메인 magic 이든 통과.
//   + 실 도메인 호환성 (typeName 검증) 은 PasteNodes 의 _RestoreFromEntry 에서 별도 처리.
//
//   [Copy 와 Cut 의 통합 - 사용자 결정 (2026-05-08)]
//   - 둘 다 같은 형식 직렬 (HGraphClipboard.Serialize) → 같은 형식 클립보드 → Paste 가 양쪽 처리.
//   - 차이는 "원본 유지 vs 제거" 한 가지뿐 — 다른 catalog 로 데이터 이전 호환 보장.
//   - milestone §1-2-2 의 ToString 형식은 ClipboardMagic 도입으로 폐기 (BaseNode.GetCopyText 제거).
//
//   [UID 처리 정책 - 사용자 결정 (2026-05-08, 이전 결정 폐기)]
//   - 이전 spec ("UID 까지 복사" / Q3-A "UID 충돌 시 거부") 폐기.
//   - 변경 — Paste 시 항상 NodeUIDRegistry.instance.Issue() 새 UID 발급.
//   + 두 catalog 가 같은 UID 노드 보유 가능한 환경에서 충돌 회피 + 데이터 무결성 우선.
//   + Cut 의 "이동" 의미는 "데이터 복사 + 원본 제거" 로 재정의 — UID 자체는 보존 X.
//   + 원본 title 은 보존 (사용자 의도 존중). 본 클래스의 entry.layout/foldoutOpen/openSize 는 그대로.
//
//   [version 갱신 정책]
//   - format 변경 시 VERSION 증가 + TryParse 가 이전 version 거부 (또는 마이그레이션 분기).
//   - V1 → V2 시 magic 도 같이 갱신 권장 ("HGRAPH_CUT_V2") — TryParse 한 번에 거부 가능.
//
//   [성능 검토]
//   - IsValid 가 매 메뉴 빌드 시 호출 — 일반 텍스트 클립보드는 magic 검사 즉시 실패라 ms 미만.
//   - 큰 클립보드 (수십 KB) JSON 파싱도 ms 단위. 메뉴 빌드 빈도 고려 시 무시 가능.
//
//   [의도된 비스코프]
//   - 엣지 직렬화 (Cut 시 양 끝 노드 간 엣지 보존) — Phase 2 엣지 시각화 후 결정.
//   - 클립보드 history / stack — OS 클립보드 외 자체 stack 유지 X.
// =============================================================================
#endif
