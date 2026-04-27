using Cysharp.Threading.Tasks;
using UnityEngine;
using HUtil.AssetHandler.Data;
using HUtil.AssetHandler.Subscription;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 레거시 int id를 token으로 변환하는 SoundClipRepository partial 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 신규 token 경로를 대체하는 것이 아니라 호환 목적입니다.
 * 2. legacy id 해석 실패 시 본 저장소 흐름으로 진입하지 않습니다.
 * =========================================================
 */
#endif

namespace HGame.Audio.Repository {
    public sealed partial class SoundClipRepository {
        #region Public - Legacy Get
        public bool TryGet(int legacyId, out AudioClip clip) {
            clip = null;
            if (!_TryResolveLegacyId(legacyId, out string token)) return false;
            return TryGet(token, out clip);
        }

        public UniTask<AudioClip> GetOrLoadAsync(
            int legacyId,
            AssetOwnerId ownerId = default,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst) {

            if (!_TryResolveLegacyId(legacyId, out string token)) {
                return UniTask.FromResult<AudioClip>(null);
            }

            return GetOrLoadAsync(token, ownerId, fetchMode);
        }
        #endregion

        #region Public - Legacy Prewarm
        public async UniTask PrewarmLegacyIdAsync(
            int legacyId,
            AssetOwnerId ownerId = default,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst) {

            await GetOrLoadAsync(legacyId, ownerId, fetchMode);
        }
        #endregion

        #region Public - Legacy Release
        public bool Release(int legacyId) {
            if (!_TryResolveLegacyId(legacyId, out string token)) return false;
            return Release(token);
        }

        public bool Release(int legacyId, AssetOwnerId ownerId) {
            if (!_TryResolveLegacyId(legacyId, out string token)) return false;
            return Release(token, ownerId);
        }
        #endregion

        #region Private - Legacy Resolve
        private bool _TryResolveLegacyId(int legacyId, out string token) {
            token = string.Empty;
            return catalogRegistry.TryGetToken(legacyId, out token);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. legacy id 기준 조회와 로드 경로를 제공합니다.
 * 2. 변환 뒤에는 본 저장소의 token 흐름을 재사용합니다.
 *
 * 사용법 ::
 * 1. 구형 호출부 유지가 필요할 때만 사용합니다.
 * 2. 실제 동작은 _TryResolveLegacyId 후 본 저장소 메서드로 연결됩니다.
 *
 * 이벤트 ::
 * 1. 별도의 이벤트는 없습니다.
 * 2. 성공 시 token-first 저장소 경로가 이어집니다.
 *
 * 기타 ::
 * 1. legacy 범위를 partial로 한정합니다.
 * 2. 신규 매니저 본체와 책임을 분리합니다.
 * =========================================================
 */
#endif
