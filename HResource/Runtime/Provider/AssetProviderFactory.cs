#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 기본 AssetProvider 조합을 만드는 정적 팩토리. 편의 생성 계층.
 *
 * 주요 기능 ::
 * CreateResources<TAsset>(rootPath) - Resources 기본 조합. key 규칙은 ResourcesKeyNormalizer(rootPath).
 * CreateAddressable<TAsset>() - Addressable 기본 조합. key 규칙은 TrimKeyNormalizer.
 * Create<TAsset>(loaders, store, keyNormalizer) - 사용자 정의 loader 조합. 규칙 생략 시 LoadMode 로 정하고 두 소스가 섞이면 예외.
 *
 * 사용법 ::
 * 도메인 코드가 빠르게 provider 를 조립할 때 사용. 기본 store 가 필요하면 인자로 전달.
 * 특수 조합 (커스텀 cache / validator / gate) 이 필요하면 AssetProvider 생성자 직접 호출.
 *
 * 주의 ::
 * 편의 생성 계층일 뿐 실제 로딩 정책을 직접 해석하지 않음. 생성된 provider 가 이후 런타임
 * 이벤트 흐름을 담당.
 * =========================================================
 */
#endif

using System.Collections.Generic;
using UnityEngine;
using HDiagnosis.Logger;
using HResource.Cache;
using HResource.Data;
using HResource.Load;
using HResource.Store;
using HResource.Validation;

namespace HResource.Provider {
    public static class AssetProviderFactory {
        #region Public - Create
        public static IAssetSource<string, TAsset> CreateResources<TAsset>(
            string resourcesRootPath,
            IAssetStore<string, TAsset> assetStore = null)
            where TAsset : Object {

            var assetLoader = new ResourcesAssetLoader<TAsset>();
            return Create(new[] { assetLoader }, assetStore, new ResourcesKeyNormalizer(resourcesRootPath));
        }

        public static IAssetSource<string, TAsset> CreateAddressable<TAsset>(
            IAssetStore<string, TAsset> assetStore = null)
            where TAsset : Object {

            var assetLoader = new AddressableAssetLoader<TAsset>();
            return Create(new[] { assetLoader }, assetStore, new TrimKeyNormalizer());
        }

        /// <summary>
        /// 로더를 직접 고르는 조합. 한 provider 는 한 소스만 담는다.
        /// keyNormalizer 를 넘기지 않으면 로더의 LoadMode 로 정한다. Resources 면 ResourcesKeyNormalizer(rootPath 없음),
        /// Addressable 이면 TrimKeyNormalizer, 둘이 섞이면 ArgumentException. 규칙을 직접 넘기면 그 선택을 따른다.
        /// </summary>
        public static IAssetSource<string, TAsset> Create<TAsset>(
            IEnumerable<IAssetLoader<string, TAsset>> assetLoaders,
            IAssetStore<string, TAsset> assetStore = null,
            IAssetKeyNormalizer<string> keyNormalizer = null)
            where TAsset : Object {

            if (assetLoaders == null) {
                HLogger.Throw(new System.ArgumentNullException(nameof(assetLoaders), "[AssetProviderFactory] assetLoaders is null."));
            }

            // 규칙을 정하는 데 한 번, provider 에 넘기는 데 한 번 읽으므로 한 번만 펼친다.
            var loaders = new List<IAssetLoader<string, TAsset>>(assetLoaders);

            return new AssetProvider<string, TAsset>(
                assetLoaders: loaders,
                assetCache: new MemoryAssetCache<string, TAsset>(),
                assetValidator: new DefaultAssetValidator<string, TAsset>(),
                assetLoadGate: new SharedAssetLoadGate<string, TAsset>(),
                keyNormalizer: keyNormalizer ?? _InferKeyNormalizer(loaders),
                assetStore: assetStore);
        }
        #endregion

        #region Private - Key Rule
        // 한 provider 는 key 규칙 하나를 쓴다. Release · TryGet 은 loadMode 를 받지 않아 요청마다 규칙을 고를 수 없으므로
        // 두 소스가 섞이면 어느 규칙으로도 한쪽이 틀린다. 조용히 틀리게 두지 않고 조립 시점에 막는다.
        static IAssetKeyNormalizer<string> _InferKeyNormalizer<TAsset>(List<IAssetLoader<string, TAsset>> loaders)
            where TAsset : Object {

            bool hasResources = false;
            bool hasAddressable = false;
            for (int k = 0; k < loaders.Count; k++) {
                // null 로더는 provider 생성자가 원인과 함께 거부한다.
                if (loaders[k] == null) continue;
                if (loaders[k].LoadMode == AssetLoadMode.Resources) hasResources = true;
                if (loaders[k].LoadMode == AssetLoadMode.Addressable) hasAddressable = true;
            }

            if (hasResources && hasAddressable) {
                HLogger.Throw(new System.ArgumentException(
                    "[AssetProviderFactory] Resources and Addressable loaders cannot share one provider. " +
                    "A provider applies one key rule and Release/TryGet carry no loadMode. Create one provider per source.",
                    nameof(loaders)));
            }

            if (hasResources) return new ResourcesKeyNormalizer(string.Empty);
            return new TrimKeyNormalizer();
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-22 (수정) :: Create 의 기본 key 규칙을 로더에서 알아내고 두 소스 혼합을 거부
 *
 * 변경 ::
 * keyNormalizer 가 null 이면 _InferKeyNormalizer 가 로더의 LoadMode 로 정한다. Resources 만이면
 * ResourcesKeyNormalizer(string.Empty), Addressable 만이면 TrimKeyNormalizer, 섞이면 ArgumentException.
 * 로더 목록을 List 로 한 번 펼쳐 추론과 provider 전달에 같이 쓴다.
 *
 * 이유 ::
 * 리뷰 지적 (P1 두 건). 2026-09-21 변경 뒤 Create(Resources 로더) 가 규칙 없이 Trim 을 받아 "Icon/A.png" 가
 * 확장자째 Resources.LoadAsync 로 들어가 실패했다. 수정 전 로더가 확장자를 떼 성공하던 요청이라 회귀였다.
 * 또 규칙이 provider 당 하나가 되면서 두 소스 혼합 provider 는 어떤 규칙으로도 한쪽이 틀리게 됐는데
 * README 는 혼합을 사용법으로 안내하고 있었다. 이전에는 로더마다 스스로 정규화해 혼합이 동작했다.
 *
 * 결과 ::
 * 규칙을 넘기지 않은 Create 는 올바른 규칙을 자동으로 받거나 조립 시점에 원인과 함께 실패한다.
 * 규칙을 직접 넘기면 그 선택을 그대로 따른다 (호출자 책임).
 *
 * 주의 ::
 * 추론한 Resources 규칙은 rootPath 가 없다. rootPath 가 필요하면 CreateResources(rootPath) 를 쓰거나 규칙을 직접 넘긴다.
 * 아래 2026-09-21 항목의 "Create 의 기본 규칙은 Trim" 은 그 시점의 기록이다.
 *
 * =========================================================
 * 2026-09-21 (수정) :: provider 에 key 정규화 규칙을 넣는다
 *
 * 변경 ::
 * CreateResources 는 ResourcesKeyNormalizer(rootPath) 를, CreateAddressable 은 TrimKeyNormalizer 를 넣는다.
 * rootPath 는 로더가 아니라 규칙이 받는다. Create 에 선택 인자 keyNormalizer 를 맨 뒤에 추가했다.
 *
 * 이유 ::
 * key 해석을 로더 안에서 provider 입구로 옮겼다 (IAssetKeyNormalizer Dev Log). 조립 지점이 이 팩토리라 규칙 선택도 여기서 한다.
 *
 * 결과 ::
 * CreateResources / CreateAddressable 호출처(HAudio · HDialogue · HLocalization · HUI)는 바뀌지 않는다.
 *
 * 주의 ::
 * Create 의 기본 규칙은 Trim 이다. Resources 로더만 직접 넘기면서 규칙을 생략하면 확장자 변형이 합쳐지지 않는다.
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
 * 2026-04-25 (최초 설계) :: AssetProviderFactory 초기 구현
 *
 * 실행 객체가 아니라 조립 헬퍼. 프로젝트 기본 구성 (MemoryAssetCache + DefaultAssetValidator
 * + SharedAssetLoadGate) 을 한곳에 모으기 위한 도구. 도메인 코드는 보일러플레이트 없이
 * 한 줄 호출로 provider 조립 가능. 특수 조합은 AssetProvider 생성자 직접 호출로 fallback.
 * =========================================================
 */
#endif
