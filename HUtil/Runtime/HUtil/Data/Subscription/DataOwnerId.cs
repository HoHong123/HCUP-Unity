#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * DataOwnerId는 데이터 로드/캐시 시스템에서 사용되는
 * "소유권 단위 식별자"를 나타내는 값 타입입니다.
 *
 * 설계 목적 ::
 * 1. object owner 기반 관리 방식의 문제(Unity lifecycle, MissingReference)를 제거
 * 2. 캐시 시스템에서 owner를 참조가 아닌 "식별자"로 관리
 * 3. 동일 owner 기준 Dependency 및 Release를 정확하게 수행하기 위함
 *
 * 핵심 개념 ::
 * 1. DataOwnerId는 "객체 자체"가 아닌 "소유권"을 의미합니다.
 * 2. 하나의 ownerId는 하나의 책임 단위(Agent, UI, System 등)를 나타냅니다.
 * 3. 동일 ownerId를 사용해야 Load/Release가 정상 동작합니다.
 *
 * 값 규칙 ::
 * 1. Value > 0 : 유효한 ownerId
 * 2. Value <= 0 : Invalid (None)
 *
 * 생성 방식 ::
 * 1. DataOwnerIdGenerator를 통해 생성해야 합니다.
 * 2. 외부에서 임의 생성 금지 (충돌 위험)
 *
 * 주의사항 ::
 * 1. 매 요청마다 새로운 ID를 생성하면 안됩니다.
 * 2. 반드시 동일 생명주기 동안 같은 ID를 유지해야 합니다.
 * 3. Load와 Release에 동일한 ownerId를 전달해야 합니다.
 * =========================================================
 */
#endif

using System;

namespace HUtil.Data.Subscription {
    public readonly struct DataOwnerId : IEquatable<DataOwnerId> {
        #region Fields
        public readonly int Value;
        #endregion

        #region Properties
        public static DataOwnerId None => new(0);
        public bool IsValid => Value > 0;
        #endregion

        #region Constructors
        public DataOwnerId(int value) {
            Value = value;
        }
        #endregion

        #region Public - Equals
        public bool Equals(DataOwnerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is DataOwnerId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => Value.ToString();
        #endregion

        #region Public - Implicit
        public static implicit operator int(DataOwnerId id) => id.Value;
        public static implicit operator DataOwnerId(int value) => new(value);
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. Owner 식별자 표현
 * 2. 값 기반 비교 (Value Equality)
 * 3. 캐시 Dependency 추적 기준 제공
 *
 * 사용법 ::
 * 1. ownerId 생성
 *    DataOwnerId ownerId = DataOwnerIdGenerator.NewId();
 *
 * 2. Load 시 전달
 *    await provider.GetOrLoadAsync(key, ownerId);
 *
 * 3. Release 시 동일 ID 사용
 *    provider.ReleaseId(key, ownerId);
 *
 * 4. Owner 단위 전체 해제
 *    provider.ReleaseOwner(ownerId);
 *
 * 주의사항 ::
 * 1. default(DataOwnerId)는 Invalid 상태입니다.
 * 2. Value == 0은 사용하지 않는 것이 원칙입니다.
 * 3. ownerId는 객체 생명주기 동안 유지되어야 합니다.
 *
 * 잘못된 사용 ::
 * 1. 매 호출마다 NewId() 생성
 * 2. 임시 변수로 생성 후 재사용하지 않음
 * 3. Release 시 다른 ownerId 전달
 *
 * 기타 ::
 * 1. object owner 방식 대비 GC 부담 감소
 * 2. UnityEngine.Object lifecycle 영향 제거
 * 3. HashSet<object> 대신 HashSet<int> 사용 가능
 * =========================================================
 */
#endif