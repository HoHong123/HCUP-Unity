#if UNITY_EDITOR
/* =========================================================
 * 외부 데이터 소스로부터 데이터를 로드하는 인터페이스입니다.
 * Key 기반 데이터 로딩 규격을 정의합니다.
 *
 * 주의사항 ::
 * 1. LoadAsync는 비동기 데이터 로드 함수입니다.
 * =========================================================
 */
#endif

namespace HUtil.Data.Load {
    public interface IDataLoad<TKey, TData> {
        DataLoadType Type { get; }
        Cysharp.Threading.Tasks.UniTask<TData> LoadAsync(TKey key);
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. Type
 *    + DataLoadType
 * 2. LoadAsync
 *    + Key 기반 데이터 로드
 *
 * 사용법 ::
 * 1. IDataLoad<TKey,TData> 구현체를 작성합니다.
 *
 * 기타 ::
 * 1. DataLoader에서 Loader 선택 시 사용됩니다.
 * =========================================================
 */
#endif