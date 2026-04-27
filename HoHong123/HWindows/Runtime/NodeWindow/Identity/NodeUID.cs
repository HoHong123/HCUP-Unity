using System;
using UnityEngine;

namespace HWindows.NodeWindow.Identity {
    [Serializable]
    public struct NodeUID : IEquatable<NodeUID> {
        #region Fields
        // readonly 키워드 제거: Unity serializer 가 readonly struct 내부 readonly field 를
        // deserialize 시 reflection 으로 쓰지 못해 value=0 으로 복원되는 문제 회피.
        // 불변성은 public 프로퍼티 get-only + 외부 mutation 경로 부재로 보장.
        [SerializeField]
        int value;
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
//
//   [Phase 1-A 스모크 중 발견된 직렬화 결함 및 수정 - 2026-04-24]
//   - 증상: SimpleNode 3개 Seed 후 HDictionaryValidator 가 "Duplicate key detected. Key='NodeUID(0)' at index=1,2,3" 경고.
//     catalog.nodes 에 들어간 NodeUID 3개가 전부 value=0 으로 복원됨.
//   - 원인: readonly struct + readonly field + [SerializeField] 조합.
//     + Unity serializer 는 보통 reflection 으로 readonly 필드도 write 가능.
//     + 그러나 readonly struct 내부 필드는 IL 레벨에서 init-only 취급 -> reflection SetValue 무효.
//     + 결과: serialize 시 YAML 에는 값이 저장되지만, deserialize 시 value=default(0) 으로 복원.
//   - 수정:
//     + public readonly struct NodeUID -> public struct NodeUID (readonly 제거)
//     + readonly int value -> int value (readonly 제거)
//     + Value 프로퍼티는 get-only 유지, 외부 mutation 경로 없음 -> 실질 불변성은 유지.
//   - 교훈:
//     + Phase 0 스모크 검증이 "컴파일 + .meta 생성" 까지만 해서 이 결함을 못 잡음.
//     + readonly struct 는 순수 메모리 내 값 타입에 적합. Unity 직렬화 대상 타입에는 피할 것.
//     + HUtil DataOwnerId 는 직렬화 대상 아니라 readonly struct 유지 OK.
//       NodeUID 는 HDictionary key 로 직렬화 필수 -> readonly 제거 필요.
// =============================================================================
#endif
