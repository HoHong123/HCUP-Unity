#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * asset 점유 주체를 식별하는 readonly struct (값 타입).
 *
 * 주요 기능 ::
 * Value (int) + IsValid (Value > 0) + None (sentinel) + IEquatable / int 로의 implicit 변환(읽기 전용).
 *
 * 사용법 ::
 * AssetOwnerIdGenerator.NewId 로 발급. AssetRequest / cache / provider release 경로에 함께 전달.
 * owner lifecycle 을 식별자로 분리해 owner 객체 자체를 참조하지 않아도 점유 추적 가능.
 *
 * 주의 ::
 * readonly struct 라 heap 할당 0. reference equality 의존 금지 (IEquatable 사용).
 * 0 이하는 invalid 로 취급 (None.Value == 0 이 sentinel).
 * int → AssetOwnerId 방향의 implicit 변환은 없다. 발급은 반드시 AssetOwnerIdGenerator.NewId 를
 * 거쳐야 하며, 임의 정수가 owner 로 통과하는 것을 컴파일 타임에 막는다.
 * 생성자와 발급기 모두 internal 이라 어셈블리 밖에서는 신원을 만들 수도 발급받을 수도 없다.
 * =========================================================
 */
#endif

using System;

namespace HResource.Subscription {
    public readonly struct AssetOwnerId : IEquatable<AssetOwnerId> {
        #region Fields
        public readonly int Value;
        #endregion

        #region Properties
        public static AssetOwnerId None => new(0);
        public bool IsValid => Value > 0;
        #endregion

        #region Public - Constructors
        // internal 이다. 어셈블리 밖에서 임의 정수로 신원을 위조하면 provider 경계의
        // 소유자 강제가 캐시 계층에서 우회된다. 발급은 AssetOwnerIdGenerator 한 곳이다.
        internal AssetOwnerId(int value) {
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
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-08-06 (수정) :: int → AssetOwnerId implicit 변환 제거 (감사 5차 HResource 항목 7)
 * 
 * 변경 ::
 * public static implicit operator AssetOwnerId(int value) 삭제. int → AssetOwnerId 방향은
 * 이제 명시 생성자 호출(new AssetOwnerId(value))만 가능. int 로 읽는 반대 방향(operator int)은 유지.
 *
 * 이유 ::
 * 이 변환이 있으면 AssetOwnerIdGenerator.NewId 를 거치지 않은 임의 정수가 호출부의 int 리터럴이나
 * 다른 카운터 값으로부터 컴파일러 묵인 하에 AssetOwnerId 로 통과할 수 있었다 (실사용 호출처는
 * 0건이었으나 계약상 우회 경로 자체가 존재). owner 식별자의 유일성은 Interlocked 발급이 유일한
 * 근거이므로, 그 근거를 우회하는 변환 경로를 없애 컴파일 타임에 차단한다.
 *
 * =========================================================
 * 2026-04-26 (수정) :: 헤더 형틀 통합 + Dev Log 형식 도입
 * 
 * 변경 ::
 * 기존 헤더 (도입 + 주의사항) 에 "주요 기능 / 사용법" 섹션 추가하여 §11 형틀 통일.
 * 하단 Dev Log 영역 추가. 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드.
 *
 * 이유 ::
 * 글로벌 CLAUDE.md §11 룰 일괄 적용.
 *
 * =========================================================
 * 2026-04-25 (최초 설계) :: AssetOwnerId 초기 구현
 * 
 * owner 객체 자체 대신 식별자 값으로 점유 추적. readonly struct + IEquatable + int 변환으로
 * heap 할당 0 + reference equality 회피. AssetOwnerIdGenerator 가 Interlocked.Increment 로
 * thread-safe 발급. 0 이하는 invalid sentinel - None.Value == 0.
 * =========================================================
 */
#endif
