#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 노드 그래프의 고유 ID (GUID string wrapper).
 *
 * 특징 ::
 * Guid.NewGuid().ToString("N") 32자 hex를 string guid 필드로 보관.
 * + NodeUID.New() 정적 팩토리로 발급 — 중앙 레지스트리 불필요.
 * + [Serializable] struct: HDictionary<NodeUID, BaseNode> Key 직렬화 대상.
 *
 * 주의사항 ::
 * None = string.Empty. IsValid = !string.IsNullOrEmpty(guid).
 * + guid 필드는 [HideInInspector] — Inspector 표시는 NodeUIDDrawer 가 전담.
 * + Value getter는 guid ?? string.Empty 로 null-safe 반환.
 * =========================================================
 */
#endif
using System;
using UnityEngine;

namespace HWindows.NodeWindow.Identity {
    [Serializable]
    public struct NodeUID : IEquatable<NodeUID> {
        #region Fields
        [SerializeField, HideInInspector]
        string guid;
        #endregion

        #region Properties
        public string Value => guid ?? string.Empty;
        public static NodeUID None => new(string.Empty);
        public bool IsValid => !string.IsNullOrEmpty(guid);
        #endregion

        #region Constructors
        public NodeUID(string guid) {
            this.guid = guid;
        }
        #endregion

        #region Public - Factory
        public static NodeUID New() => new(Guid.NewGuid().ToString("N"));
        #endregion

        #region Public - Equals
        public bool Equals(NodeUID other) => string.Equals(guid, other.guid, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is NodeUID other && Equals(other);
        public override int GetHashCode() => guid?.GetHashCode() ?? 0;
        public override string ToString() => $"NodeUID({(string.IsNullOrEmpty(guid) ? "None" : guid)})";
        #endregion

        #region Public - Operators
        public static bool operator ==(NodeUID a, NodeUID b) => a.Equals(b);
        public static bool operator !=(NodeUID a, NodeUID b) => !a.Equals(b);
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.09 guid 필드 [HReadOnly] → [HideInInspector] + NodeUIDDrawer 패턴 도입
 *
 * # 변경
 * - [SerializeField, HReadOnly] → [SerializeField, HideInInspector]
 * - using HInspector 제거 (HReadOnly 미사용)
 * - 헤더 주의사항 섹션에 NodeUIDDrawer 전담 명시
 *
 * # 이유
 * - NodeUID(struct)는 propertyType == Generic + hasVisibleChildren → HDictionaryDrawer 가
 *   컨테이너 경로(_DrawContainerCell)로 라우팅해 foldout 박스 렌더.
 * - [HideInInspector]로 guid 자식을 invisible → hasVisibleChildren == false →
 *   _IsContainerType == false → simple cell 경로(_DrawSimpleCell) → NodeUIDDrawer 호출.
 * - NodeUIDDrawer(CustomPropertyDrawer<NodeUID>)가 32자 GUID 한 줄 SelectableLabel 표시.
 * - HDictionaryDrawer 코드 변경 없이 6 지점(rootUID/uid/nodes key/3 editor HDicts) 전부 해결.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.09 NodeUID int → string GUID 전환
 *
 * # 변경
 * - int value 필드 → string guid 필드로 교체 (Guid.NewGuid().ToString("N"))
 * - NodeUID.New() 정적 팩토리 추가 (NodeUIDRegistry.Issue() 완전 대체)
 * - implicit operator int / NodeUID(int) 제거
 * - None = string.Empty, IsValid = !string.IsNullOrEmpty(guid)
 * - GetHashCode = guid?.GetHashCode() ?? 0 (string 기반)
 *
 * # 이유
 * - int + 중앙 NodeUIDRegistry 방식: 동시 다중 브랜치에서 nextValue 충돌 필연
 * - GUID 방식: 충돌 확률 ≈ 0 (UUID v4 기준 2^122), 레지스트리 삭제로 브랜치 충돌 원천 제거
 *
 * =============================================================================
 */
// =============================================================================
// (이하 이전 엔트리 — 원래 형식 보존)
// =============================================================================
// @Jason - PKH 2026-04-22 NodeUID 의 역할 - 노드의 변경 불가 정체성 (GUID string wrapper)
//
//   [역할]
//   - 노드 그래프 시스템의 unique identity. 128-bit GUID를 "N" format string 으로 보관.
//   - [Serializable] struct + IEquatable 로 값 의미체계 유지.
//
//   [설계 근거]
//   - HUtil 의 DataOwnerId 관용(readonly struct + Interlocked 발급) 패턴에서 출발했으나
//     의미 분리 + 중앙 카운터 제거를 위해 GUID 방식으로 전환.
//   + [Serializable] 유지: HDictionary<NodeUID, BaseNode> Key 직렬화 필수.
//
//   [값 규칙]
//   - guid != null && guid != "": 유효한 NodeUID (IsValid == true)
//   - guid == null || guid == "": NodeUID.None (IsValid == false)
//
//   [발급 책임]
//   - NodeUID.New() 정적 팩토리 — 어디서든 충돌 없이 발급 가능.
//   - 중앙 Registry 제거로 분산 브랜치 환경에서 UID 충돌 원천 차단.
//   - 삭제된 노드의 UID 재발급 불가 — GUID 고유성으로 구조적 보장.
//
//   [확장 노트] 향후 평행 식별자 (예: SubGraphUID) 추가 시 같은 패턴 복제.
//   EdgeUID 는 검토 후 폐기 — parallel edge 금지로 (branch, leaf) pair 가 곧 ID.
//
//   [Phase 1-A 직렬화 결함 - 2026-04-24]
//   - readonly struct + readonly field + [SerializeField] 조합의 직렬화 결함으로
//     readonly 를 제거한 이력 있음. 현재 string guid 는 mutable field 라 해당 없음.
//
//   [Phase 1-A→E int 기반 설계 폐기 이유 - 2026-05-09]
//   - 기존 int + NodeUIDRegistry(ProjectSettings/NodeUIDRegistry.asset) 방식은
//     동시 다중 브랜치 환경에서 같은 nextValue 가 서로 다른 노드에 발급될 수 있음.
//   + GUID 방식으로 전환 — NodeUID.New() = Guid.NewGuid().ToString("N").
//   + 충돌 확률 = 0 (UUID v4 기준 2^122 경우의 수).
//   + NodeUIDRegistry 및 ProjectSettings/NodeUIDRegistry.asset 삭제.
//   + YAML 기존 에셋은 value: int → guid: string 마이그레이션 (Clean break).
// =============================================================================
#endif
