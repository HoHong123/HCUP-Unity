#if UNITY_EDITOR
/* =========================================================
 * 데이터를 영속 저장소에 저장하기 위한 인터페이스입니다.
 * 로드된 데이터를 서버 또는 로컬 저장소에 저장합니다.
 *
 * 주의사항 ::
 * SaveAsync는 비동기 저장을 수행합니다.
 * =========================================================
 */
#endif

namespace HUtil.Data.Save {
    public interface IDataSave<TKey, TData> {
        Load.DataLoadType Type { get; }
        Cysharp.Threading.Tasks.UniTask SaveAsync(TKey key, TData data);
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. SaveAsync
 *    + key 기반 데이터 저장
 *
 * 사용법 ::
 * 1. IDataSave 구현체를 작성하여 DataEndpoint에 전달합니다.
 *
 * 기타 ::
 * 1. Local 저장 또는 Server 저장 시스템에서 사용됩니다.
 * =========================================================
 */
#endif