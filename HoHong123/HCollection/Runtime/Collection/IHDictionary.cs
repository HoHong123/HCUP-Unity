#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * HDictionary<TKey, TValue> 계열 타입에 비제네릭 공통 API를 제공하는 인터페이스.
 *
 * 목적 ::
 * Editor 측 검증 로직(HDictionaryValidator)이 reflection으로 필드를 순회할 때
 * 타입 파라미터에 의존하지 않고 중복 Key 여부를 질의할 수 있도록 한다.
 *
 * 사용 ::
 * IHDictionary dict = field.GetValue(target) as IHDictionary;
 * if (dict != null && dict.HasDuplicateKeys()) { ... }
 *
 * 주의 ::
 * 런타임 배포 빌드에서도 참조되는 인터페이스이므로 #if UNITY_EDITOR로 감싸지 않는다.
 * 구현체의 HasDuplicateKeys 본문은 entries 프록시 List가 남아 있는 에디터 맥락에서만
 * 의미 있는 결과를 반환한다.
 * =========================================================
 */
#endif

namespace HCollection {
    public interface IHDictionary {
        bool HasDuplicateKeys();
        int DuplicateKeyCount();
    }
}
