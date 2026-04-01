#if UNITY_EDITOR
/* =========================================================
 * 로드 후 수명 관리가 필요한 데이터 로더를 위한 인터페이스입니다.
 * Addressables 처럼 명시적 Release가 필요한 시스템에서 사용합니다.
 *
 * 주의사항 ::
 * 1. 캐시 제거 시점과 Release 호출 시점을 일치시켜야 합니다.
 * =========================================================
 */
#endif

namespace HUtil.Data.Load {
    public interface IReleasableDataLoad<TKey, TData> : IDataLoad<TKey, TData> {
        void Release(TKey key);
        void ReleaseAll();
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. Release
 *    + 특정 Key 수명 해제
 * 2. ReleaseAll
 *    + 전체 수명 해제
 *
 * 사용법 ::
 * 1. 명시적 해제가 필요한 Loader가 구현합니다.
 *
 * 기타 ::
 * 1. AssetProvider가 Cache 제거 이벤트와 연결하여 사용합니다.
 * =========================================================
 */
#endif