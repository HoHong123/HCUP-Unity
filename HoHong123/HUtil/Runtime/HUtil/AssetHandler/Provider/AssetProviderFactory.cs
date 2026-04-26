#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 기본 AssetProvider 조합을 만드는 정적 팩토리. 편의 생성 계층.
 *
 * 주요 기능 ::
 * CreateResources<TAsset>(rootPath) — Resources 기본 조합.
 * CreateAddressable<TAsset>() — Addressable 기본 조합.
 * Create<TAsset>(loaders, store) — 사용자 정의 loader 조합.
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
using UnityEngine.Assertions;
using HUtil.AssetHandler.Cache;
using HUtil.AssetHandler.Load;
using HUtil.AssetHandler.Store;
using HUtil.AssetHandler.Validation;

namespace HUtil.AssetHandler.Provider {
    public static class AssetProviderFactory {
        #region Public - Create
        public static AssetProvider<string, TAsset> CreateResources<TAsset>(
            string resourcesRootPath,
            IAssetStore<string, TAsset> assetStore = null)
            where TAsset : Object {

            var assetLoader = new ResourcesAssetLoader<TAsset>(resourcesRootPath);
            return Create(new[] { assetLoader }, assetStore);
        }

        public static AssetProvider<string, TAsset> CreateAddressable<TAsset>(
            IAssetStore<string, TAsset> assetStore = null)
            where TAsset : Object {

            var assetLoader = new AddressableAssetLoader<TAsset>();
            return Create(new[] { assetLoader }, assetStore);
        }

        public static AssetProvider<string, TAsset> Create<TAsset>(
            IEnumerable<IAssetLoader<string, TAsset>> assetLoaders,
            IAssetStore<string, TAsset> assetStore = null)
            where TAsset : Object {

            Assert.IsNotNull(assetLoaders, "[AssetProviderFactory] assetLoaders is null.");

            return new AssetProvider<string, TAsset>(
                assetLoaders: assetLoaders,
                assetCache: new MemoryAssetCache<string, TAsset>(),
                assetValidator: new DefaultAssetValidator<string, TAsset>(),
                assetLoadGate: new SharedAssetLoadGate<string, TAsset>(),
                assetStore: assetStore);
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
 * 2026-04-25 (최초 설계) :: AssetProviderFactory 초기 구현
 * =========================================================
 * 실행 객체가 아니라 조립 헬퍼. 프로젝트 기본 구성 (MemoryAssetCache + DefaultAssetValidator
 * + SharedAssetLoadGate) 을 한곳에 모으기 위한 도구. 도메인 코드는 보일러플레이트 없이
 * 한 줄 호출로 provider 조립 가능. 특수 조합은 AssetProvider 생성자 직접 호출로 fallback.
 * =========================================================
 */
#endif
