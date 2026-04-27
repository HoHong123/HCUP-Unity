#if UNITY_EDITOR
/* =========================================================
 * 데이터 로드 시퀀스의 기본 클래스입니다.
 * 로드 키 정규화와 실제 로드 호출을 분리합니다.
 *
 * 주의사항 ::
 * 실제 로드 로직은 _LoadByKeyAsync에서 구현됩니다.
 * =========================================================
 */
#endif

using Cysharp.Threading.Tasks;
using HUtil.Data.Load;

namespace HUtil.Data.Sequence {
    public abstract class BaseLoadSequence<TData> :
        IDataLoad<string, TData>
        where TData : class {
        #region Properties
        public DataLoadType Type { get; }
        #endregion

        #region Protected - Constructors
        protected BaseLoadSequence(DataLoadType type) { Type = type; }
        #endregion

        #region Public - Load
        public virtual UniTask<TData> LoadAsync(string tokenOrPath) {
            if (string.IsNullOrWhiteSpace(tokenOrPath)) return UniTask.FromResult<TData>(null);

            var key = _NormalizeKey(tokenOrPath);
            if (string.IsNullOrWhiteSpace(key)) return UniTask.FromResult<TData>(null);

            return _LoadByKeyAsync(key);
        }
        #endregion

        #region Protected - Path Conversion
        /// <summary> 자료 주소 안정화 </summary>
        /// <example>
        /// "/Equipment/Item/{target}" = "/Equipment/Item/{target}"
        /// "\\Equipment\\Item\\{target}" = "/Equipment/Item/{target}"
        /// "/{target}" = "/{target}"
        /// </example>
        protected string _TrimExtension(string path) {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            return System.IO.Path.ChangeExtension(path, null)?.Replace("\\", "/") ?? string.Empty;
        }
        #endregion

        #region Protected Abstract - Key Conversion
        protected abstract string _NormalizeKey(string tokenOrPath);
        protected abstract UniTask<TData> _LoadByKeyAsync(string key);
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. LoadAsync
 * 2. Key Normalize
 * 3. 실제 데이터 로드 호출
 *
 * 사용법 ::
 * 1. BaseLoadSequence를 상속합니다.
 * 2. _NormalizeKey와 _LoadByKeyAsync를 구현합니다.
 *
 * 기타 ::
 * 1. ResourcesLoadSequence 및 AddressableLoadSequence의
 *    베이스 클래스입니다.
 * =========================================================
 */
#endif
