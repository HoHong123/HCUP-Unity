#if UNITY_EDITOR
/* =========================================================
 * 데이터 Provider의 기본 인터페이스입니다.
 * Provider는 특정 데이터의 로드, 캐시 및 생명주기를 관리하는 책임을 가집니다.
 *
 * 주의사항 ::
 * IDataProvider는 데이터 시스템의 외부 접근 지점입니다.
 * =========================================================
 */
#endif

using Cysharp.Threading.Tasks;

namespace HUtil.Data.Provider {
    public interface IDataProvider<TKey, TData> {
        UniTask PrewarmIdAsync(TKey id);
        UniTask<TData> GetOrLoadAsync(TKey id);
        bool TryGet(TKey id, out TData data);
        void ReleaseId(TKey id);
        void Prune();
        void Clear();
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. PrewarmIdAsync
 * 2. GetOrLoadAsync
 * 3. TryGet
 * 4. ReleaseId
 * 5. Prune
 * 6. Clear
 *
 * 사용법 ::
 * 1. 데이터 시스템의 Entry Provider로 사용됩니다.
 *
 * 기타 ::
 * 1. AssetProvider 및 기타 데이터 Provider에서 구현됩니다.
 * =========================================================
 */
#endif