using System;
using System.Collections.Generic;
using HUtil.AssetHandler.Subscription;
using HDiagnosis.Logger;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 메모리 기반 AssetHandler 캐시 구현 스크립트입니다.
 *
 * 주의사항 ::
 * 1. asset 자체 저장뿐 아니라 owner 점유 상태도 함께 추적합니다.
 * 2. 실제 제거 조건은 anonymous dependency와 owner set이 모두 비어야 합니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Cache {
    public sealed class MemoryAssetCache<TKey, TAsset> : IAssetCache<TKey, TAsset> {
        #region Nested Types
        sealed class Item {
            public TAsset Asset;
            public int AnonymousDependency;
            public HashSet<AssetOwnerId> Owners = new();
        }
        #endregion

        #region Fields
        readonly Dictionary<TKey, Item> table = new();
        readonly Dictionary<AssetOwnerId, HashSet<TKey>> ownerTable = new();
        #endregion

        #region Events
        public event Action<TKey, TAsset> OnAssetRemoved;
        #endregion

        #region Public - Load
        public bool TryLoad(TKey key, out TAsset asset) {
            asset = default;
            if (!_TryGetItem(key, out var item, out asset)) {
                return false;
            }

            item.AnonymousDependency++;
            return true;
        }

        public bool TryLoad(TKey key, AssetOwnerId ownerId, out TAsset asset) {
            asset = default;
            if (!ownerId.IsValid) {
                return TryLoad(key, out asset);
            }

            if (!_TryGetItem(key, out var item, out asset)) {
                return false;
            }

            if (!item.Owners.Add(ownerId)) {
                return true;
            }

            _RegisterOwnerKey(ownerId, key);
            return true;
        }
        #endregion

        #region Public - Get
        public bool TryGet(TKey key, out TAsset asset) {
            asset = default;
            return _TryGetItem(key, out _, out asset);
        }
        #endregion

        #region Public - Save
        public bool Save(TKey key, TAsset asset) {
            if (ReferenceEquals(asset, null)) return false;

            if (table.TryGetValue(key, out var item)) {
                if (ReferenceEquals(item.Asset, asset)) {
                    item.AnonymousDependency++;
                    return true;
                }

                HLogger.Error($"[AssetCache] Save rejected. Key '{key}' already holds a different asset.");
                return false;
            }

            table[key] = new Item {
                Asset = asset,
                AnonymousDependency = 1,
            };

            return true;
        }

        public bool Save(TKey key, TAsset asset, AssetOwnerId ownerId) {
            if (!ownerId.IsValid) return Save(key, asset);

            if (ReferenceEquals(asset, null)) return false;

            if (table.TryGetValue(key, out var item)) {
                if (ReferenceEquals(item.Asset, asset)) {
                    if (item.Owners.Add(ownerId)) _RegisterOwnerKey(ownerId, key);
                    return true;
                }

                HLogger.Error($"[AssetCache] Save rejected. Key '{key}' already holds a different asset.");
                return false;
            }

            var newItem = new Item { Asset = asset };
            newItem.Owners.Add(ownerId);
            table[key] = newItem;

            _RegisterOwnerKey(ownerId, key);
            return true;
        }
        #endregion

        #region Public - Release
        public bool Release(TKey key) {
            if (!table.TryGetValue(key, out var item) || ReferenceEquals(item.Asset, null)) return false;
            if (item.AnonymousDependency < 1) return false;
            item.AnonymousDependency--;
            return _TryRemoveItem(key, item);
        }

        public bool Release(TKey key, AssetOwnerId ownerId) {
            if (!ownerId.IsValid) return Release(key);

            if (!table.TryGetValue(key, out var item) || ReferenceEquals(item.Asset, null)) return false;
            if (!item.Owners.Remove(ownerId)) return false;

            _UnregisterOwnerKey(ownerId, key);
            return _TryRemoveItem(key, item);
        }

        public int ReleaseOwner(AssetOwnerId ownerId) {
            if (!ownerId.IsValid) return 0;
            if (!ownerTable.TryGetValue(ownerId, out var keys)) return 0;

            var releaseKeys = new List<TKey>(keys);
            int releasedCount = 0;

            ownerTable.Remove(ownerId);

            foreach (var key in releaseKeys) {
                if (!table.TryGetValue(key, out var item) || ReferenceEquals(item.Asset, null)) continue;
                if (!item.Owners.Remove(ownerId)) continue;

                releasedCount++;
                _TryRemoveItem(key, item);
            }

            return releasedCount;
        }

        public void ReleaseAll() {
            _ClearItems();
        }

        public void Clear() {
            _ClearItems();
        }
        #endregion

        #region Private - Item
        private bool _TryGetItem(TKey key, out Item item, out TAsset asset) {
            asset = default;
            item = null;

            if (!table.TryGetValue(key, out item) || ReferenceEquals(item.Asset, null)) return false;

            asset = item.Asset;
            return true;
        }

        private bool _TryRemoveItem(TKey key, Item item) {
            if (item.AnonymousDependency > 0) return false;
            if (item.Owners.Count > 0) return false;
            return _RemoveItem(key, item);
        }

        private bool _RemoveItem(TKey key, Item item) {
            if (!table.Remove(key)) return false;
            _NotifyRemoved(key, item.Asset);
            return true;
        }
        #endregion

        #region Private - Owner
        private void _RegisterOwnerKey(AssetOwnerId ownerId, TKey key) {
            if (!ownerTable.TryGetValue(ownerId, out var keys)) {
                keys = new HashSet<TKey>();
                ownerTable[ownerId] = keys;
            }

            keys.Add(key);
        }

        private void _UnregisterOwnerKey(AssetOwnerId ownerId, TKey key) {
            if (!ownerTable.TryGetValue(ownerId, out var keys)) return;

            keys.Remove(key);
            if (keys.Count < 1) ownerTable.Remove(ownerId);
        }
        #endregion

        #region Private - Clear
        private void _ClearItems() {
            if (table.Count < 1) return;

            var removeItems = new List<KeyValuePair<TKey, Item>>(table);
            table.Clear();
            ownerTable.Clear();

            foreach (var pair in removeItems) {
                _NotifyRemoved(pair.Key, pair.Value.Asset);
            }
        }
        #endregion

        #region Private - Event
        private void _NotifyRemoved(TKey key, TAsset asset) {
            if (ReferenceEquals(asset, null)) return;
            OnAssetRemoved?.Invoke(key, asset);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. 메모리 딕셔너리 기반 조회와 저장을 제공합니다.
 * 2. owner별 key 테이블을 유지합니다.
 * 3. asset 제거 시 이벤트를 발생시킵니다.
 *
 * 사용법 ::
 * 1. 기본 AssetProvider 조합에서 기본 cache로 사용합니다.
 * 2. owner가 있는 요청은 Save와 TryLoad에서 점유를 갱신합니다.
 *
 * 이벤트 ::
 * 1. asset이 실제 테이블에서 제거될 때 OnAssetRemoved가 발생합니다.
 * 2. owner release 시 관련 key들이 함께 정리될 수 있습니다.
 *
 * 기타 ::
 * 1. lease 계층과는 독립적으로 동작합니다.
 * 2. 가장 얇은 기본 캐시이지만 owner-aware 구조를 포함합니다.
 * =========================================================
 * @Jason - PKH
 * 양방향 멀티탭 패턴
 *  - Item.Owners: "이 key를 누가 잡고 있나?"
 *  - ownerTable: "이 owner가 뭘 잡고 있나?" 일괄 해제 성능을 위한 역인덱스.
 * =========================================================
 */
#endif
