#if UNITY_EDITOR
/* =========================================================
 * UID(int) 기반 데이터를 string token 기반 Loader로 변환하기 위한 Adapter 클래스입니다.
 *
 * 주의사항 ::
 * UID가 음수이거나 token이 비어있으면 null을 반환합니다.
 * =========================================================
 */
#endif

using System;
using Cysharp.Threading.Tasks;

namespace HUtil.Data.Load {
    public sealed class UidToStringConvertor<TData> : IDataLoad<int, TData> where TData : class {
        #region Fields
        readonly IDataLoad<string, TData> stringLoader;
        readonly Func<int, string> resolveToken;
        #endregion

        #region Properties
        public DataLoadType Type => stringLoader.Type;
        #endregion

        #region Public - Constructors
        public UidToStringConvertor(
            IDataLoad<string, TData> stringLoader,
            Func<int, string> resolveToken) {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsNotNull(stringLoader);
            UnityEngine.Assertions.Assert.IsNotNull(resolveToken);
#endif
            this.stringLoader = stringLoader;
            this.resolveToken = resolveToken;
        }
        #endregion

        #region Public - Load
        public UniTask<TData> LoadAsync(int uid) {
            if (uid < 0) return UniTask.FromResult<TData>(null);

            var token = resolveToken(uid);
            if (string.IsNullOrWhiteSpace(token)) return UniTask.FromResult<TData>(null);

            return stringLoader.LoadAsync(token);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. UID → Token 변환
 * 2. Token 기반 Loader 호출
 *
 * 사용법 ::
 * 1. stringLoader와 resolveToken 함수를 전달하여 생성합니다.
 *
 * 기타 ::
 * 1. UID 기반 데이터 시스템과 string 기반 Asset Loader를
 *    연결하는 Adapter 역할을 합니다.
 * =========================================================
 */
#endif