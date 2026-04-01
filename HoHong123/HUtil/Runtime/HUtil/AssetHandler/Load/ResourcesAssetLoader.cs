using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using HUtil.AssetHandler.Data;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Resources 단일 asset 로더 구현 스크립트입니다.
 *
 * 주의사항 ::
 * 1. resourcesRootPath와 token path 조합 규칙이 프로젝트 규칙과 맞아야 합니다.
 * 2. Resources는 별도 source release를 요구하지 않습니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Load {
    public sealed class ResourcesAssetLoader<TAsset> : IAssetLoader<string, TAsset>
        where TAsset : Object {
        #region Fields
        readonly string resourcesRootPath;
        #endregion

        #region Properties
        public AssetLoadMode LoadMode => AssetLoadMode.Resources;
        #endregion

        #region Public - Constructors
        public ResourcesAssetLoader() : this(string.Empty) {}

        public ResourcesAssetLoader(string resourcesRootPath) {
            this.resourcesRootPath = _NormalizeRootPath(resourcesRootPath);
        }
        #endregion

        #region Public - Load
        public UniTask<TAsset> LoadAsync(string key) {
            var normalizedKey = _NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(normalizedKey)) {
                return UniTask.FromResult<TAsset>(null);
            }
            return UniTask.FromResult(Resources.Load<TAsset>(normalizedKey));
        }
        #endregion

        #region Private - Normalize
        private string _NormalizeKey(string key) {
            if (string.IsNullOrWhiteSpace(key)) {
                return string.Empty;
            }

            var normalizedKey = _TrimExtension(key).TrimStart('/');
            if (string.IsNullOrWhiteSpace(normalizedKey)) {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(resourcesRootPath)) {
                return normalizedKey;
            }

            if (normalizedKey.StartsWith(resourcesRootPath, StringComparison.OrdinalIgnoreCase)) {
                return normalizedKey;
            }

            return $"{resourcesRootPath}/{normalizedKey}";
        }

        private string _NormalizeRootPath(string path) {
            if (string.IsNullOrWhiteSpace(path)) {
                return string.Empty;
            }

            return _TrimExtension(path).Trim('/').Trim();
        }

        private string _TrimExtension(string path) {
            if (string.IsNullOrWhiteSpace(path)) {
                return string.Empty;
            }

            return Path.ChangeExtension(path, null)?.Replace("\\", "/") ?? string.Empty;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. 문자열 key를 Resources 경로로 정리합니다.
 * 2. Resources.Load 기반 비동기 반환 경로를 제공합니다.
 *
 * 사용법 ::
 * 1. AssetProvider의 Resources loader로 등록해 사용합니다.
 * 2. catalog가 만든 path와 token을 통해 실제 Resources key를 구성합니다.
 *
 * 이벤트 ::
 * 1. 직접 이벤트는 없습니다.
 * 2. provider가 load 결과를 cache/store와 연결합니다.
 *
 * 기타 ::
 * 1. 정규화 책임만 최소한으로 포함합니다.
 * 2. owner 추적과 cache 정책은 상위 계층이 담당합니다.
 * =========================================================
 */
#endif
