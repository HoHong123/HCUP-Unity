using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using HGame.Sound.Core;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * SoundCatalogSO를 인덱싱하는 런타임 레지스트리 스크립트입니다.
 *
 * 주의사항 ::
 * 1. RegisterCatalog와 ReleaseCatalog는 짝을 맞춰 사용해야 합니다.
 * 2. 동일 token 충돌은 같은 엔트리 의미일 때만 허용됩니다.
 * =========================================================
 */
#endif

namespace HGame.Audio.Catalog {
    public sealed partial class SoundCatalogRegistry {
        #region Nested Types
        sealed class EntrySlot {
            public SoundCatalogSO.Entry Entry { get; private set; }
            public int RefCount { get; private set; }

            public EntrySlot(SoundCatalogSO.Entry entry) {
                Entry = entry;
                RefCount = 1;
            }

            public void Retain() {
                RefCount++;
            }

            public int Release() {
                if (RefCount > 0) RefCount--;
                return RefCount;
            }
        }
        #endregion

        #region Fields
        readonly Dictionary<SoundCatalogSO, int> catalogRefTable = new();
        readonly Dictionary<string, EntrySlot> tokenEntryTable = new Dictionary<string, EntrySlot>(StringComparer.Ordinal);
        #endregion

        #region Properties
        public int CatalogCount => catalogRefTable.Count;
        public int EntryCount => tokenEntryTable.Count;
        #endregion

        #region Public - Catalog
        public int RegisterCatalog(SoundCatalogSO catalog) {
            Assert.IsNotNull(catalog, "[SoundCatalogRegistry] catalog is null.");
            if (!catalog) return 0;

            if (catalogRefTable.TryGetValue(catalog, out var currentRefCount)) {
                currentRefCount++;
                catalogRefTable[catalog] = currentRefCount;
                return currentRefCount;
            }

            catalog.BuildCache();
            catalogRefTable.Add(catalog, 1);

            foreach (var entry in catalog.Entries) {
                _RegisterEntry(entry);
            }

            return 1;
        }

        public int ReleaseCatalog(
            SoundCatalogSO catalog,
            List<SoundCatalogSO.Entry> removedEntries = null) {

            Assert.IsNotNull(catalog, "[SoundCatalogRegistry] catalog is null.");
            if (!catalog) return 0;
            if (!catalogRefTable.TryGetValue(catalog, out var currentRefCount)) return 0;

            if (currentRefCount > 1) {
                currentRefCount--;
                catalogRefTable[catalog] = currentRefCount;
                return currentRefCount;
            }

            catalogRefTable.Remove(catalog);

            foreach (var entry in catalog.Entries) {
                if (_ReleaseEntry(entry)) {
                    removedEntries?.Add(entry);
                }
            }

            return 0;
        }

        public void Clear() {
            catalogRefTable.Clear();
            tokenEntryTable.Clear();
            _ClearLegacyCache();
        }
        #endregion

        #region Public - Lookup
        public bool TryGetEntry(string token, out SoundCatalogSO.Entry entry) {
            entry = null;

            string normalizedToken = _NormalizeToken(token);
            if (string.IsNullOrWhiteSpace(normalizedToken)) return false;
            if (!tokenEntryTable.TryGetValue(normalizedToken, out var slot)) return false;

            entry = slot.Entry;
            return entry != null;
        }

        public bool ContainsToken(string token) {
            string normalizedToken = _NormalizeToken(token);
            if (string.IsNullOrWhiteSpace(normalizedToken)) return false;
            return tokenEntryTable.ContainsKey(normalizedToken);
        }
        #endregion

        #region Private - Register
        private void _RegisterEntry(SoundCatalogSO.Entry entry) {
            Assert.IsNotNull(entry, "[SoundCatalogRegistry] entry is null.");
            if (entry == null) return;

            string normalizedToken = _NormalizeToken(entry.Token);
            string normalizedPath = _NormalizePath(entry.Path);

            Assert.IsFalse(
                string.IsNullOrWhiteSpace(normalizedToken),
                $"[SoundCatalogRegistry] Token is empty. key={entry.Key}");
            _RetainEntry(
                tokenEntryTable,
                normalizedToken,
                entry,
                existing => _AssertEquivalentEntry(existing, entry, normalizedToken, normalizedPath),
                duplicateKey => $"[SoundCatalogRegistry] Token collision detected. token={duplicateKey}");

            _RegisterLegacyEntry(entry, normalizedToken, normalizedPath);
        }

        private bool _ReleaseEntry(SoundCatalogSO.Entry entry) {
            Assert.IsNotNull(entry, "[SoundCatalogRegistry] entry is null.");
            if (entry == null) return false;

            bool removed = _ReleaseEntry(tokenEntryTable, _NormalizeToken(entry.Token));
            _ReleaseLegacyEntry(entry, removed);
            return removed;
        }
        #endregion

        #region Private - Entry Slot
        private void _RetainEntry<TKey>(
            Dictionary<TKey, EntrySlot> table,
            TKey key,
            SoundCatalogSO.Entry entry,
            Action<SoundCatalogSO.Entry> assertEquivalent,
            Func<TKey, string> duplicateMessageFactory) {

            if (table.TryGetValue(key, out var slot)) {
                Assert.IsNotNull(slot, duplicateMessageFactory(key));
                Assert.IsNotNull(slot.Entry, duplicateMessageFactory(key));
                assertEquivalent(slot.Entry);
                slot.Retain();
                return;
            }

            table.Add(key, new EntrySlot(entry));
        }

        private bool _ReleaseEntry<TKey>(Dictionary<TKey, EntrySlot> table, TKey key) {
            if (!table.TryGetValue(key, out var slot)) return false;
            if (slot.Release() > 0) return false;
            table.Remove(key);
            return true;
        }
        #endregion

        #region Private - Compare
        private void _AssertEquivalentEntry(
            SoundCatalogSO.Entry existing,
            SoundCatalogSO.Entry incoming,
            string normalizedIncomingToken,
            string normalizedIncomingPath) {

            Assert.IsNotNull(existing, "[SoundCatalogRegistry] existing entry is null.");
            Assert.IsNotNull(incoming, "[SoundCatalogRegistry] incoming entry is null.");
            if (existing == null || incoming == null) return;

            string normalizedExistingToken = _NormalizeToken(existing.Token);
            string normalizedExistingPath = _NormalizePath(existing.Path);

            bool isEquivalent =
                existing.Key.Equals(incoming.Key) &&
                string.Equals(normalizedExistingToken, normalizedIncomingToken, StringComparison.Ordinal) &&
                string.Equals(normalizedExistingPath, normalizedIncomingPath, StringComparison.Ordinal);

            Assert.IsTrue(
                isEquivalent,
                "[SoundCatalogRegistry] Active catalogs contain conflicting entries.");
        }
        #endregion

        #region Private - Normalize
        private string _NormalizeToken(string token) {
            if (string.IsNullOrWhiteSpace(token)) return string.Empty;
            return token.Trim();
        }

        private string _NormalizePath(string path) {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            return path.Replace("\\", "/").Trim().Trim('/');
        }
        #endregion

        #region Private - Legacy Hooks
        partial void _RegisterLegacyEntry(
            SoundCatalogSO.Entry entry,
            string normalizedToken,
            string normalizedPath);

        partial void _ReleaseLegacyEntry(SoundCatalogSO.Entry entry, bool tokenRemoved);
        partial void _ClearLegacyCache();
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. catalog ref count를 관리합니다.
 * 2. token 기준으로 SoundCatalogSO.Entry를 조회합니다.
 * 3. legacy partial이 연결될 수 있는 확장 지점을 제공합니다.
 *
 * 사용법 ::
 * 1. catalog preload 전에 RegisterCatalog를 호출합니다.
 * 2. catalog 사용이 끝나면 ReleaseCatalog로 참조를 줄입니다.
 * 3. token lookup이 필요할 때 TryGetEntry를 사용합니다.
 *
 * 이벤트 ::
 * 1. catalog 등록 시 ref count가 증가합니다.
 * 2. ref count가 0이 되면 관련 entry 인덱스가 제거됩니다.
 *
 * 기타 ::
 * 1. source 로딩은 직접 수행하지 않습니다.
 * 2. core registry는 token-first 원칙을 유지합니다.
 * =========================================================
 */
#endif
