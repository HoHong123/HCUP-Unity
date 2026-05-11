#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 다중 출구 라우팅 노드 (허브노드).
 *
 * 특징 ::
 * 입구 Port 1개 + 출구 Port N개 (사용자 정의 string 키 목록).
 * + 각 키는 하나의 출구 Port 에 1:1 대응.
 * + [HTitle("Output Ports")] — Inspector 키값 직접 수정 시 OnValidate → KeysChanged → HGraphHubNode 즉시 동기화.
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
    public class HubNode : BaseNode {
        #region Public - Clipboard
        public override string ClipboardMagic => "HGRAPH_HUB_NODE_V1";
        #endregion

        #region Nested Types
        [Serializable]
        public struct HubPortEntry {
            public string Key;
            public HubPortEntry(string key) { Key = key; }
        }
        #endregion

        #region Fields
        [HTitle("Output Ports")]
        [SerializeField]
        List<HubPortEntry> entries = new();
        #endregion

        #region Properties
        public IReadOnlyList<HubPortEntry> Entries => entries;
        public int PortCount => entries.Count;
        #endregion

        #region Internal - Entry Mutation (Editor Only)
#if UNITY_EDITOR
        internal event Action KeysChanged;
        private void OnValidate() => KeysChanged?.Invoke();

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
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.11 — HTitle 라벨 영문화
 *
 * # 변경
 * - [HTitle("출구 포트")] → [HTitle("Output Ports")].
 * - 헤더 주석의 라벨 인용 동기화.
 *
 * # 이유
 * - 전역 규칙: 코드/변수명은 영어. HTitle 인자는 화면 표시 라벨 + 코드 식별자 양쪽 성격.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.11 — [HReadOnly] 제거 + Inspector 키값 편집 즉시 동기화
 *
 * # 변경
 * - entries: [HReadOnly] 제거 — Inspector 직접 키값 수정 허용.
 * - KeysChanged (internal event Action) + OnValidate() 추가 (#if UNITY_EDITOR).
 *   Inspector 편집 완료 시 OnValidate → KeysChanged 발화 → HGraphHubNode 가 구독.
 *
 * # 이유
 * - [HReadOnly] 시절 키값 수정 채널이 GraphView body 버튼뿐 → UX 불편.
 * - OnValidate + event 패턴: SO → 뷰 의존 역전 없이 뷰가 구독/해제 관리.
 *   키값만 변경 시 CatalogMutated 전체 repopulate 없이 포트명 + 바디 부분 갱신.
 *
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
