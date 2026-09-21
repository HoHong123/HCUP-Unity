#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Resources 경로 규칙의 key 정규화. 확장자 제거 + 선행 슬래시 제거 + rootPath 결합.
 *
 * 주요 기능 ::
 * "Icon/A", "Icon/A.png", "/Icon/A.png" 를 모두 "Icon/A" 로 맞춘다. Resources.Load 가 받는 유일한 형태다.
 *
 * 사용법 ::
 * AssetProviderFactory.CreateResources(rootPath) 가 만들어 provider 에 넣는다.
 *
 * 주의 ::
 * 멱등이 아니다. Path.ChangeExtension 이 마지막 점 뒤를 지우므로 두 번 거치면 "foo.v2.png" 가 "foo" 가 된다.
 * provider 입구에서 한 번만 부른다. 점이 들어간 에셋 이름은 한 번만 거쳐도 "foo.v2" 가 "foo" 로 읽힌다 (이전과 같음).
 * "이미 rootPath 하위" 판정은 경로 경계(뒤따르는 '/' 또는 완전 일치)까지 검사한다 - 단순
 * StartsWith 는 rootPath="Icon"·key="IconSet/A" 같은 접두 오탐으로 이중 결합을 만든다.
 * =========================================================
 */
#endif

using System;
using System.IO;

namespace HResource.Load {
    public sealed class ResourcesKeyNormalizer : IAssetKeyNormalizer<string> {
        #region Fields
        readonly string resourcesRootPath;
        #endregion

        #region Public - Constructors
        public ResourcesKeyNormalizer(string resourcesRootPath) {
            this.resourcesRootPath = _NormalizeRootPath(resourcesRootPath);
        }
        #endregion

        #region Public - Normalize
        public string Normalize(string key) {
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
        #endregion

        #region Private - Normalize
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
 * 2026-09-21 (최초 설계) :: ResourcesAssetLoader 의 정규화를 provider 입구 규칙으로 분리
 *
 * 변경 ::
 * ResourcesAssetLoader 의 _NormalizeKey / _NormalizeRootPath / _TrimExtension 을 그대로 옮겼다. 규칙은 바뀌지 않았다.
 *
 * 이유 ::
 * 로더 안의 정규화는 로더 표에만 적용되어 게이트 · 캐시와 key 모양이 달랐다. provider 입구로 옮겨 모든 표가 같은 key 를 쓴다.
 *
 * 결과 ::
 * rootPath 경계 검사(2026-08-06 감사 5차 HResource 항목 8 교정)도 함께 옮겼다. 이력은 ResourcesAssetLoader.cs Dev Log 에 있다.
 * =========================================================
 */
#endif
