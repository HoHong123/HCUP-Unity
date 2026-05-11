#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- HubNode 전용 엣지 — 출구 포트 키값 보유.
 *
 * 특징 ::
 * BaseNodeEdge 에 branchPortKey(string) 추가.
 * + HubNode 의 어느 출구 Port 에 연결된 엣지인지 식별용.
 * + ConnectEdge<TEdge> new() 제약 충족.
 * =========================================================
 */
#endif
using System;
using UnityEngine;

namespace HWindows.NodeWindow {
    [Serializable]
    public sealed class HubNodeEdge : BaseNodeEdge {
        #region Fields
        [SerializeField]
        string branchPortKey;
        #endregion

        #region Properties
        public string BranchPortKey => branchPortKey;
        #endregion

        #region Internal - Key Assignment (Editor Only)
#if UNITY_EDITOR
        internal void SetPortKey(string key) {
            branchPortKey = key;
        }
#endif
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.10 Phase 3+ — HubNodeEdge 베이스 코드 생성
 *
 * # 목적
 * - HubNode 출구 Port 와 연결된 엣지의 portKey 를 직렬화 보관.
 * - repopulate 시 key → portIndex 매핑으로 올바른 Port 에 시각 연결.
 *
 * # 사용 흐름
 * - NodeCatalogAuthor.ConnectHubEdge(catalog, branch, leaf, portKey) 가 생성.
 * - HGraphCanvas._PopulateInternal: edge is HubNodeEdge → GetOutputPortByKey(key) 조회.
 *
 * =============================================================================
 */
#endif
