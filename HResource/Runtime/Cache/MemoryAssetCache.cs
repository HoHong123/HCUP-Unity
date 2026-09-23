using System;
using System.Collections.Generic;
using HResource.Subscription;
using HDiagnosis.Logger;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 메모리 기반 AssetHandler 캐시 기본 구현. key 별 소유자 수 + owner 별 점유 집합.
 *
 * 주요 기능 ::
 * key → Item (Asset + 그 key 를 잡고 있는 소유자 수) 메인 테이블.
 * ownerId → key HashSet 역인덱스 (점유의 정본. 중복 판정과 ReleaseOwner 일괄 회수).
 * OnAssetRemoved 이벤트 - 실제 테이블 제거 시점 신호 (provider 의 release 연쇄 trigger).
 *
 * 사용법 ::
 * AssetProviderFactory.Create 에서 기본 cache 로 자동 주입. 4 가지 release 메서드
 * (Release / ReleaseOwner / ReleaseAll / Clear) 가 점유 단위의 조합.
 *
 * 주의 ::
 * 실제 제거 조건은 OwnerCount == 0 이다. 익명 축은 2026-09-04 에 제거됐다.
 * leash 계층과 독립 동작. 가장 얇은 기본 cache 이지만 owner-aware 구조를 포함하여
 * 다중 호출자 점유 추적 가능.
 * 조회(TryGet)는 점유를 바꾸지 않는다 - 점유 등록은 Save 만 담당한다.
 * Release 가 false 를 돌려주는 두 경우 중 "점유가 애초에 없음" 은 경고를 남긴다.
 *
 * 양방향 멀티탭 패턴 ::
 * - Item.OwnerCount: "이 key 를 몇 소유자가 잡고 있나?" 제거 판정에는 수만 필요하다.
 * - ownerTable[ownerId] → keys: "이 owner 가 뭘 잡고 있나?" 누가 잡았는지는 에디터 진단이 이것을 뒤집어 얻는다.
 * =========================================================
 */
#endif

namespace HResource.Cache {
    public sealed class MemoryAssetCache<TKey, TAsset> : IAssetCache<TKey, TAsset>
#if UNITY_EDITOR
        , IAssetCacheDiagnostics
#endif
        where TAsset : class {
        #region Nested Types
        sealed class Item {
            public TAsset Asset;
            // 이 key 를 잡고 있는 서로 다른 소유자 수. 획득 횟수가 아니다.
            // 같은 소유자의 중복은 ownerTable 의 HashSet 이 막으므로 여기는 수만 든다.
            public int OwnerCount;
        }
        #endregion

        #region Fields
        readonly Dictionary<TKey, Item> assetTable = new();
        readonly Dictionary<AssetOwnerId, HashSet<TKey>> ownerTable = new();
        #endregion

#if UNITY_EDITOR
        #region Fields - Editor Diagnostics
        readonly AssetCacheDiagnosticsHandle diagnosticsHandle;
        #endregion

        #region 생성자
        // 진단 레지스트리 등록 지점.
        public MemoryAssetCache() {
            diagnosticsHandle = AssetCacheDiagnosticsRegistry.Register(this, typeof(TKey), typeof(TAsset));
        }
        #endregion
#endif

        #region Events
        public event Action<TKey, TAsset> OnAssetRemoved;
        #endregion

        #region Public - Get
        public bool TryGet(TKey key, out TAsset asset) {
            asset = null;
            return _TryGetItem(key, out _, out asset);
        }
        #endregion

        #region Public - Save
        public bool Save(TKey key, TAsset asset, AssetOwnerId ownerId) {
            if (!ownerId.IsValid) {
                HLogger.Error(
                    $"[AssetCache] Save rejected. Key '{key}' was given an invalid owner.\n" +
                    $"Every occupancy must be attributable. Pass a valid AssetOwnerId.");
                return false;
            }

            if (ReferenceEquals(asset, null)) return false;

            if (assetTable.TryGetValue(key, out var item)) {
                if (ReferenceEquals(item.Asset, asset)) {
                    _AddOwnerDependency(item, ownerId, key);
                    return true;
                }

                HLogger.Error($"[AssetCache] Save rejected. Key '{key}' already holds a different asset.");
                return false;
            }

            var newItem = new Item { Asset = asset };
            assetTable[key] = newItem;
            _SyncDiagnostics();

            _AddOwnerDependency(newItem, ownerId, key);
            return true;
        }
        #endregion

        #region Public - Release
        public bool Release(TKey key, AssetOwnerId ownerId) {
            if (!ownerId.IsValid) {
                _WarnUnpairedRelease(key, "invalid owner. every release must name its owner");
                return false;
            }

            if (!assetTable.TryGetValue(key, out var item) || ReferenceEquals(item.Asset, null)) {
                _WarnUnpairedRelease(key, $"no cache entry (ownerId={ownerId})");
                return false;
            }

            if (!_UnregisterOwnerKey(ownerId, key)) {
                _WarnUnpairedRelease(key, $"ownerId={ownerId} holds no dependency on this key");
                return false;
            }

            item.OwnerCount--;
            return _TryRemoveItem(key, item);
        }

        public int ReleaseOwner(AssetOwnerId ownerId) {
            if (!ownerId.IsValid) return 0;
            if (!ownerTable.TryGetValue(ownerId, out var keys)) return 0;

            // 떼어낼 점유가 가리키던 Item 을 함께 기억한다. 알림 도중 Clear 뒤 재저장으로 key 가
            // 새 Item 을 가리키면 그 Item 에는 이 owner 의 몫이 없으므로 깎지 않는다.
            var releaseItems = new List<KeyValuePair<TKey, Item>>(keys.Count);
            foreach (var key in keys) {
                if (assetTable.TryGetValue(key, out var item)) releaseItems.Add(new KeyValuePair<TKey, Item>(key, item));
            }
            int releasedCount = 0;

            ownerTable.Remove(ownerId);

            foreach (var pair in releaseItems) {
                if (!assetTable.TryGetValue(pair.Key, out var item) || !ReferenceEquals(item, pair.Value)) continue;
                if (ReferenceEquals(item.Asset, null)) continue;
                // 이 owner 가 잡고 있던 key 를 전부 내려놓는다. 단건 Release 와 같은 의미다.
                item.OwnerCount--;
                releasedCount++;
                _TryRemoveItem(pair.Key, item);
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
            asset = null;
            item = null;

            if (!assetTable.TryGetValue(key, out item) || ReferenceEquals(item.Asset, null)) return false;

            asset = item.Asset;
            return true;
        }

        private bool _TryRemoveItem(TKey key, Item item) {
            if (item.OwnerCount > 0) return false;
            return _RemoveItem(key, item);
        }

        private bool _RemoveItem(TKey key, Item item) {
            if (!assetTable.Remove(key)) return false;

            _SyncDiagnostics();
            _NotifyRemoved(key, item.Asset);
            return true;
        }
        #endregion

        #region Private - Owner
        private void _AddOwnerDependency(Item item, AssetOwnerId ownerId, TKey key) {
            // 이미 잡고 있으면 아무 일도 하지 않는다. 같은 소유자의 재요청은 상태를 바꾸지 않는다.
            if (!_RegisterOwnerKey(ownerId, key)) return;

            item.OwnerCount++;
        }

        /// <summary> 새로 잡았으면 true. 이미 잡고 있던 key 면 false </summary>
        private bool _RegisterOwnerKey(AssetOwnerId ownerId, TKey key) {
            if (!ownerTable.TryGetValue(ownerId, out var keys)) {
                keys = new HashSet<TKey>();
                ownerTable[ownerId] = keys;
            }

            return keys.Add(key);
        }

        /// <summary> 잡고 있던 key 를 놓았으면 true. 잡은 적이 없으면 false </summary>
        private bool _UnregisterOwnerKey(AssetOwnerId ownerId, TKey key) {
            if (!ownerTable.TryGetValue(ownerId, out var keys)) return false;
            if (!keys.Remove(key)) return false;

            if (keys.Count < 1) ownerTable.Remove(ownerId);
            return true;
        }
        #endregion

        #region Private - Clear
        private void _ClearItems() {
            if (assetTable.Count < 1) return;

            // OnAssetRemoved 구독자가 알림 도중 Save 를 다시 호출하면 그 항목은 Clear 를 통과해 살아남는다.
            // 구독자 입장에서는 방금 비운 캐시에 유령이 남는다. 잔존 항목이 없어질 때까지 반복하고, 폭주는 상한으로 끊는다.
            const int MAX_CLEAR_PASSES = 8;
            for (int pass = 0; pass < MAX_CLEAR_PASSES; pass++) {
                if (assetTable.Count < 1) return;

                var removeItems = new List<KeyValuePair<TKey, Item>>(assetTable);
                assetTable.Clear();
                ownerTable.Clear();
                _SyncDiagnostics();

                foreach (var pair in removeItems) {
                    _NotifyRemoved(pair.Key, pair.Value.Asset);
                }
            }

            if (assetTable.Count > 0) {
                HLogger.Error(
                    $"[AssetCache] Clear did not converge: {assetTable.Count} item(s) were re-saved during removal notifications.");
            }
        }
        #endregion

        // Conditional 이라 빌드에서는 호출부까지 컴파일러가 지운다.
        // 호출 지점에 #if 를 흩지 않는다.
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void _SyncDiagnostics() {
#if UNITY_EDITOR
            diagnosticsHandle.EntryCount = assetTable.Count;
#endif
        }

#if UNITY_EDITOR
        #region Public - Editor Diagnostics
        public string CacheLabel => diagnosticsHandle.Label;
        public int EntryCount => assetTable.Count;

        /// <summary> 호출자가 준 버퍼를 비우고 key 기준 점유로 채운다. ownerTable 을 뒤집어 만든다 </summary>
        public void CaptureOccupancy(List<AssetOccupancySnapshot> buffer) {
            if (buffer == null) return;
            buffer.Clear();

            var ownersByKey = new Dictionary<TKey, List<AssetOwnerOccupancy>>(assetTable.Count);
            foreach (var pair in ownerTable) {
                foreach (var key in pair.Value) {
                    if (!ownersByKey.TryGetValue(key, out var owners)) {
                        owners = new List<AssetOwnerOccupancy>();
                        ownersByKey[key] = owners;
                    }
                    owners.Add(new AssetOwnerOccupancy(pair.Key.Value));
                }
            }

            foreach (var pair in assetTable) {
                if (!ownersByKey.TryGetValue(pair.Key, out var owners)) owners = new List<AssetOwnerOccupancy>();

                // TotalCount 는 제거 판정이 쓰는 카운트다. 뒤집은 목록과 수가 다르면 두 인덱스가 어긋난 것이다.
                buffer.Add(new AssetOccupancySnapshot(_FormatKey(pair.Key), pair.Value.OwnerCount, owners));
            }
        }

        /// <summary> 호출자가 준 버퍼를 비우고 소유자 기준 점유로 채운다. ownerTable 을 그대로 옮긴다 </summary>
        public void CaptureHoldings(List<AssetHoldingSnapshot> buffer) {
            if (buffer == null) return;
            buffer.Clear();

            foreach (var pair in ownerTable) {
                var keys = new List<string>(pair.Value.Count);
                foreach (var key in pair.Value) {
                    keys.Add(_FormatKey(key));
                }
                buffer.Add(new AssetHoldingSnapshot(pair.Key.Value, keys));
            }
        }

        /// <summary> 이 소유자의 점유를 강제 해제. 정상 반납과 같은 경로. int 변환은 이 어셈블리 안 </summary>
        public int ForceReleaseOwner(int ownerId) {
            return ReleaseOwner(new AssetOwnerId(ownerId));
        }
        #endregion

        #region Private - Editor Diagnostics
        private string _FormatKey(TKey key) {
            return ReferenceEquals(key, null) ? "(null)" : key.ToString();
        }
        #endregion
#endif

        #region Private - Diagnostics
        // Release 의 false 는 두 가지 뜻이 겹쳐 있다
        // 1. "아직 다른 점유가 남아 살아있다"(정상)
        // 2. "애초에 이 호출자의 점유가 없다"(획득/해제 짝 오류).
        // 반환값만으로는 호출자가 둘을 구분할 수 없으므로, 후자에 한해 경고를 남겨 짝 오류를 관측 가능하게 한다.
        // Error 가 아니라 Warning 인 이유 : ReleaseAll/Clear 이후의 뒤늦은 Release 처럼 회복 가능한 정리 순서 문제도 이 경로로 들어오기 때문.
        private void _WarnUnpairedRelease(TKey key, string reason) {
            HLogger.Warning($"[AssetCache] Unpaired release for key '{key}' - {reason}.");
        }
        #endregion

        #region Private - Event
        // 멀티캐스트 델리게이트는 구독자 하나가 던지면 뒤 구독자를 호출하지 않는다.
        // 이 이벤트는 provider 의 로더 핸들 회수를 트리거하는 정리(clean-up) 신호라,
        // 중간에 끊기면 남은 항목들은 이미 table 에서 제거되서 재시도 대상도 아니고 핸들만 살아남는다.
        // (케이스 리포트 07 EXC-2 - SceneLoader._InvokeSafely 와 동일 처방)
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
 * 2026-09-23 (수정 2) :: ReleaseOwner 가 재생성된 Item 을 깎지 않게
 *
 * 변경 ::
 * ReleaseOwner 가 떼어낼 key 마다 그때의 Item 을 기억하고, 루프에서 key 가 여전히 같은 Item 을
 * 가리킬 때만 OwnerCount 를 내린다.
 *
 * 이유 ::
 * 아래 항목 직후 검수에서 드러났다. 제거 알림 도중 구독자가 Clear 후 같은 key 를 다른 owner 로
 * 다시 Save 하면, 루프가 새 Item 의 수를 깎아 key 를 지우고 살아있는 owner 의 자산에 가짜
 * OnAssetRemoved 를 쐈다 (Provider 라면 그 로더 핸들 반납). 옛 Owners.Remove 검사가 막던 경로다.
 *
 * 결과 ::
 * 재진입 시나리오 16건에서 불변식 위반 0. 재진입 없는 호출 60000회에서 이전 구현과 결과가 같다.
 * Clear 후 같은 owner 가 다시 잡는 경우(옛 구현에서도 깨지던 경로)도 이제 일관된다.
 *
 * 주의 ::
 * ReleaseOwner 도중 같은 owner 가 단건 Release 를 불러도 이미 떼어낸 몫은 그 호출로 놓이지 않는다.
 * owner 의 집합을 먼저 떼기 때문이다. 경우별 결과는 셋이다.
 * - 다시 Save 하지 않은 key : false + "Unpaired release" 경고.
 * - 같은 Item 에 다시 Save 한 key : 다시 잡은 몫만 놓인다. 떼어낸 몫이 남아 있어 false, 경고 없음.
 * - Clear 뒤 새 Item 에 다시 잡은 key : 정상 해제라 true.
 * 앞의 두 경우 최종 상태는 옛 구현과 같지만 바깥 ReleaseOwner 의 반환값은 다르다(옛 1, 지금 2).
 * 세 번째 경우는 반환값도 같다.
 * key 별로 떼면 이 경고는 사라지지만, 알림 도중 같은 owner 의 재 Save 가 true 를 받고도 자산을
 * 잃는 옛 결함이 돌아온다. 떼어낸 몫을 owner 별 pending 스택에 두고 단건 Release 가 거기서 꺼내면
 * 둘 다 해결되는 것을 검증용 사본으로 확인했지만, 필드와 분기가 늘어 적용하지 않았다.
 * ReleaseOwner 루프 도중에는 떼어낸 몫이 지역 목록에 있어 ownerTable 이 잠시 정본이 아니다.
 * 같은 owner 가 알림 중 같은 Item 을 다시 잡으면 OwnerCount 가 잠시 서로 다른 소유자 수보다 커진다.
 * Clear 뒤 새 Item 이면 그렇지 않다. 어느 쪽이든 루프가 끝나면 맞아진다.
 *
 * =========================================================
 * 2026-09-23 (수정) :: Item.Owners 를 OwnerCount 로 축소 + 진단 캡처 두 방향
 *
 * 변경 ::
 * - Item.Owners(HashSet<AssetOwnerId>) 를 OwnerCount(int) 로 바꿨다.
 * - 같은 소유자의 중복 판정을 ownerTable 의 HashSet<TKey> 로 옮겼다.
 *   _RegisterOwnerKey / _UnregisterOwnerKey 가 결과를 bool 로 돌려주고, 그 결과로 수를 올리고 내린다.
 * - CaptureOccupancy 는 ownerTable 을 뒤집어 key 기준으로 채운다. CaptureHoldings 를 새로 두어
 *   소유자 기준은 ownerTable 을 그대로 옮긴다.
 *
 * 이유 ::
 * 두 구조는 같은 관계를 양방향으로 들고 있었다. 빌드에서 key 쪽에 필요한 것은 제거 판정용 수뿐이고,
 * 소유자 목록은 에디터 진단만 읽었다. 목록을 한쪽(ownerTable)에만 두면 빌드는 key 마다 HashSet 을
 * 들지 않고, 모든 연산이 전체 순회 없이 끝난다.
 *
 * 결과 ::
 * 점유의 정본은 ownerTable 하나다. 공개 API 와 해제 의미는 그대로다.
 *
 * 주의 ::
 * 불변식은 "OwnerCount == 그 key 를 담은 ownerTable 집합 수" 이다. 두 값을 따로 고치지 말 것.
 * 진단 스냅샷의 TotalCount(수)와 Owners(뒤집은 목록) 길이가 다르면 이 불변식이 깨진 것이다.
 * ReleaseOwner 는 소유 여부를 다시 묻지 않는다. releaseKeys 가 곧 그 소유자의 점유 집합이다.
 *
 * =========================================================
 * 2026-09-23 (수정) :: table 을 assetTable 로 개명 + TAsset 에 class 제약
 *
 * 변경 ::
 * 메인 테이블 이름을 assetTable 로 바꿨다. TAsset 에 where TAsset : class 를 걸고
 * TryGet / _TryGetItem 의 asset = default 를 asset = null 로 바꿨다.
 *
 * 이유 ::
 * 캐시가 드는 것은 Addressables / Resources 에서 받은 리소스라 전부 참조 타입이다.
 *
 * 주의 ::
 * 제약은 구현체에만 건다. IAssetCache 계약까지 걸면 계약을 쓰는 제네릭 전부로 번진다.
 * 기반 목록의 첫 항목(IAssetCache)은 #if 밖에 둔다. 가드 안이 첫 항목이면 빌드에서 ':' 만 남는다.
 *
 * =========================================================
 * 2026-09-07 (수정) :: CaptureOwners 제거
 * 
 * 변경 ::
 * 어제 넣은 CaptureOwners 구현을 뺐다. ForceReleaseOwner 는 워처가 쓰므로 남는다.
 *
 * 이유 ::
 * 계약에서 빠졌다. 캐시는 소유자 생존을 모르므로 orphan 판정의 출발점이 될 수 없었다.
 *
 * 결과 ::
 * 캐시의 런타임 공개 표면이 09-05 상태로 돌아왔다.
 *
 * 주의 ::
 * ownerTable 은 여전히 ReleaseOwner 의 역인덱스다. 자료구조는 그대로다.
 *
 * =========================================================
 * 2026-09-06 (수정) :: CaptureOwners / ForceReleaseOwner 구현
 * 
 * 변경 ::
 * 소유자 테이블의 키를 버퍼에 담는 CaptureOwners 와,
 * int 신원으로 강제 해제하는 ForceReleaseOwner 를 구현했다.
 *
 * 이유 ::
 * 전자는 런타임 orphan 정리의 입력, 후자는 에디터 워처의 정리 창구다.
 *
 * 결과 ::
 * 두 경로 모두 기존 ReleaseOwner 하나로 모인다. 해제 의미가 갈라지지 않는다.
 *
 * 주의 ::
 * ForceReleaseOwner 는 에디터 진단 region 안이다.
 * int 를 AssetOwnerId 로 바꾸는 일이 이 어셈블리 안에서만 일어나 위조 벽은 유지된다.
 *
 * =========================================================
 * 2026-09-05 (수정) :: 소유자 점유를 횟수에서 유무로
 * 
 * 변경 ::
 * - Item.Owners 를 Dictionary<AssetOwnerId, int> 에서 HashSet<AssetOwnerId> 로 바꿨다.
 * - _AddOwnerDependency 는 이미 잡고 있으면 아무 일도 하지 않는다.
 * - Release(key, ownerId) 는 한 번에 그 소유자의 점유를 없앤다.
 * - AssetOwnerOccupancy.Count 를 제거했다. 항상 1 이 되기 때문이다.
 *
 * 이유 ::
 * 소유는 불린 관계다. 횟수를 세면 소유자가 자기 획득 횟수를 기억해야 하는데, 그 기록을
 * 들 자리가 없는 소유자가 실제로 있었고 두 곳에서 누수가 나고 있었다.
 * - CharacterPortraitController 는 loadedSpriteKey 필드 하나뿐이라 같은 포즈를 두 번
 *   표시하면 카운트만 2 가 되고 해제는 1회뿐이었다.
 * - AudioClipRepository.PrewarmCatalog 는 호출마다 전 엔트리를 획득하는데 ReleaseCatalog
 *   는 레지스트리 refCount 가 0 이 될 때 각 key 를 한 번만 놓았다.
 * 그리고 ReleaseOwner 와 파괴 프로브는 원래부터 횟수를 무시했다. 같은 논리 조작이 API 에
 * 따라 다르게 동작하고 있었고, 이제 세 경로가 일치한다.
 *
 * 주의 ::
 * 한 소유자 안에서 서로 모르는 두 지점이 각각 획득하면 한쪽의 Release 가 둘 다 놓는다.
 * 그 둘이 독립 수명이라면 서로 다른 소유자여야 한다. leash 모델이 그것을 싸게 만든다.
 * =========================================================
 * @Jason - PKH 2026.09.04 에디터 진단 표면 추가
 *
 * # 변경
 * - IAssetCacheDiagnostics 구현. CacheLabel / EntryCount / CaptureOccupancy 를 노출한다.
 * - 생성자에서 AssetCacheDiagnosticsRegistry 에 자가등록한다. 전부 #if UNITY_EDITOR 다.
 * - 파일 내 em dash 를 하이픈으로 교체 (전역 표기 규약).
 *
 * # 이유
 * - Owner Watcher 가 소유자의 수명만 보여주고 점유 내용을 전혀 보여주지 못했다.
 *   데이터는 table 과 ownerTable 에 이미 양방향으로 다 있는데 꺼낼 경로가 없었다.
 *
 * # 주의
 * - 빌드 공개 표면은 늘지 않는다. 진단 축 전체가 에디터 가드 안에 있다.
 * - CaptureOccupancy 는 key 마다 소유자 리스트를 새로 만든다. 창이 자동 리페인트되므로
 *   초당 수 회 할당이 생기지만, 에디터 전용이고 항목 수가 적어 풀링은 두지 않았다.
 *
 * =========================================================
 * 2026-08-06 (수정 2) :: OnAssetRemoved 구독자 예외 격리 (케이스 리포트 07 EXC-2)
 * 
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
 * 
 * 변경 ::
 * 1) TryLoad(key) / TryLoad(key, ownerId) 구현 삭제 - IAssetReader 계약에서 함께 제거.
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
 * 
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
 * 
 * AssetHandler 의 reference counting 산실. Item.Owners (소유자 미지정 호출의 카운터인
 * AnonymousDependency 와 함께) + ownerTable 역인덱스 두 자료구조로 owner-aware + 익명 호출
 * 두 경로를 한 자료구조에서 통합 추적. 실제 제거 조건은 두 카운터가 모두 비었을 때.
 *
 * ReleaseOwner(ownerId) 가 ownerTable 역인덱스를 활용해 owner 가 잡은 모든 key 를 한 번에
 * 회수 - 시간복잡도가 점유 수에 정비례. cache 제거 시 OnAssetRemoved 이벤트로 provider
 * 의 source release 연쇄가 이어짐 (Cache 와 Loader 의 결합도 0).
 *
 * Save 동일 key 에 다른 asset 시도는 Reject + LogError - silent overwrite 방지.
 * Save 동일 key 에 같은 asset 재시도는 점유 카운터만 증가 (idempotent 하지 않음 - 의도적).
 * =========================================================
 */
#endif
