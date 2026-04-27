using Cysharp.Threading.Tasks;
using UnityEngine;
using HGame.Sound.Core;
using HUtil.AssetHandler.Data;
using HUtil.AssetHandler.Subscription;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Audio 도메인의 오디오 클립 로드 진입 계약 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 신규 표준 진입은 string token 기준입니다.
 * 2. 구현체는 load mode와 catalog 해석 정책을 내부에서 처리합니다.
 * =========================================================
 */
#endif

namespace HGame.Audio.Repository {
    public partial interface ISoundClipRepository {
        AssetLoadMode LoadMode { get; }

        bool TryGet(string token, out AudioClip clip);

        UniTask<AudioClip> GetOrLoadAsync(
            string token,
            AssetOwnerId ownerId = default,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst);

        UniTask PrewarmTokenAsync(
            string token,
            AssetOwnerId ownerId = default,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst);

        UniTask PrewarmCatalogAsync(
            SoundCatalogSO catalog,
            AssetOwnerId ownerId = default,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst);

        bool Release(string token);
        bool Release(string token, AssetOwnerId ownerId);
        void ReleaseCatalog(SoundCatalogSO catalog);
        void ReleaseCatalog(SoundCatalogSO catalog, AssetOwnerId ownerId);
        int ReleaseOwner(AssetOwnerId ownerId);
        void ReleaseAll();
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. 즉시 조회, 로드, preload, release 계약을 제공합니다.
 * 2. owner 기반 release 경로를 노출합니다.
 *
 * 사용법 ::
 * 1. SoundManager는 source loader 대신 이 인터페이스를 참조합니다.
 * 2. token 기준 API를 우선 사용합니다.
 *
 * 이벤트 ::
 * 1. 직접 발생시키는 이벤트는 없습니다.
 * 2. 실제 로딩 이벤트는 구현체와 하위 provider가 담당합니다.
 *
 * 기타 ::
 * 1. 레거시 int 경로는 partial 인터페이스로 분리되어 있습니다.
 * 2. 도메인 경계를 고정하기 위한 인터페이스입니다.
 * =========================================================
 */
#endif
