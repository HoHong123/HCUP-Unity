#if UNITY_EDITOR
/* =========================================================
 * Unity Resources 폴더에서 데이터를 로드하는 LoadSequence 클래스입니다.
 * Resources.Load<T>() 기반 데이터 로드를 수행합니다.
 *
 * 주의사항 ::
 * Resources 경로 기준으로 Key가 정규화됩니다.
 * =========================================================
 */
#endif

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using HUtil.Data.Load;

namespace HUtil.Data.Sequence {
    public class ResourcesLoadSequence<TData> :
        BaseLoadSequence<TData>,
        IDataLoad<string, TData>
        where TData : UnityEngine.Object {
        #region Fields
        protected string path;
        #endregion

        #region Public - Constructors
        public ResourcesLoadSequence(string path) : base(DataLoadType.Resources) {
            this.path = path;
        }
        #endregion

        #region Protected - Normalize Key
        protected override string _NormalizeKey(string tokenOrPath) {
            if (string.IsNullOrWhiteSpace(tokenOrPath)) return string.Empty;

            var normalized = _TrimExtension(tokenOrPath).TrimStart('/'); // 확장자/폴더 제거

            // Resources 기준 상대경로 강제
            if (!normalized.StartsWith(path, StringComparison.OrdinalIgnoreCase)) normalized = $"{path}{normalized}";

            return normalized;
        }
        #endregion

        #region Protected - Load
        protected override UniTask<TData> _LoadByKeyAsync(string key) =>
            UniTask.FromResult(Resources.Load<TData>(key));
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. Resources.Load 기반 데이터 로드
 * 2. 경로 정규화
 * 3. 확장자 제거
 *
 * 사용법 ::
 * 1. Resources 폴더 경로를 전달하여 생성합니다.
 *
 * 기타 ::
 * 1. BaseLoadSequence 기반 구현입니다.
 * =========================================================
 */
#endif