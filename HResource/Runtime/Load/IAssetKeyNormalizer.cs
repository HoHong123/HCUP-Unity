#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * provider 입구에서 key 를 한 가지 모양으로 맞추는 규칙 계약.
 *
 * 주요 기능 ::
 * Normalize(key) - 같은 에셋을 가리키는 여러 표기를 하나로 맞춘다. 결과가 비면 "로드할 수 없는 key" 다.
 *
 * 사용법 ::
 * AssetProvider 가 생성자로 받아 획득 · 조회 · 반납 입구에서 부른다. 팩토리가 로더 종류에 맞는 구현을 넣는다.
 * 게이트 · 캐시 · 로더 · 반납 추적표는 이 결과만 본다.
 *
 * 주의 ::
 * 정규화는 provider 입구에서 한 번만 한다. 로더는 받은 key 를 해석하지 않는다.
 * 구현이 멱등이라는 보장은 없다. ResourcesKeyNormalizer 는 두 번 거치면 "foo.v2.png" 가 "foo" 가 된다.
 * =========================================================
 */
#endif

namespace HResource.Load {
    public interface IAssetKeyNormalizer<TKey> {
        TKey Normalize(TKey key);
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-21 (최초 설계) :: provider 입구 key 정규화 계약
 *
 * 변경 ::
 * key 해석을 로더 안쪽에서 provider 입구로 옮기기 위한 계약을 새로 만들었다.
 *
 * 이유 ::
 * 로더만 key 를 정규화해 게이트 · 캐시 · 반납 추적표는 원본 key 를, 로더 표는 정규화 key 를 썼다.
 * 확장자만 다른 두 key 가 캐시 항목 2개와 로더 항목 1개를 만들어, 한쪽 반납이 다른 쪽 몫을 가져갔다.
 * IAssetSource.Release / TryGet 은 loadMode 를 받지 않아 반납 시점에 "어느 로더의 규칙인가" 를 알 수 없으므로,
 * 로더별 위임이 아니라 provider 하나에 규칙 하나를 주입한다.
 *
 * 결과 ::
 * 구현은 ResourcesKeyNormalizer (확장자 · 슬래시 · rootPath) 와 TrimKeyNormalizer (Addressables 주소) 둘.
 *
 * 주의 ::
 * Resources 와 Addressables 로더를 한 provider 에 섞으면 규칙을 하나로 정하기 어렵다. 팩토리 기본값은 Trim 이다.
 * =========================================================
 */
#endif
