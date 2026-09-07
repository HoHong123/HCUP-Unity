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
 * "이미 rootPath 하위" 판정은 경로 경계(뒤따르는 '/' 또는 완전 일치)까지 검사한다 - 단순
 * StartsWith 는 rootPath="Icon"·key="IconSet/A" 같은 접두 오탐으로 이중 결합을 만든다.
 * =========================================================
 */
#endif

using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using HResource.Data;
using Object = UnityEngine.Object;

namespace HResource.Load {
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

            // StartsWith 만으로는 경로 경계를 검사하지 않는다.
            // rootPath="Icon" 일 때 key="IconSet/A" 가 "Icon" 으로 시작한다는 이유로
            // 오탐되어 "Icon/IconSet/A" 로 잘못 중복 결합되지 않도록,
            // 정확히 rootPath 뒤에 '/' 가 오거나 rootPath 자체와 같은 경우만 "이미 rootPath 하위" 로 인정한다.
            bool isUnderRootPath = normalizedKey.Equals(resourcesRootPath, StringComparison.OrdinalIgnoreCase)
                || normalizedKey.StartsWith(resourcesRootPath + "/", StringComparison.OrdinalIgnoreCase);
            if (isUnderRootPath) {
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
 * 2026-08-06 (수정) :: rootPath 경계 검사 없는 StartsWith 교정 (감사 5차 HResource 항목 8)
 * =========================================================
 * 변경 ::
 * _NormalizeKey 의 "이미 rootPath 하위인가" 판정을 StartsWith(rootPath) 에서
 * Equals(rootPath) 또는 StartsWith(rootPath + "/") 로 교체.
 *
 * 이유 ::
 * StartsWith 만으로는 경로 경계를 검사하지 않아, rootPath="Icon" 일 때 key="IconSet/A" 가
 * "Icon" 으로 시작한다는 이유로 "이미 rootPath 하위" 로 오판되어 rootPath 결합을 건너뛴다.
 * 현재 프로젝트 전 호출이 rootPath="" 라 미도달이었으나, rootPath 를 실제로 쓰는 호출이
 * 생기는 순간 접두 겹침 조합에서 조용히 잘못된 키로 로드가 실패하므로 지금 교정한다.
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
 * 정규화 책임만 최소한으로 포함 - 확장자 제거 + 슬래시 trim + rootPath 결합. owner 추적과
 * cache 정책은 상위 계층 (provider) 이 담당. Resources.Load 가 동기 호출이므로 UniTask
 * .FromResult 로 즉시 완료 비동기로 래핑 (인터페이스 일관성 + 조합성). IAssetReleasableLoader
 * 미구현 - Resources 자산은 명시 release 가 불필요 (Unity 가 씬 전환 시 자동 정리).
 * =========================================================
 */
#endif
