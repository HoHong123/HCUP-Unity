#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Resources 단일 asset 로더 구현. string key → Resources.LoadAsync<TAsset> 를 UniTask 로 await.
 *
 * 주요 기능 ::
 * 문자열 key 정규화 (확장자 제거 + 슬래시 trim + rootPath 결합).
 * Resources.LoadAsync 로 메인 스레드 부담을 여러 프레임으로 나눈다 (통합은 메인, WebGL 은 로드도 메인). 없으면 null.
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
        public async UniTask<TAsset> LoadAsync(string key) {
            var normalizedKey = _NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(normalizedKey)) {
                return null;
            }

            Object asset = await Resources.LoadAsync<TAsset>(normalizedKey).ToUniTask();
            return asset as TAsset;
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
 * 2026-09-21 (수정) :: 비동기 로드의 스레드 서술 정정
 *
 * 변경 ::
 * 헤더의 "메인 스레드를 막지 않는다" 를 "메인 스레드 부담을 여러 프레임으로 나눈다" 로 교체하고
 * WebGL 과 로드 후 통합 단계의 스레드를 명시.
 *
 * 이유 ::
 * 리뷰 지적. 아래 항목 "결과" 의 "로드가 백그라운드 로딩 스레드에서 진행" 은 WebGL 에서 틀리다.
 * WebGL 은 기본 설정에 엔진 로딩 스레드가 없다. 데스크톱 · 모바일에서도 로드 후 오브젝트 통합과
 * 텍스처 업로드는 메인 스레드에서 일어나므로 "막지 않는다" 는 과장이다.
 *
 * 결과 ::
 * 코드 변경 없음. docs/Load.md 의 같은 서술도 함께 고쳤다.
 *
 * 주의 ::
 * 아래 항목의 서술은 기록이므로 고치지 않는다. 이 항목이 그 정정이다.
 *
 * =========================================================
 * 2026-09-21 (수정) :: Resources.Load 동기 호출을 Resources.LoadAsync 로 교체
 *
 * 변경 ::
 * LoadAsync 를 async 로 바꾸고 UniTask.FromResult(Resources.Load(...)) 를
 * Resources.LoadAsync<TAsset>(...).ToUniTask() await 로 교체. 결과는 Object 이므로 as TAsset 캐스팅.
 *
 * 이유 ::
 * FromResult 는 인자를 평가하는 시점에 Resources.Load 가 이미 동기로 끝나므로 시그니처만 비동기였다.
 * 디스크 I/O 와 역직렬화가 호출 프레임에서 전부 수행되어 메인 스레드가 멈췄다.
 *
 * 결과 ::
 * 로드가 백그라운드 로딩 스레드에서 진행되고 완료는 다음 프레임 이후에 온다.
 * 자산이 없을 때 null 을 반환하는 계약은 동일 (ResourceRequest 는 예외를 던지지 않음).
 *
 * 주의 ::
 * 호출 프레임 안에서 즉시 완료되던 동작은 사라졌다. 결과를 같은 프레임에 기대는 호출처가 있으면
 * 깨진다. 2026-09-21 기준 호출처는 AssetProvider 의 await 하나라 영향 없음.
 *
 * =========================================================
 * 2026-08-06 (수정) :: rootPath 경계 검사 없는 StartsWith 교정 (감사 5차 HResource 항목 8)
 *
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
 *
 * 변경 ::
 * 기존 헤더 (상단 도입+주의사항 + 하단 주요기능/사용법/이벤트/기타) 를 한 곳에 통합하여
 * §11 형틀 통일. 하단 Dev Log 영역 추가. 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드.
 *
 * 이유 ::
 * 글로벌 CLAUDE.md §11 룰 일괄 적용.
 *
 * =========================================================
 * 2026-04-25 (최초 설계) :: ResourcesAssetLoader 초기 구현
 *
 * 정규화 책임만 최소한으로 포함 - 확장자 제거 + 슬래시 trim + rootPath 결합. owner 추적과
 * cache 정책은 상위 계층 (provider) 이 담당. Resources.Load 가 동기 호출이므로 UniTask
 * .FromResult 로 즉시 완료 비동기로 래핑 (인터페이스 일관성 + 조합성). IAssetReleasableLoader
 * 미구현 - Resources 자산은 명시 release 가 불필요 (Unity 가 씬 전환 시 자동 정리).
 * =========================================================
 */
#endif
