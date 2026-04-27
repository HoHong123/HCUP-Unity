#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Resources 단일 asset 로더 구현. string key → Resources.Load<TAsset> 동기 호출 후 UniTask 래핑.
 *
 * 주요 기능 ::
 * 문자열 key 정규화 (확장자 제거 + 슬래시 trim + rootPath 결합).
 * Resources.Load 호출 결과를 UniTask.FromResult 로 즉시 완료 비동기로 노출.
 *
 * 사용법 ::
 * AssetProviderFactory.CreateResources(rootPath) 가 자동 등록. 또는 사용자 정의 조합으로
 * AssetProvider 생성자에 직접 주입. catalog 가 만든 path/token 이 그대로 key 로 들어옴.
 *
 * 주의 ::
 * resourcesRootPath 와 token path 조합 규칙이 프로젝트 규칙과 맞아야 함. Resources 는 별도
 * source release 를 요구하지 않으므로 IAssetReleasableLoader 를 구현하지 않음 (cache 만 정리).
 * =========================================================
 */
#endif

using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using HUtil.AssetHandler.Data;
using Object = UnityEngine.Object;

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
 * Dev Log
 * =========================================================
 *
 * =========================================================
 * 2026-04-26 (수정) :: 헤더 형틀 통합 + Dev Log 형식 도입
 * =========================================================
 * 변경 ::
 * 기존 헤더 (상단 도입+주의사항 + 하단 주요기능/사용법/이벤트/기타) 를 한 곳에 통합하여
 * §11 형틀 통일. 하단 Dev Log 영역 추가. 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드.
 *
 * 이유 ::
 * 글로벌 CLAUDE.md §11 룰 일괄 적용.
 *
 * =========================================================
 * 2026-04-25 (최초 설계) :: ResourcesAssetLoader 초기 구현
 * =========================================================
 * 정규화 책임만 최소한으로 포함 — 확장자 제거 + 슬래시 trim + rootPath 결합. owner 추적과
 * cache 정책은 상위 계층 (provider) 이 담당. Resources.Load 가 동기 호출이므로 UniTask
 * .FromResult 로 즉시 완료 비동기로 래핑 (인터페이스 일관성 + 조합성). IAssetReleasableLoader
 * 미구현 — Resources 자산은 명시 release 가 불필요 (Unity 가 씬 전환 시 자동 정리).
 * =========================================================
 */
#endif
