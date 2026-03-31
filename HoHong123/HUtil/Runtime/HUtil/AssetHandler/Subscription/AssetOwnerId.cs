using System;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * asset 점유 주체를 식별하는 값 타입 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 참조 객체 자체 대신 식별자 값으로 owner를 다룹니다.
 * 2. 0 이하는 유효한 ownerId로 취급하지 않습니다.
 * =========================================================
 */
#endif

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
 * @Jason - PKH
 * 주요 기능 ::
 * 1. owner 식별 값을 보관합니다.
 * 2. 유효성 판별과 equality 비교를 제공합니다.
 *
 * 사용법 ::
 * 1. AssetRequest와 cache, provider release 경로에 함께 전달합니다.
 * 2. owner lifecycle을 식별자로 분리할 때 사용합니다.
 *
 * 이벤트 ::
 * 1. 직접 이벤트는 없습니다.
 * 2. 생성과 해제 통지는 generator가 담당합니다.
 *
 * 기타 ::
 * 1. readonly struct입니다.
 * 2. reference equality에 의존하지 않기 위한 핵심 값 타입입니다.
 * =========================================================
 */
#endif
