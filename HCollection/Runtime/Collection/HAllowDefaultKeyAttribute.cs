#if UNITY_EDITOR
/* =========================================================
 * 그 타입의 기본값이 사전의 정당한 키라고 선언하는 표시입니다.
 *
 * 특징 / 지원기능 ::
 * + HDictionary 가 값 타입 키에서 default 를 "미배정 행" 으로 의심해 경고한다. 이 표시가
 *   붙은 타입은 그 경고에서 빠진다.
 * + 열거형과 구조체에 붙는다. 필드가 아니라 **타입**에 붙는다.
 *
 * 사용 ::
 * + 0 번 멤버가 진짜 값인 열거형에 붙인다. 예를 들어 첫 단계가 0 인 진행 열거형.
 * + 0 번이 None 같은 센티넬이면 붙이지 않는다. 그때는 경고가 제 일을 한다.
 *
 * 주의사항 ::
 * + **필드에 붙일 수 없다.** 경고는 HDictionary.OnAfterDeserialize 안에서 나는데 그 시점의
 *   사전은 자기가 어느 필드에 담겼는지 모른다. 읽을 수 있는 것은 TKey 타입뿐이다.
 * + 붙이면 그 타입을 키로 쓰는 **모든** 사전에서 경고가 꺼진다. 0 이 진짜 값인지는 타입의
 *   성질이므로 그것이 맞는 범위다.
 * =========================================================
 */
#endif

using System;

namespace HCollection {
    /// <summary>
    /// 이 타입의 기본값이 사전의 정당한 키임을 선언합니다. HDictionary 의 미배정 행 경고에서 빠집니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class HAllowDefaultKeyAttribute : Attribute { }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.09.13 HAllowDefaultKeyAttribute.cs 베이스 코드 생성
 *
 * # 목적
 * - HDictionary 의 "Default-valued key" 경고가 정당한 키에도 뜨는 것을 끈다.
 *
 * # 계기
 * - DesktopForest 의 FloraStage.Sprout 과 AnimalState.Walk 가 0 이다. 둘 다 진짜 값인데
 *   default 와 같아서 인스펙터를 그릴 때마다 경고가 났다.
 *
 * # 결정
 * - **필드가 아니라 타입에 붙인다.** 두 가지 이유다. 하나는 기술적 제약으로,
 *   OnAfterDeserialize 안에서는 필드 속성을 읽을 길이 없다. 다른 하나는 의미인데,
 *   "0 이 진짜 값인가" 는 그 열거형의 성질이지 그것을 담은 필드의 성질이 아니다.
 * - **자동 판별을 시도하지 않았다.** 0 이 정의돼 있는지만으로는 갈리지 않는다.
 *   FloraSpecies.None = 0 도 정의돼 있지만 그쪽은 센티넬이다. 그 차이는 의미라서
 *   사람이 선언해야 한다.
 *
 * =============================================================================
 */
#endif
