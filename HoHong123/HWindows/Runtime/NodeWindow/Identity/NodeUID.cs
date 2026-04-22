using System;
using UnityEngine;

namespace HWindows.NodeWindow.Identity {
    [Serializable]
    public readonly struct NodeUID : IEquatable<NodeUID> {
        #region Fields
        [SerializeField]
        readonly int value;
        #endregion

        #region Properties
        public int Value => value;
        public static NodeUID None => new(0);
        public bool IsValid => value > 0;
        #endregion

        #region Constructors
        public NodeUID(int value) {
            this.value = value;
        }
        #endregion

        #region Public - Equals
        public bool Equals(NodeUID other) => value == other.value;
        public override bool Equals(object obj) => obj is NodeUID other && Equals(other);
        public override int GetHashCode() => value;
        public override string ToString() => $"NodeUID({value})";
        #endregion

        #region Public - Operators
        public static implicit operator int(NodeUID id) => id.value;
        public static implicit operator NodeUID(int v) => new(v);
        public static bool operator ==(NodeUID a, NodeUID b) => a.Equals(b);
        public static bool operator !=(NodeUID a, NodeUID b) => !a.Equals(b);
        #endregion
    }
}

#if UNITY_EDITOR
// =============================================================================
// Dev Log
// =============================================================================
// @Jason - PKH 2026-04-22 NodeUID 의 역할 - 노드의 변경 불가 정체성 (project-global int wrapper)
//
//   [역할]
//   - 노드 그래프 시스템의 unique identity. project-global 단조 증가 int.
//   - readonly struct + IEquatable + 암시적 int 변환으로 값 의미체계 유지.
//
//   [설계 근거]
//   - HUtil 의 DataOwnerId 관용(readonly struct + Interlocked 발급) 패턴 차용.
//   + BUT 의미 분리: DataOwnerId 는 lease 추적용, NodeUID 는 graph identity.
//     같은 패턴을 다른 타입으로 분리해 의미 오염 방지 + 이벤트 오염 방지.
//   + [Serializable] 추가는 DataOwnerId 와의 차이점. HDictionary 의 Entry struct
//     Key 로 직렬화되어야 하므로 필수.
//
//   [값 규칙]
//   - value > 0: 유효한 NodeUID
//   - value <= 0: NodeUID.None (0 예약, IsValid == false)
//
//   [발급 책임]
//   - Editor-only NodeUIDRegistry.instance.Issue() 만 발급 가능.
//   - 런타임 발급 경로 구조적으로 0 (Registry 가 Editor asmdef 에 거주).
//
//   [재사용 금지] 삭제된 노드의 UID 는 재발급 안 됨. Registry 카운터는 단조 증가만.
//
//   [확장 노트] 향후 평행 식별자 (예: SubGraphUID) 추가 시 같은 패턴 복제.
//   EdgeUID 는 검토 후 폐기 — parallel edge 금지로 (branch, leaf) pair 가 곧 ID.
// =============================================================================
#endif
