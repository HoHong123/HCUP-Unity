#if UNITY_EDITOR
/* =========================================================
 * Addressables 기반 데이터 로드를 위한 LoadSequence 클래스입니다.
 * Addressables token 또는 path 기반 데이터 로드를 지원합니다.
 *
 * 주의사항 ::
 * 현재 Addressables 실제 로드 구현은 TODO 상태입니다.
 * =========================================================
 */
#endif

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using HUtil.Data.Load;

namespace HUtil.Data.Sequence {
    public class AddressableLoadSequence<TData> :
        BaseLoadSequence<TData>,
        IDataLoad<string, TData>
        where TData : class {
        #region Public - Constructors
        public AddressableLoadSequence() : base(DataLoadType.Addressable) {}
        #endregion

        #region Protected - Load By Key
        protected override UniTask<TData> _LoadByKeyAsync(string key) {
            // TODO: Addressables 도입 시
            return UniTask.FromResult<TData>(null);
        }
        #endregion

        #region Protected - Normalize Key
        // To Addressable Token Path
        protected override string _NormalizeKey(string tokenOrPath) {
            if (string.IsNullOrWhiteSpace(tokenOrPath)) return string.Empty;
            return _TrimExtension(tokenOrPath);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. Addressables token 기반 로드
 * 2. Path 정규화
 *
 * 사용법 ::
 * 1. BaseLoadSequence를 상속하여 Addressables 로드를 구현합니다.
 *
 * 기타 ::
 * 1. Addressables 시스템 도입 시 구현 예정입니다.
 * =========================================================
 */
#endif