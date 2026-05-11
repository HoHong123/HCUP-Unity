#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 다중 출구 라우팅 노드 (허브노드).
 *
 * 특징 ::
 * 입구 Port 1개 + 출구 Port N개 (사용자 정의 string 키 목록).
 * + 각 키는 하나의 출구 Port 에 1:1 대응.
 * + [HTitle("출구 포트")] + [HReadOnly] — GraphView body 의 Add/Remove 버튼이 정식 채널.
 *
 * 주의사항 ::
 * 키값 범위 / 규칙 / 의미는 사용자 정의 — 시스템이 검증하지 않음.
 * + 키 추가/삭제 → Author.AddHubEntry / RemoveHubEntry 를 통해야 CatalogMutated 발화.
 * =========================================================
 */
#endif
using System;
using System.Collections.Generic;
using HInspector;
using UnityEngine;

namespace HWindows.NodeWindow {
    [Serializable]
    public struct HubPortEntry {
        public string Key;
        public HubPortEntry(string key) { Key = key; }
    }

    public class HubNode : BaseNode {
        #region Fields
        [HTitle("출구 포트")]
        [HReadOnly]
        [SerializeField]
        List<HubPortEntry> entries = new();
        #endregion

        #region Properties
        public IReadOnlyList<HubPortEntry> Entries => entries;
        public int PortCount => entries.Count;
        #endregion

        #region Internal - Entry Mutation (Editor Only)
#if UNITY_EDITOR
        internal void AddEntry(string key) {
            entries.Add(new HubPortEntry(key));
        }

        internal void RemoveEntry(int index) {
            if (index >= 0 && index < entries.Count)
                entries.RemoveAt(index);
        }

        internal void SetEntryKey(int index, string key) {
            if (index >= 0 && index < entries.Count)
                entries[index] = new HubPortEntry(key);
        }
#endif
        #endregion

        #region Public - Clipboard
        public override string ClipboardMagic => "HGRAPH_HUB_NODE_V1";
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.10 — Inspector HTitle + HReadOnly 추가
 *
 * # 변경
 * - using HInspector 추가.
 * - entries: [HTitle("출구 포트")] + [HReadOnly].
 *
 * # 이유
 * - entries 직접 Inspector 편집 시 CatalogMutated 미발화 → canvas repopulate 미실행.
 *   Add/Remove 는 Author.AddHubEntry / RemoveHubEntry (GraphView body 버튼) 경유 필수.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.10 Phase 3+ HubNode — 허브노드 베이스 코드 생성
 *
 * # 목적
 * - CatalogNode 에서 다중 출구 Port 기능 분리. HubNode 가 라우팅 전담.
 * - 입구 1개 + 출구 N개 (List<HubPortEntry> 키 목록 기반).
 *
 * # 사용 흐름
 * - NodeCatalogAuthor.CreateHubNode → HubNode 생성.
 * - Author.AddHubEntry / RemoveHubEntry → entries 조작 → CatalogMutated → repopulate.
 * - HGraphCanvas._PopulateInternal: HubNode 검출 → EnsureOutputPorts(PortCount).
 * - HGraphCanvas._OnGraphViewChanged: HubNode output port 사용 → HubNodeEdge(portKey) 생성.
 *
 * =============================================================================
 */
#endif
