using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using HResource.Data;
using HResource.Provider;
using HDiagnosis.Logger;
using UnityEngine;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 소유자의 지문(AssetOwnerId) 발급과 수명 신호를 전담한다. provider 의 상주 객체다.
 *
 * 주요 기능 ::
 * Fingerprint(Component) - 지문 발급 + 파괴 프로브 부착. 같은 소유자는 항상 같은 지문.
 * Leash(object, Component anchor) - 순수 C# 소유자용 창구. 수명 상한을 anchor 가 준다.
 * Reclaim / ReclaimEntry - 그 소유자의 점유를 일괄 회수하고 지문을 폐기한다.
 * ReclaimDeadOwners() - 소유자가 죽은 항목을 약한 표 대조로 찾아 회수한다. 수동 호출이다.
 *
 * 사용법 ::
 * 직접 만들지 않는다. AssetProvider 생성자가 하나를 들고, IAssetSource 공개 API 가 위임한다.
 *
 * 주의 ::
 * 지문 테이블은 ConditionalWeakTable 이다. 일반 Dictionary 로 바꾸면 provider 가 자기가
 * 서비스한 모든 소유자를 영원히 살려두어, 소유권 누수를 고치려던 물건이 더 큰 누수가 된다.
 *
 * 캡처 경계 ::
 * 프로브 핸들러와 AssetLeash 는 소유자 객체를 절대 참조하지 않는다. 둘 다 LeashEntry 만 든다.
 * 하나라도 owner 를 캡처하면 앵커(다른 GameObject)가 순수 객체의 수명을 늘리는 역전이 생긴다.
 * 그래서 LeashEntry 에는 owner 로 가는 필드가 없다.
 *
 * 역할 경계 ::
 * - AssetProvider     : 자산을 어떻게 얻고 캐싱하고 검증하는가.
 * - AssetLeashManager : 누가 무엇을 들고 있고 언제 놓는가.
 *
 * 자동 통지의 사각 :: Destroy(component)
 * 프로브는 GameObject 파괴만 본다. Destroy(gameObject) 는 잡지만, Destroy(component) 로
 * 소유자 컴포넌트만 지우면 GameObject 와 프로브가 살아있어 통지가 오지 않는다.
 * 매 프레임 소유자를 순회해 잡는 비용은 이 사각의 발생 빈도에 비해 크다. 그래서 자동으로
 * 걷지 않고 ReclaimDeadOwners 를 부를 때만 약한 표를 훑어 걷어낸다.
 * 부르지 않으면 그 점유는 provider 폐기까지 남고 Owner Watcher 의 ORPHAN 행으로 드러난다.
 * 소유자 컴포넌트만 떼어낼 일이 있으면 그 전에 Release / ReleaseOwner 를 직접 부를 것.
 * =========================================================
 */
#endif

namespace HResource.Subscription {
    internal sealed class AssetLeashManager<TKey, TAsset> : System.IDisposable {
        #region Nested
        // owner 로 가는 필드를 두지 않는다. 캡처 경계 참조.
        sealed class LeashEntry {
            internal AssetOwnerId Id;
            internal OwnerLeashProbe Probe;
            internal System.Action Handler;
            internal AssetLeash Leash;
            internal bool Reclaimed;
            // 프로브 부착이 실패한 적이 있는가. 재부착 시 조용히 앵커가 바뀌는 것을 알린다.
            internal bool AnchorAttachFailed;
        }

        sealed class AssetLeash : IAssetLeash<TKey, TAsset> {
            readonly AssetLeashManager<TKey, TAsset> manager;
            readonly LeashEntry entry;
            // 발급 시점의 신원. entry 가 회수 후 되살아나면 Id 가 바뀌므로 이 값과 어긋난다.
            readonly AssetOwnerId issuedId;
            bool disposed;

            internal AssetLeash(AssetLeashManager<TKey, TAsset> manager, LeashEntry entry) {
                this.manager = manager;
                this.entry = entry;
                this.issuedId = entry.Id;
            }

            public UniTask<TAsset> GetAsync(
                TKey key,
                AssetLoadMode loadMode,
                AssetFetchMode fetchMode = AssetFetchMode.CacheFirst) {

                if (_RejectIfClosed(nameof(GetAsync))) return UniTask.FromResult<TAsset>(default);
                return manager.provider.GetForOwnerAsync(key, loadMode, fetchMode, entry.Id);
            }

            public bool TryGet(TKey key, out TAsset asset) {
                asset = default;
                if (_RejectIfClosed(nameof(TryGet))) return false;
                return manager.provider.TryGet(key, out asset);
            }

            public bool Release(TKey key) {
                if (_RejectIfClosed(nameof(Release))) return false;
                return manager.provider.ReleaseForOwner(key, entry.Id);
            }

            public void Dispose() {
                if (disposed) return;
                disposed = true;

                // 이 창구가 발급된 뒤 entry 가 되살아났다면 지금 신원은 남의 것이다.
                // 그대로 회수하면 새 소유자의 점유를 대신 내려놓는 교차 해제가 된다.
                if (entry.Id != issuedId) return;
                manager._ReclaimEntry(entry);
            }

            // 두 갈래로 닫힌다. 스스로 Dispose 했거나, 앵커가 파괴되어 회수당했거나.
            bool _RejectIfClosed(string apiName) {
                // 순서 주의 : provider 폐기를 먼저 본다. Dispose 가 잔여 항목에 Reclaimed 를
                // 세우므로, 아래 분기를 먼저 태우면 "앵커가 파괴됐다" 는 틀린 원인을 보고한다.
                if (manager.disposed) {
                    HLogger.Error(
                        $"[AssetLeash] {apiName} rejected. The provider that issued this leash was disposed." +
                        " The anchor is not the problem - check the lifetime of whoever created the provider.");
                    return true;
                }
                if (disposed) {
                    HLogger.Error(
                        $"[AssetLeash] {apiName} rejected. This leash was already disposed." +
                        " Get a new one with source.Leash(owner, anchor).");
                    return true;
                }
                // 순서 주의 : 낡은 핸들 판정을 앵커 판정보다 먼저 한다. 되살아난 신원이 다시
                // 회수된 경우 두 조건이 함께 참인데, 그때 호출자에게 필요한 정보는 "이 핸들이
                // 낡았다" 이지 "앵커가 죽었다" 가 아니다.
                if (entry.Id != issuedId) {
                    HLogger.Error(
                        $"[AssetLeash] {apiName} rejected. This leash belongs to a previous identity of its owner." +
                        " The owner was re-leashed after a reclaim, so this handle is stale. Use the new leash.");
                    return true;
                }
                if (entry.Reclaimed) {
                    HLogger.Error(
                        $"[AssetLeash] {apiName} rejected. The anchor was destroyed and this leash was reclaimed." +
                        " The owner outlived its anchor - give it an anchor that lives at least as long.");
                    return true;
                }
                return false;
            }
        }
        #endregion

        #region Fields
        readonly AssetProvider<TKey, TAsset> provider;
        // 키를 약하게 잡는다. 이 테이블이 소유자 수명을 붙잡으면 안 된다.
        readonly ConditionalWeakTable<object, LeashEntry> table = new();
        // 폐기 시 정리할 대상. LeashEntry 는 owner 를 참조하지 않으므로 강하게 들어도 안전.
        readonly List<LeashEntry> liveEntries = new();
        bool disposed;
        #endregion

        #region Public - Constructors
        public AssetLeashManager(AssetProvider<TKey, TAsset> provider) {
            if (provider == null) HLogger.Throw(new System.ArgumentNullException(nameof(provider)));
            this.provider = provider;
        }
        #endregion

        #region Internal - Fingerprint
        /// <summary> Component 소유자의 지문을 확보한다. 자기 GameObject 가 곧 앵커다. </summary>
        internal AssetOwnerId Fingerprint(Component owner) {
            if (disposed) {
                HLogger.Warning("[AssetLeash] Fingerprint called after the provider was disposed.");
                return AssetOwnerId.None;
            }
            // Unity 의 == 오버로드가 파괴된 컴포넌트도 걸러낸다.
            if (owner == null) {
                HLogger.Error(
                    "[AssetLeash] The owner is null or already destroyed, so this request cannot be attributed." +
                    " Pass a live Component as the owner.");
                return AssetOwnerId.None;
            }

            LeashEntry entry = _EnsureEntry(owner);
            if (entry.Probe == null) _AttachProbe(owner, entry);

            // 상한을 걸 수 없으면 획득 자체를 성립시키지 않는다. 무효 신원을 돌려주면
            // GetAsync 진입부가 로드 전에 빠지므로 로더 핸들도 잡히지 않는다.
            if (entry.Probe == null) {
                _ReclaimEntry(entry);
                return AssetOwnerId.None;
            }
            return entry.Id;
        }

        /// <summary>
        /// 이 지문이 아직 살아있는지. 회수된 항목은 liveEntries 에서 빠지므로
        /// "목록에 없다" 가 곧 "회수됐거나 신원이 갱신됐다" 이다.
        /// 로드 완료 시점에 소유자 생존을 판정하는 데 쓴다.
        ///
        /// 선형 탐색이고 로드 성공마다 돈다. Dictionary 를 병행하면 O(1) 이 되지만 동기화할
        /// 자료구조가 하나 늘어난다. 살아있는 소유자 수는 수십 규모라 그 교환이 남는 장사가
        /// 아니다. 소유자가 수천 단위로 늘어나면 그때 바꾼다.
        /// </summary>
        internal bool IsLive(AssetOwnerId ownerId) {
            if (disposed || !ownerId.IsValid) return false;

            for (int k = 0; k < liveEntries.Count; k++) {
                LeashEntry entry = liveEntries[k];
                if (entry == null || entry.Reclaimed) continue;
                if (entry.Id.Value == ownerId.Value) return true;
            }
            return false;
        }

        /// <summary> 이미 지문이 있는 소유자만 조회한다. 없으면 발급하지 않는다. </summary>
        internal bool TryFingerprint(Component owner, out AssetOwnerId ownerId) {
            ownerId = AssetOwnerId.None;
            if (disposed || owner == null) return false;
            if (!table.TryGetValue(owner, out LeashEntry entry)) return false;
            if (entry.Reclaimed) return false;

            ownerId = entry.Id;
            return true;
        }
        #endregion

        #region Internal - Leash
        /// <summary>
        /// 순수 C# 소유자용 창구를 발급한다. anchor 가 수명 상한이다.
        /// 창구를 Dispose 하지 않고 버려도 anchor 가 파괴되면 그 점유는 회수된다.
        /// </summary>
        internal IAssetLeash<TKey, TAsset> Leash(object owner, Component anchor) {
            if (owner == null) {
                HLogger.Throw(new System.ArgumentNullException(
                    nameof(owner), "[AssetLeash] Leash(owner, anchor) requires a non-null owner."));
            }

            // owner 가 object 라 위의 == 는 참조 비교.
            // 파괴된 Unity 객체는 그것을 통과해 약한 키가 되어 버리므로 여기서 따로 걸러낸다.
            if (owner is UnityEngine.Object destroyedOwner && destroyedOwner == null) {
                HLogger.Throw(new System.ArgumentException(
                    "[AssetLeash] The owner is a destroyed UnityEngine.Object, so it cannot be attributed.",
                    nameof(owner)));
            }
            if (anchor == null) {
                HLogger.Throw(new System.ArgumentNullException(
                    nameof(anchor),
                    "[AssetLeash] Leash(owner, anchor) requires a live Component as the anchor." +
                    " Without one this occupancy would have no upper bound."));
            }
            if (disposed) {
                HLogger.Warning("[AssetLeash] Leash called after the provider was disposed.");
                return null;
            }

            LeashEntry entry = _EnsureEntry(owner);
            entry.Leash ??= new AssetLeash(this, entry);

            if (entry.Probe == null) {
                if (entry.AnchorAttachFailed) {
                    HLogger.Warning(
                        $"[AssetLeash] Re-anchoring this owner to '{anchor.gameObject.name}' after an earlier" +
                        " probe attach failed. Its lifetime bound changes to this anchor.");
                }
                _AttachProbe(anchor, entry);
            }
            else if (entry.Probe != null && entry.Probe.gameObject != anchor.gameObject) {
                // 한 소유자는 하나의 수명 상한만 갖는다. 조용히 버리면 호출자는 자기가 준
                // 앵커가 상한이라고 오해한 채로 남는다.
                HLogger.Warning(
                    $"[AssetLeash] This owner is already anchored to '{entry.Probe.gameObject.name}'. " +
                    $"The anchor '{anchor.gameObject.name}' is ignored - one owner keeps one lifetime bound.");
            }

            // Fingerprint 와 같은 규칙이다. 상한 없는 창구는 발급하지 않는다.
            if (entry.Probe == null) {
                _ReclaimEntry(entry);
                return null;
            }
            return entry.Leash;
        }
        #endregion

        #region Internal - Reclaim
        /// <summary> 이 소유자의 점유를 일괄 회수한다. 회수한 key 수를 돌려준다. </summary>
        internal int Reclaim(object owner) {
            if (disposed || owner == null) return 0;
            if (!table.TryGetValue(owner, out LeashEntry entry)) return 0;
            return _ReclaimEntry(entry);
        }

        /// <summary>
        /// 소유자를 잃은 점유의 수동 회수. 반환값은 회수한 key 수
        /// 프로브가 GameObject 파괴만 보므로 Destroy(component) 로 죽은 소유자의 유일한 출구
        /// </summary>
        internal int ReclaimDeadOwners() {
            if (disposed) return 0;

            // 약한 표에 남은 것이 살아있는 소유자. 파괴된 Unity 객체는 == 오버로드로 제외
            var aliveEntries = new HashSet<LeashEntry>();
            foreach (KeyValuePair<object, LeashEntry> pair in table) {
                if (pair.Key is UnityEngine.Object unityOwner && unityOwner == null) continue;
                aliveEntries.Add(pair.Value);
            }

            // 대상 선수집. _ReclaimEntry 가 liveEntries 를 줄이므로 순회 중 회수 금지
            var deadEntries = new List<LeashEntry>();
            for (int k = 0; k < liveEntries.Count; k++) {
                LeashEntry entry = liveEntries[k];
                if (entry == null || entry.Reclaimed) continue;
                if (aliveEntries.Contains(entry)) continue;

                deadEntries.Add(entry);
            }

            int reclaimedCount = 0;
            for (int k = 0; k < deadEntries.Count; k++) {
                reclaimedCount += _ReclaimEntry(deadEntries[k]);
            }
            return reclaimedCount;
        }

        /// <summary> 회수의 단일 창구. 프로브 / 창구 Dispose / ReleaseOwner / 죽은 소유자 정리 네 경로 </summary>
        int _ReclaimEntry(LeashEntry entry) {
            if (disposed || entry == null || entry.Reclaimed) return 0;

            // 순서 주의 : 표시를 먼저 세운다. 회수 중 재진입해도 두 번 회수되지 않는다.
            entry.Reclaimed = true;
            _DetachProbe(entry);
            liveEntries.Remove(entry);

            int released = provider.ReleaseOwnerId(entry.Id);
            AssetOwnerIdGenerator.NotifyReleased(entry.Id);
            return released;
        }
        #endregion

        #region Public - Dispose
        public void Dispose() {
            if (disposed) return;
            // 아래 NotifyReleased 는 public static 이벤트를 쏘므로 외부 구독자가 재진입할 수 있다.
            // 이 플래그가 _ReclaimEntry 를 막아 순회 도중 liveEntries 가 변형되는 것을 방지.
            disposed = true;

            for (int k = 0; k < liveEntries.Count; k++) {
                LeashEntry entry = liveEntries[k];
                if (entry == null || entry.Reclaimed) continue;
                entry.Reclaimed = true;
                _DetachProbe(entry);
                AssetOwnerIdGenerator.NotifyReleased(entry.Id);
            }
            liveEntries.Clear();
        }
        #endregion

        #region Private
        LeashEntry _EnsureEntry(object owner) {
            if (table.TryGetValue(owner, out LeashEntry entry)) {
                // 회수된 뒤 같은 소유자가 다시 요청하면 새 지문 제공.
                // 폐기된 id 를 재사용하면 이미 끝난 수명 위에 점유가 증가.
                if (!entry.Reclaimed) return entry;

                entry.Id = AssetOwnerIdGenerator.NewId(owner);
                entry.Leash = null;
                entry.Reclaimed = false;
                entry.AnchorAttachFailed = false;
                liveEntries.Add(entry);
                return entry;
            }

            entry = new LeashEntry { Id = AssetOwnerIdGenerator.NewId(owner) };
            table.Add(owner, entry);
            liveEntries.Add(entry);
            return entry;
        }

        void _AttachProbe(Component anchor, LeashEntry entry) {
            GameObject go = anchor.gameObject;
            OwnerLeashProbe probe = go.GetComponent<OwnerLeashProbe>();
            if (probe == null) probe = go.AddComponent<OwnerLeashProbe>();

            // 파괴가 진행 중인 GameObject 에 AddComponent 는 예외가 아니라 null 반환.
            if (probe == null) {
                HLogger.Error(
                    $"[AssetLeash] Could not attach a destroy probe to '{go.name}' because it is being destroyed. " +
                    "The request is refused instead of creating an occupancy with no upper bound. " +
                    "Acquire assets before teardown.");
                entry.AnchorAttachFailed = true;
                return;
            }

            // entry 만 캡처. owner 를 캡처하면 앵커의 컴포넌트가 소유자를 살려두어,
            // 수명 상한을 주려던 앵커가 오히려 수명을 늘리는 역전.
            entry.Probe = probe;
            entry.AnchorAttachFailed = false;
            entry.Handler = () => _ReclaimEntry(entry);
            probe.OnGameObjectDestroyed += entry.Handler;
        }

        void _DetachProbe(LeashEntry entry) {
            if (entry.Probe == null || entry.Handler == null) return;

            entry.Probe.OnGameObjectDestroyed -= entry.Handler;
            entry.Probe = null;
            entry.Handler = null;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-07 (수정) :: ReclaimOrphan 을 ReclaimDeadOwners 로 대체
 * 
 * 변경 ::
 * 신원 기반 ReclaimOrphan 을 걷어내고, 약한 표를 열거해 소유자 생존으로 판정하는
 * ReclaimDeadOwners 를 넣었다.
 * 헤더의 사각 절과 _ReclaimEntry 요약을 새 경로에 맞춰 고쳤다.
 *
 * 이유 ::
 * 프로브는 GameObject 파괴에만 발화한다. Destroy(component) 단독으로 죽은 소유자는
 * liveEntries 에 남아 IsLive 가 true 를 냈고, 그것이 orphan 의 대표 경로였다.
 *
 * 결과 ::
 * 회수가 _ReclaimEntry 단일 창구를 탄다. 프로브 해제·중복 방지·통지가 평소와 같아졌다.
 * Dispose 없이 GC 된 순수 소유자도 약한 표에서 사라지므로 함께 걸린다.
 *
 * 주의 ::
 * 약한 표는 회수된 항목도 계속 담는다. liveEntries 에 없는 항목은 대상이 아니다.
 *
 * =========================================================
 * 2026-09-06 (수정) :: ReclaimOrphan 추가
 * 
 * 변경 ::
 * entry 가 남아있지 않은 신원의 점유를 회수하는 ReclaimOrphan 을 넣었다.
 *
 * 이유 ::
 * _ReclaimEntry 는 entry 를 전제한다. 창구가 이미 사라진 신원은 그 경로를 탈 수 없다.
 *
 * 결과 ::
 * 반납과 통지의 짝이 _ReclaimEntry 와 같은 순서로 유지된다.
 * AssetProvider 는 여전히 AssetOwnerIdGenerator 를 참조하지 않는다.
 *
 * 주의 ::
 * 생존 판정을 하지 않는다. 호출자가 IsLive 로 걸러 부른다.
 *
 * =========================================================
 * 2026-09-05 (수정) :: 상한 없는 획득을 성립시키지 않는다
 * 
 * 변경 ::
 * - IsLive(ownerId) 신설. 로드 완료 시점에 소유자 생존을 판정한다.
 * - 프로브 부착 실패 시 Fingerprint 는 AssetOwnerId.None, Leash 는 null 을 돌려준다.
 *   entry 를 즉시 회수 표시해 이후 어떤 경로로도 점유가 남지 않게 한다.
 *
 * 이유 ::
 * - RACE-1 : await 사이에 소유자가 죽으면 프로브가 이미 회수를 마친 뒤라, 뒤늦게 등록되는
 *   점유는 아무도 내려놓을 수 없는 ORPHAN 이 됐다. 종전 아키텍처에서는 provider 가 소유자
 *   생존을 알 수 없어 "구조적으로 불가" 로 판정했으나, leash 계층이 그것을 알게 되어 닫혔다.
 * - 부착 실패 : 종전에는 경고만 남기고 획득을 진행해 상한 없는 점유를 만들었다. 거부 시점을
 *   로드 전으로 당겨 로더 핸들조차 잡히지 않게 했다.
 *
 * 주의 ::
 * Destroy(component) 단독은 여전히 잡지 못한다. 헤더의 "유일한 사각" 절 참조.
 * =========================================================
 * 2026-09-04 (수정) :: 독립 리뷰 지적 반영
 * 
 * 변경 ::
 * - _AttachProbe 가 AddComponent 결과를 null 검사한다. 파괴 중인 GameObject 에 대해
 *   AddComponent 는 예외가 아니라 null 을 돌려주므로 다음 줄이 NRE 가 되고 있었다.
 * - AssetLeash 가 발급 시점 신원(issuedId)을 기억한다. entry 가 회수 후 되살아나면
 *   옛 창구가 Reclaimed=false 를 보고 다시 살아나 새 소유자의 점유를 해제했다.
 * - Dispose 가 잔여 항목의 신원 통지와 프로브 구독 해제를 수행한다. liveEntries 신설.
 * - Leash 가 파괴된 UnityEngine.Object 를 owner 로 받는 것을 막는다.
 * - 두 번째 anchor 가 무시될 때 경고를 남긴다.
 * - 클래스를 internal 로 낮췄다. 공개 멤버가 하나도 쓰이지 않는 표면이었다.
 *
 * 이유 ::
 * Dispose 건이 특히 나빴다. 신원 통지를 빼먹으면 워처에 행이 남고 그것이 묘비를 거쳐
 * 가짜 ORPHAN 이 된다. 진짜 누수를 찾는 도구가 깨끗한 종료마다 거짓 경보를 만든다.
 * 프로브 구독도 남아 살아있는 GameObject 가 폐기된 provider 그래프를 붙잡았다.
 *
 * 주의 ::
 * (무효) AddComponent 실패 시 획득 자체는 막지 않는다. 상한이 없어졌다는 사실만 Error 로 알린다.
 * -> 2026-09-05 항목이 이 판단을 뒤집었다. 거부 시점을 로드 이전으로 당기면 로더 핸들이
 *    잡히지 않으므로, 우려했던 "핸들은 잡혔는데 저장이 거부되는" 상태가 생기지 않는다.
 * =========================================================
 * 2026-09-04 (수정) :: 순수 C# 소유자에 앵커를 강제
 * 
 * 변경 ::
 * - Leash(object) -> Leash(object owner, Component anchor). anchor 는 필수다.
 * - AssetLeash 가 owner 참조를 버리고 LeashEntry 만 든다.
 * - LeashEntry.Reclaimed 신설. 앵커가 죽은 뒤의 leash 사용을 거부한다.
 * - 회수 경로 3갈래(프로브 / Dispose / ReleaseOwner)를 _ReclaimEntry 하나로 모았다.
 * - _EnsureEntry 가 회수된 항목을 새 지문으로 되살린다.
 *
 * 이유 ::
 * 앵커 없는 순수 소유자는 창구를 그냥 버려도 아무도 회수하지 못했다. provider 폐기까지는
 * 상한이 있었으나, 그 상황이 "표현 가능하다" 는 것 자체가 문제였다.
 * anchor 를 필수 인자로 만들면 상한 없는 점유를 코드로 적을 수 없게 된다.
 *
 * 결과 ::
 * 창구를 버려도 anchor 파괴 시 회수된다. GC Probe 는 예방 수단이 아니라
 * "anchor 는 살아있는데 창구만 일찍 버려져 필요 이상으로 오래 잡고 있는" 연성 상태를 보는
 * 진단 도구로 역할이 좁아진다.
 *
 * 주의 ::
 * 프로브 핸들러와 AssetLeash 는 owner 를 절대 캡처하면 안 된다. 하나라도 캡처하면
 * 앵커가 순수 객체를 살려두어 ConditionalWeakTable 의 약한 키가 무의미해진다.
 * =========================================================
 */
#endif
