using System;
using System.Collections.Generic;
using HResource.Subscription;
using HDiagnosis.Logger;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 메모리 기반 AssetHandler 캐시 기본 구현. 양방향 멀티탭 + AnonymousDependency 통합 reference counting.
 *
 * 주요 기능 ::
 * key → Item (Asset + AnonymousDependency 카운터 + Owners 별 점유 횟수 Dictionary) 메인 테이블.
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
 * 조회(TryGet)는 점유를 바꾸지 않는다 — 점유 등록은 Save 만 담당한다.
 * Release 가 false 를 돌려주는 두 경우 중 "점유가 애초에 없음" 은 경고를 남긴다.
 *
 * 양방향 멀티탭 패턴 ::
 * - Item.Owners: "이 key 를 누가 잡고 있나?"
 * - ownerTable[ownerId] → keys: "이 owner 가 뭘 잡고 있나?" 일괄 해제 성능을 위한 역인덱스.
 * =========================================================
 */
#endif

namespace HResource.Cache {
    public sealed class MemoryAssetCache<TKey, TAsset> : IAssetCache<TKey, TAsset> {
        #region Nested Types
        sealed class Item {
            public TAsset Asset;
            public int AnonymousDependency;
            // owner 별 점유 횟수. HashSet 이었을 때는 같은 owner 의 2회 점유(직접 prewarm +
            // catalog prewarm 등)가 1로 접혀 한 번의 Release 로 조기 해제되는 사고가 있었다.
            public Dictionary<AssetOwnerId, int> Owners = new();
        }
        #endregion

        #region Fields
        readonly Dictionary<TKey, Item> table = new();
        readonly Dictionary<AssetOwnerId, HashSet<TKey>> ownerTable = new();
        #endregion

        #region Events
        public event Action<TKey, TAsset> OnAssetRemoved;
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
                    _AddOwnerDependency(item, ownerId, key);
                    return true;
                }

                HLogger.Error($"[AssetCache] Save rejected. Key '{key}' already holds a different asset.");
                return false;
            }

            var newItem = new Item { Asset = asset };
            table[key] = newItem;

            _AddOwnerDependency(newItem, ownerId, key);
            return true;
        }
        #endregion

        #region Public - Release
        public bool Release(TKey key) {
            if (!table.TryGetValue(key, out var item) || ReferenceEquals(item.Asset, null)) {
                _WarnUnpairedRelease(key, "no cache entry");
                return false;
            }
            if (item.AnonymousDependency < 1) {
                _WarnUnpairedRelease(key, "no anonymous dependency registered");
                return false;
            }
            item.AnonymousDependency--;
            return _TryRemoveItem(key, item);
        }

        public bool Release(TKey key, AssetOwnerId ownerId) {
            if (!ownerId.IsValid) return Release(key);

            if (!table.TryGetValue(key, out var item) || ReferenceEquals(item.Asset, null)) {
                _WarnUnpairedRelease(key, $"no cache entry (ownerId={ownerId})");
                return false;
            }
            if (!item.Owners.TryGetValue(ownerId, out int dependency)) {
                _WarnUnpairedRelease(key, $"ownerId={ownerId} holds no dependency on this key");
                return false;
            }

            if (dependency > 1) {
                item.Owners[ownerId] = dependency - 1;
                return false;
            }

            item.Owners.Remove(ownerId);
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
                // owner 일괄 회수는 점유 횟수와 무관하게 해당 owner 의 점유 전부를 내려놓는다.
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
        private void _AddOwnerDependency(Item item, AssetOwnerId ownerId, TKey key) {
            if (item.Owners.TryGetValue(ownerId, out int dependency)) {
                item.Owners[ownerId] = dependency + 1;
                return;
            }

            item.Owners[ownerId] = 1;
            _RegisterOwnerKey(ownerId, key);
        }

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

            // OnAssetRemoved 구독자가 알림 도중 Save 를 다시 호출하면 그 항목은 Clear 를
            // 통과해 살아남는다 (구독자 입장에서는 방금 비운 캐시에 유령이 남는다).
            // 잔존 항목이 없어질 때까지 반복하고, 폭주는 상한으로 끊는다.
            const int MAX_CLEAR_PASSES = 8;
            for (int pass = 0; pass < MAX_CLEAR_PASSES; pass++) {
                if (table.Count < 1) return;

                var removeItems = new List<KeyValuePair<TKey, Item>>(table);
                table.Clear();
                ownerTable.Clear();

                foreach (var pair in removeItems) {
                    _NotifyRemoved(pair.Key, pair.Value.Asset);
                }
            }

            if (table.Count > 0) {
                HLogger.Error(
                    $"[AssetCache] Clear did not converge: {table.Count} item(s) were re-saved during removal notifications.");
            }
        }
        #endregion

        #region Private - Diagnostics
        // Release 의 false 는 두 가지 뜻이 겹쳐 있다 — "아직 다른 점유가 남아 살아있다"(정상)
        // 와 "애초에 이 호출자의 점유가 없다"(획득/해제 짝 오류). 반환값만으로는 호출자가
        // 둘을 구분할 수 없으므로, 후자에 한해 경고를 남겨 짝 오류를 관측 가능하게 한다.
        // Error 가 아니라 Warning 인 이유 : ReleaseAll/Clear 이후의 뒤늦은 Release 처럼
        // 회복 가능한 정리 순서 문제도 이 경로로 들어오기 때문.
        private void _WarnUnpairedRelease(TKey key, string reason) {
            HLogger.Warning($"[AssetCache] Unpaired release for key '{key}' — {reason}.");
        }
        #endregion

        #region Private - Event
        // 멀티캐스트 델리게이트는 구독자 하나가 던지면 뒤 구독자를 호출하지 않는다. 이 이벤트는
        // provider 의 로더 핸들 회수를 트리거하는 정리(clean-up) 신호라, 중간에 끊기면 남은
        // 항목들은 이미 table 에서 제거된 뒤라서 재시도 대상도 아니고 핸들만 살아남는다.
        // (케이스 리포트 07 EXC-2 — SceneLoader._InvokeSafely 와 동일 처방)
        private void _NotifyRemoved(TKey key, TAsset asset) {
            if (ReferenceEquals(asset, null)) return;

            var handlers = OnAssetRemoved;
            if (handlers == null) return;

            foreach (Action<TKey, TAsset> handler in handlers.GetInvocationList()) {
                try {
                    handler(key, asset);
                }
                catch (Exception e) {
                    HLogger.Error($"[AssetCache] OnAssetRemoved subscriber threw for key '{key}': {e}");
                }
            }
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
 * 2026-08-06 (수정 2) :: OnAssetRemoved 구독자 예외 격리 (케이스 리포트 07 EXC-2)
 * =========================================================
 * 변경 ::
 * _NotifyRemoved 가 OnAssetRemoved?.Invoke 한 번으로 끝내던 것을 GetInvocationList() 순회 +
 * 구독자별 try/catch 로 교체.
 *
 * 이유 ::
 * _ClearItems 는 table 을 먼저 비운 뒤 복사본을 순회하며 알림을 쏜다. 구독자
 * (AssetProvider._OnAssetRemoved → Addressables.Release) 가 하나라도 던지면 멀티캐스트가
 * 끊기고 foreach 도 중단되는데, 남은 항목은 이미 table 에서 사라져 재시도 대상이 아니다.
 * 결과는 "캐시에는 없고 로더 핸들만 사는" 미아가 대량 발생. Dispose 가 곧 ReleaseAll 이라
 * 모든 소유자의 OnDestroy 가 사정권이었다. 처방은 리포트 04 의 SceneLoader._InvokeSafely 와 동일.
 *
 * =========================================================
 * 2026-08-06 (수정) :: TryLoad 2종 제거 + 짝 없는 Release 경고 (케이스 리포트 01 TST-2/NEG-2)
 * =========================================================
 * 변경 ::
 * 1) TryLoad(key) / TryLoad(key, ownerId) 구현 삭제 — IAssetReader 계약에서 함께 제거.
 * 2) Release 2종에 _WarnUnpairedRelease 추가. "점유가 없어서 false" 인 경로에만 경고.
 *
 * 이유 ::
 * 1) TryLoad 는 "조회하며 점유를 올리는" 의미라 provider 의 호출자별 1:1 등록 규약과
 *    이중 카운트가 난다. 호출자 0건인 지금 지우는 편이 후속 오용보다 싸다.
 * 2) Release 의 false 는 "아직 남아있음"(정상) 과 "애초에 없음"(짝 오류) 두 뜻이 겹쳐
 *    호출자가 구분할 수 없었다. 반환 타입을 바꾸면 공개 API 파괴 변경이므로, 계약은
 *    유지한 채 후자에만 경고를 붙여 관측 가능성만 확보했다.
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
