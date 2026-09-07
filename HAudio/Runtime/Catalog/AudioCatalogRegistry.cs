using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using HAudio.Core;
using HDiagnosis.Logger;

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

namespace HAudio.Catalog {
    public sealed class AudioCatalogRegistry {
        #region Nested Types
        sealed class EntrySlot {
            public AudioCatalogSO.Entry Entry { get; private set; }
            public int RefCount { get; private set; }

            public EntrySlot(AudioCatalogSO.Entry entry) {
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
        readonly Dictionary<AudioCatalogSO, int> catalogRefTable = new();
        readonly Dictionary<string, EntrySlot> tokenEntryTable = new Dictionary<string, EntrySlot>(StringComparer.Ordinal);
        // uid 축 인덱스. token 축과 같은 EntrySlot 을 가리키지 않고 별도 슬롯을 쓴다.
        // 두 축의 retain/release 를 독립적으로 세야 한 축의 해제가 다른 축을 무너뜨리지 않는다.
        // 재생 경로는 이쪽만 탄다 (int 해시 = 항등, 할당 0).
        readonly Dictionary<int, EntrySlot> uidEntryTable = new();
        #endregion

        #region Properties
        public int CatalogCount => catalogRefTable.Count;
        public int EntryCount => tokenEntryTable.Count;
        public int UidEntryCount => uidEntryTable.Count;
        #endregion

        #region Public - Catalog
        public int RegisterCatalog(AudioCatalogSO catalog) {
            Assert.IsNotNull(catalog, "[AudioCatalogRegistry] catalog is null.");
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
            AudioCatalogSO catalog,
            List<AudioCatalogSO.Entry> removedEntries = null) {

            Assert.IsNotNull(catalog, "[AudioCatalogRegistry] catalog is null.");
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
            uidEntryTable.Clear();
        }
        #endregion

        #region Public - Lookup
        /// <summary> uid 로 조회한다. 재생 경로의 기본 진입점 - 문자열을 만지지 않는다. </summary>
        public bool TryGetEntry(int uid, out AudioCatalogSO.Entry entry) {
            entry = null;
            if (uid == 0) return false;
            if (!uidEntryTable.TryGetValue(uid, out var slot)) return false;

            entry = slot.Entry;
            return entry != null;
        }

        /// <summary> token 으로 조회한다. 저작·에디터·디버그 축. </summary>
        public bool TryGetEntry(string token, out AudioCatalogSO.Entry entry) {
            entry = null;

            string normalizedToken = _NormalizeToken(token);
            if (string.IsNullOrWhiteSpace(normalizedToken)) return false;
            if (!tokenEntryTable.TryGetValue(normalizedToken, out var slot)) return false;

            entry = slot.Entry;
            return entry != null;
        }

        public bool ContainsUid(int uid) {
            if (uid == 0) return false;
            return uidEntryTable.ContainsKey(uid);
        }

        public bool ContainsToken(string token) {
            string normalizedToken = _NormalizeToken(token);
            if (string.IsNullOrWhiteSpace(normalizedToken)) return false;
            return tokenEntryTable.ContainsKey(normalizedToken);
        }
        #endregion

        #region Private - Register
        private void _RegisterEntry(AudioCatalogSO.Entry entry) {
            Assert.IsNotNull(entry, "[AudioCatalogRegistry] entry is null.");
            if (entry == null) return;

            string normalizedToken = _NormalizeToken(entry.Token);
            string normalizedPath = _NormalizePath(entry.Path);

            Assert.IsFalse(
                string.IsNullOrWhiteSpace(normalizedToken),
                $"[AudioCatalogRegistry] Token is empty. path={entry.Path}");
            _RetainEntry(
                tokenEntryTable,
                normalizedToken,
                entry,
                existing => _AssertEquivalentEntry(existing, entry, normalizedToken, normalizedPath),
                duplicateKey => $"[AudioCatalogRegistry] Token collision detected. token={duplicateKey}");

            if (_TryResolveUid(entry, normalizedToken, out int uid)) {
                _RetainEntry(
                    uidEntryTable,
                    uid,
                    entry,
                    existing => _AssertEquivalentEntry(existing, entry, normalizedToken, normalizedPath),
                    duplicateKey => $"[AudioCatalogRegistry] Uid collision detected. uid={duplicateKey}");
            }
        }

        private bool _ReleaseEntry(AudioCatalogSO.Entry entry) {
            Assert.IsNotNull(entry, "[AudioCatalogRegistry] entry is null.");
            if (entry == null) return false;

            string normalizedToken = _NormalizeToken(entry.Token);

            // token 축의 제거 여부를 반환값으로 삼는다 - 자산 해제의 기준은 종전과 동일하게 token 축이다.
            // uid 축은 같은 시점에 함께 내려놓기만 하고 판정에는 관여하지 않는다.
            if (_TryResolveUid(entry, normalizedToken, out int uid)) {
                _ReleaseEntry(uidEntryTable, uid);
            }

            return _ReleaseEntry(tokenEntryTable, normalizedToken);
        }

        // uid 는 Entry.uid 필드가 정본이다. 다만 필드가 도입되기 전에 만들어진 카탈로그는 0 이므로,
        // 그 경우에 한해 token 규약("{uid}_{이름}")에서 복구한다 - 등록 시점 1회뿐이고
        // 재생 경로에서는 절대 파싱하지 않는다.
        private bool _TryResolveUid(AudioCatalogSO.Entry entry, string normalizedToken, out int uid) {
            uid = entry.Uid;

            if (uid != 0) {
                // 필드와 token 이 어긋나면 둘 중 하나가 손상된 것이다. 조용히 한쪽을 택하지 않는다.
                if (AudioCatalogSO.TryParseUid(normalizedToken, out int parsedUid) && parsedUid != uid) {
                    HLogger.Error(
                        $"[AudioCatalogRegistry] Uid mismatch between field and token. uid={uid}, token={normalizedToken}. Regenerate the catalog.");
                    return false;
                }
                return true;
            }

            if (!AudioCatalogSO.TryParseUid(normalizedToken, out uid)) {
                HLogger.Error(
                    $"[AudioCatalogRegistry] Entry has no uid and its token does not follow the \"{{uid}}_{{name}}\" convention. token={normalizedToken}");
                return false;
            }

            HLogger.Warning(
                $"[AudioCatalogRegistry] Entry uid was recovered from the token. Regenerate the catalog to persist it. token={normalizedToken}");
            return true;
        }
        #endregion

        #region Private - Entry Slot
        private void _RetainEntry<TKey>(
            Dictionary<TKey, EntrySlot> table,
            TKey key,
            AudioCatalogSO.Entry entry,
            Action<AudioCatalogSO.Entry> assertEquivalent,
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
            AudioCatalogSO.Entry existing,
            AudioCatalogSO.Entry incoming,
            string normalizedIncomingToken,
            string normalizedIncomingPath) {

            Assert.IsNotNull(existing, "[AudioCatalogRegistry] existing entry is null.");
            Assert.IsNotNull(incoming, "[AudioCatalogRegistry] incoming entry is null.");
            if (existing == null || incoming == null) return;

            string normalizedExistingToken = _NormalizeToken(existing.Token);
            string normalizedExistingPath = _NormalizePath(existing.Path);

            bool isEquivalent =
                existing.Major == incoming.Major &&
                string.Equals(normalizedExistingToken, normalizedIncomingToken, StringComparison.Ordinal) &&
                string.Equals(normalizedExistingPath, normalizedIncomingPath, StringComparison.Ordinal);

            Assert.IsTrue(
                isEquivalent,
                "[AudioCatalogRegistry] Active catalogs contain conflicting entries.");
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

    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. catalog ref count를 관리합니다.
 * 2. token 기준으로 AudioCatalogSO.Entry를 조회합니다.
 * 3. 활성 catalog 집합과 token 인덱스를 함께 유지합니다.
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
