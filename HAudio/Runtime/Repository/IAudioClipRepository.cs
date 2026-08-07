using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using HAudio.Core;
using HResource.Data;
using HResource.Subscription;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Audio 도메인의 오디오 클립 로드 진입 계약 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 신규 표준 진입은 string token 기준입니다.
 * 2. 구현체는 load mode와 catalog 해석 정책을 내부에서 처리합니다.
 * 3. 저장소를 만든 쪽(AudioManager)은 OnDestroy에서 Dispose를 호출할 책임을 집니다.
 * =========================================================
 */
#endif

namespace HAudio.Repository {
    public interface IAudioClipRepository : IDisposable {
        AssetLoadMode LoadMode { get; }

        // uid 축 — 재생 경로. 문자열 정규화·할당이 없다.
        bool TryGet(int uid, out AudioClip clip);

        UniTask<AudioClip> GetOrLoadAsync(
            int uid,
            AssetOwnerId ownerId = default,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst);

        UniTask PrewarmTokenAsync(
            int uid,
            AssetOwnerId ownerId = default,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst);

        bool Release(int uid);
        bool Release(int uid, AssetOwnerId ownerId);

        // token 축 — 저작·에디터·디버그.
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
            AudioCatalogSO catalog,
            AssetOwnerId ownerId = default,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst);

        bool Release(string token);
        bool Release(string token, AssetOwnerId ownerId);
        void ReleaseCatalog(AudioCatalogSO catalog);
        void ReleaseCatalog(AudioCatalogSO catalog, AssetOwnerId ownerId);
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
 * 1. AudioManager는 source loader 대신 이 인터페이스를 참조합니다.
 * 2. token 기준 API를 우선 사용합니다.
 *
 * 이벤트 ::
 * 1. 직접 발생시키는 이벤트는 없습니다.
 * 2. 실제 로딩 이벤트는 구현체와 하위 provider가 담당합니다.
 *
 * 기타 ::
 * 1. 기준 키는 string token 단일 체계입니다.
 * 2. 도메인 경계를 고정하기 위한 인터페이스입니다.
 * =========================================================
 */
#endif
