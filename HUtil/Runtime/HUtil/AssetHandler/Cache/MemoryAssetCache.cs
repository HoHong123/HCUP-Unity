using System;
using System.Collections.Generic;
using HUtil.AssetHandler.Subscription;
using HDiagnosis.Logger;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 메모리 기반 AssetHandler 캐시 기본 구현. 양방향 멀티탭 + AnonymousDependency 통합 reference counting.
 *
 * 주요 기능 ::
 * key → Item (Asset + AnonymousDependency 카운터 + Owners HashSet) 메인 테이블.
 * ownerId → key HashSet 역인덱스 (ReleaseOwner 일괄 회수용).
 * OnAssetRemoved 이벤트 — 실제 테이블 제거 시점 신호 (provider 의 release 연쇄 trigger).
 *
 * 사용법 ::
 * AssetProviderFactory.Create 에서 기본 cache 로 자동 주입. 5 가지 release 메서드 (단일 key /
 * 단일 owner / 전체 / 익명 카운터 단순 감소) 가 점유 단위와 의미의 조합.
 *
 * 주의 ::
 * 실제 제거 조건은 AnonymousDependency == 0 + Owners.Count == 0 (둘 다 비어야 함).
 * lease 계층과 독립 동작. 가장 얇은 기본 cache 이지만 owner-aware 구조를 포함하여
 * 다중 호출자 점유 추적 가능.
 *
 * 양방향 멀티탭 패턴 ::
 * - Item.Owners: "이 key 를 누가 잡고 있나?"
 * - ownerTable[ownerId] → keys: "이 owner 가 뭘 잡고 있나?" 일괄 해제 성능을 위한 역인덱스.
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
 * Dev Log
 * =========================================================
 *
 * =========================================================
 * 2026-04-26 (수정) :: 헤더 형틀 통합 + Dev Log 형식 도입
 * =========================================================
 * 변경 ::
 * 기존 헤더 (상단 도입+주의사항 + 하단 주요기능/사용법/이벤트/기타 + 별도 양방향 멀티탭 설명)
 * 을 한 곳에 통합하여 §11 형틀 통일 (양방향 멀티탭 설명도 헤더에 흡수). 하단 Dev Log 영역
 * 추가. 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드.
 *
 * 이유 ::
 * 글로벌 CLAUDE.md §11 룰 일괄 적용. 자료구조 결정의 근거 (양방향 멀티탭) 를 헤더에 두어
 * reader 가 코드 진입 직전에 자료구조 의도를 파악할 수 있도록.
 *
 * =========================================================
 * 2026-04-25 (최초 설계) :: MemoryAssetCache 초기 구현
 * =========================================================
 * AssetHandler 의 reference counting 산실. Item.Owners (소유자 미지정 호출의 카운터인
 * AnonymousDependency 와 함께) + ownerTable 역인덱스 두 자료구조로 owner-aware + 익명 호출
 * 두 경로를 한 자료구조에서 통합 추적. 실제 제거 조건은 두 카운터가 모두 비었을 때.
 *
 * ReleaseOwner(ownerId) 가 ownerTable 역인덱스를 활용해 owner 가 잡은 모든 key 를 한 번에
 * 회수 — 시간복잡도가 점유 수에 정비례. cache 제거 시 OnAssetRemoved 이벤트로 provider
 * 의 source release 연쇄가 이어짐 (Cache 와 Loader 의 결합도 0).
 *
 * Save 동일 key 에 다른 asset 시도는 Reject + LogError — silent overwrite 방지.
 * Save 동일 key 에 같은 asset 재시도는 점유 카운터만 증가 (idempotent 하지 않음 — 의도적).
 * =========================================================
 */
#endif
