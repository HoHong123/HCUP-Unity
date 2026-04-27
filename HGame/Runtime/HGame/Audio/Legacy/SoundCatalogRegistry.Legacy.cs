using System.Collections.Generic;
using UnityEngine.Assertions;
using HGame.Sound.Core;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * SoundCatalogRegistry의 레거시 int id 인덱스 partial 스크립트입니다.
 *
 * 주의사항 ::
 * 1. core registry의 기준은 token이며 이 파일은 보조 경로입니다.
 * 2. legacy id 테이블은 catalog 등록 해제 흐름과 함께 유지되어야 합니다.
 * =========================================================
 */
#endif

namespace HGame.Audio.Catalog {
    public sealed partial class SoundCatalogRegistry {
        #region Fields
        readonly Dictionary<int, EntrySlot> legacyIdEntryTable = new();
        #endregion

        #region Public - Legacy Lookup
        public bool TryGetEntry(int legacyId, out SoundCatalogSO.Entry entry) {
            entry = null;
            if (!legacyIdEntryTable.TryGetValue(legacyId, out var slot)) return false;

            entry = slot.Entry;
            return entry != null;
        }

        public bool TryGetToken(int legacyId, out string token) {
            token = string.Empty;
            if (!legacyIdEntryTable.TryGetValue(legacyId, out var slot)) return false;
            if (slot.Entry == null) return false;

            token = _NormalizeToken(slot.Entry.Token);
            return !string.IsNullOrWhiteSpace(token);
        }
        #endregion

        #region Private - Legacy Hooks
        partial void _RegisterLegacyEntry(
            SoundCatalogSO.Entry entry,
            string normalizedToken,
            string normalizedPath) {

            _RetainEntry(
                legacyIdEntryTable,
                entry.Key.Id,
                entry,
                existing => _AssertEquivalentEntry(existing, entry, normalizedToken, normalizedPath),
                duplicateKey => $"[SoundCatalogRegistry] Legacy id collision detected. id={duplicateKey}");
        }

        partial void _ReleaseLegacyEntry(SoundCatalogSO.Entry entry, bool tokenRemoved) {
            bool legacyRemoved = _ReleaseEntry(legacyIdEntryTable, entry.Key.Id);
            Assert.AreEqual(
                tokenRemoved,
                legacyRemoved,
                $"[SoundCatalogRegistry] Removal mismatch. token={entry.Token}, id={entry.Key.Id}");
        }

        partial void _ClearLegacyCache() {
            legacyIdEntryTable.Clear();
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. int id 기준 entry lookup을 제공합니다.
 * 2. core registry 수명과 맞춰 legacy cache를 유지합니다.
 *
 * 사용법 ::
 * 1. 구형 사운드 id 조회가 필요한 경우에만 사용합니다.
 * 2. 직접 source 로드 대신 entry 해석 보조로 사용합니다.
 *
 * 이벤트 ::
 * 1. catalog 등록 시 legacy 인덱스도 함께 유지됩니다.
 * 2. catalog 제거 시 legacy 인덱스도 같이 정리됩니다.
 *
 * 기타 ::
 * 1. token-first 본체를 보조하는 partial입니다.
 * 2. 신규 시스템이 안정되면 제거 범위를 판단할 수 있습니다.
 * =========================================================
 */
#endif
