#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * asset 점유 주체를 식별하는 readonly struct (값 타입).
 *
 * 주요 기능 ::
 * Value (int) + IsValid (Value > 0) + None (sentinel) + IEquatable / implicit int 변환.
 *
 * 사용법 ::
 * AssetOwnerIdGenerator.NewId 로 발급. AssetRequest / cache / provider release 경로에 함께 전달.
 * owner lifecycle 을 식별자로 분리해 owner 객체 자체를 참조하지 않아도 점유 추적 가능.
 *
 * 주의 ::
 * readonly struct 라 heap 할당 0. reference equality 의존 금지 (IEquatable 사용).
 * 0 이하는 invalid 로 취급 (None.Value == 0 이 sentinel).
 * =========================================================
 */
#endif

using System;

namespace HUtil.AssetHandler.Subscription {
    public readonly struct AssetOwnerId : IEquatable<AssetOwnerId> {
        #region Fields
        public readonly int Value;
        #endregion

        #region Properties
        public static AssetOwnerId None => new(0);
        public bool IsValid => Value > 0;
        #endregion

        #region Public - Constructors
        public AssetOwnerId(int value) {
            Value = value;
        }
        #endregion

        #region Public - Equals
        public bool Equals(AssetOwnerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is AssetOwnerId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => Value.ToString();
        #endregion

        #region Public - Implicit
        public static implicit operator int(AssetOwnerId ownerId) => ownerId.Value;
        public static implicit operator AssetOwnerId(int value) => new AssetOwnerId(value);
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 *
 * =========================================================
 * 2026-04-26 (수정) :: 헤더 형틀 통합 + Dev Log 형식 도입
 * =========================================================
 * 변경 ::
 * 기존 헤더 (도입 + 주의사항) 에 "주요 기능 / 사용법" 섹션 추가하여 §11 형틀 통일.
 * 하단 Dev Log 영역 추가. 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드.
 *
 * 이유 ::
 * 글로벌 CLAUDE.md §11 룰 일괄 적용.
 *
 * =========================================================
 * 2026-04-25 (최초 설계) :: AssetOwnerId 초기 구현
 * =========================================================
 * owner 객체 자체 대신 식별자 값으로 점유 추적. readonly struct + IEquatable + int 변환으로
 * heap 할당 0 + reference equality 회피. AssetOwnerIdGenerator 가 Interlocked.Increment 로
 * thread-safe 발급. 0 이하는 invalid sentinel — None.Value == 0.
 * =========================================================
 */
#endif
