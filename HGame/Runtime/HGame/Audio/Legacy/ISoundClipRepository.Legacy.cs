using Cysharp.Threading.Tasks;
using UnityEngine;
using HUtil.AssetHandler.Data;
using HUtil.AssetHandler.Subscription;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 레거시 int id 진입을 위한 partial 인터페이스 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 신규 코드의 기본 경로로 사용하지 않는 것을 전제로 합니다.
 * 2. token-first 본체와 섞이지 않도록 partial로 분리되어 있습니다.
 * =========================================================
 */
#endif

namespace HGame.Audio.Repository {
    public partial interface ISoundClipRepository {
        bool TryGet(int legacyId, out AudioClip clip);

        UniTask<AudioClip> GetOrLoadAsync(
            int legacyId,
            AssetOwnerId ownerId = default,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst);

        UniTask PrewarmLegacyIdAsync(
            int legacyId,
            AssetOwnerId ownerId = default,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst);

        bool Release(int legacyId);
        bool Release(int legacyId, AssetOwnerId ownerId);
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. legacy id 기준 TryGet과 GetOrLoadAsync를 제공합니다.
 * 2. legacy preload와 release 계약을 제공합니다.
 *
 * 사용법 ::
 * 1. 구형 호출부를 유지해야 할 때만 참조합니다.
 * 2. 내부적으로는 token 경로로 변환하는 구현과 연결됩니다.
 *
 * 이벤트 ::
 * 1. 직접 이벤트를 발생시키지 않습니다.
 * 2. 실제 로딩 이벤트는 본 저장소 구현이 담당합니다.
 *
 * 기타 ::
 * 1. 신규 시스템 전환 시 제거 후보가 될 수 있습니다.
 * 2. 레거시 범위를 명확히 드러내기 위한 분리 파일입니다.
 * =========================================================
 */
#endif
