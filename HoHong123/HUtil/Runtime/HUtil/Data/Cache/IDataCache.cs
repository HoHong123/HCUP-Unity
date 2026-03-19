#if UNITY_EDITOR
/* =========================================================
 * 런타임 데이터 캐시 시스템의 기본 인터페이스입니다.
 *
 * 데이터 캐싱 및 의존성 관리 기능을 정의합니다.
 *
 * 주의사항 ::
 * IDataCache는 실제 캐시 구현체(BaseDataCache 등)에 의해 구현됩니다.
 * =========================================================
 */
#endif

namespace HUtil.Data.Cache {
    public interface IDataCache<TKey, TData> {
        bool TryLoad(TKey key, out TData data);
        bool TryLoad(TKey key, object owner, out TData data);
        bool TryGet(TKey key, out TData data);
        bool Save(TKey key, TData data);
        bool Save(TKey key, TData data, object owner);
        void Prune();
        bool Release(TKey key);
        bool Release(TKey key, object owner);
        int ReleaseOwner(object owner);
        void ForceRemove(TKey key);
        void Clear();
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. TryLoad
 * 2. TryGet
 * 3. Save
 * 4. Release
 * 5. Prune
 * 6. ForceRemove
 * 7. Clear
 *
 * 사용법 ::
 * 1. DataCache 구현체를 통해 캐시 시스템을 구성합니다.
 *
 * 기타 ::
 * 1. DataEndpoint 및 Loader 시스템과 함께 사용됩니다.
 * =========================================================
 */
#endif
