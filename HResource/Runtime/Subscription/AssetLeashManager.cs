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
 * Fingerprint(Component) - 파괴 프로브를 붙이고 OwnerLiveToken 을 발급한다.
 * OwnerLiveToken - 발급 시점의 항목과 신원을 담는다. 신원 조회와 O(1) 생존 판정을 겸한다.
 * Leash(object, Component anchor) - 순수 C# 소유자용 창구. 수명 상한을 anchor 가 준다.
 * Reclaim / ReclaimEntry - 그 소유자의 점유를 일괄 회수하고 지문을 폐기한다.
 * ReclaimDeadOwners() - 약한 표를 훑어 파괴된 Unity 소유자의 항목을 회수한다. 수동 호출이다.
 *
 * 사용법 ::
 * 직접 만들지 않는다. AssetProvider 생성자가 하나를 들고, IAssetSource 공개 API 가 위임한다.
 *
 * 주의 ::
 * 지문 테이블은 ConditionalWeakTable 이다. 일반 Dictionary 로 바꾸면 provider 가 자기가
 * 서비스한 모든 소유자를 영원히 살려두어, 소유권 누수를 고치려던 물건이 더 큰 누수가 된다.
 *
 * 캡처 경계 ::
 * 프로브 핸들러와 CSharpAssetLeash 는 소유자 객체를 절대 참조하지 않는다. 둘 다 LeashEntry 만 든다.
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
        // internal 인 이유는 Dev Log 2026-09-08 항목 참조.
        internal sealed class LeashEntry {
            internal AssetOwnerId Id;
            internal OwnerLeashProbe Probe;
            internal System.Action Handler;
            internal CSharpAssetLeash Leash;
            internal bool Reclaimed;
            // 프로브 부착이 실패한 적이 있는가. 재부착 시 조용히 앵커가 바뀌는 것을 알린다.
            internal bool AnchorAttachFailed;
        }

        /// <summary>
        /// 발급 시점의 항목과 신원을 함께 담은 생존 판정 토큰. 목록 탐색 없이 O(1) 로 답한다
        /// entry 가 회수됐거나 부활해 신원이 바뀌었으면 죽은 것으로 본다
        /// </summary>
        internal readonly struct OwnerLiveToken {
            readonly LeashEntry entry;
            readonly AssetOwnerId issuedId;

            /// <summary> 발급 시점의 신원. 호출부가 별도 인자로 들고 다니지 않게 한다 </summary>
            internal AssetOwnerId IssuedId => issuedId;

            // 발급은 이 어셈블리 안에서만 일어난다. 접근성 연쇄는 Dev Log 참조.
            private OwnerLiveToken(LeashEntry entry) {
                this.entry = entry;
                this.issuedId = entry != null ? entry.Id : AssetOwnerId.None;
            }

            internal static OwnerLiveToken Issue(LeashEntry entry) => new OwnerLiveToken(entry);

            internal bool IsLive =>
                entry != null && !entry.Reclaimed && entry.Id == issuedId && issuedId.IsValid;
        }

        // LeashEntry.Leash 필드가 이 타입을 담으므로 접근성을 맞춘다(CS0052).
        internal sealed class CSharpAssetLeash : ICSharpAssetLeash<TKey, TAsset> {
            readonly AssetLeashManager<TKey, TAsset> manager;
            readonly LeashEntry entry;
            // 발급 시점의 신원. entry 가 회수 후 되살아나면 Id 가 바뀌므로 이 값과 어긋난다.
            readonly AssetOwnerId issuedId;
            bool disposed;

            internal CSharpAssetLeash(AssetLeashManager<TKey, TAsset> manager, LeashEntry entry) {
                this.manager = manager;
                this.entry = entry;
                this.issuedId = entry.Id;
            }

            public UniTask<TAsset> GetAsync(
                TKey key,
                AssetLoadMode loadMode,
                AssetFetchMode fetchMode = AssetFetchMode.CacheFirst) {

                if (_RejectIfClosed(nameof(GetAsync))) return UniTask.FromResult<TAsset>(default);
                return manager.provider.GetForOwnerAsync(key, loadMode, fetchMode, OwnerLiveToken.Issue(entry));
            }

            public bool TryGet(TKey key, out TAsset asset) {
                asset = default;
                if (_RejectIfClosed(nameof(TryGet))) return false;
                return manager.provider.TryGet(key, out asset);
            }

            public bool Release(TKey key) {
                if (_RejectIfClosed(nameof(Release))) return false;
                return manager.provider.ReleaseForOwner(key, issuedId);
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
        readonly ConditionalWeakTable<object, LeashEntry> leashTable = new();
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
        internal OwnerLiveToken Fingerprint(Component owner) {
            if (disposed) {
                HLogger.Warning("[AssetLeash] Fingerprint called after the provider was disposed.");
                return default;
            }
            // Unity 의 == 오버로드가 파괴된 컴포넌트도 걸러낸다.
            if (owner == null) {
                HLogger.Error(
                    "[AssetLeash] The owner is null or already destroyed, so this request cannot be attributed." +
                    " Pass a live Component as the owner.");
                return default;
            }

            LeashEntry entry = _EnsureEntry(owner);
            if (entry.Probe == null) _AttachProbe(owner, entry);

            // 상한을 걸 수 없으면 획득 자체를 성립시키지 않는다. 무효 신원을 돌려주면
            // GetAsync 진입부가 로드 전에 빠지므로 로더 핸들도 잡히지 않는다.
            if (entry.Probe == null) {
                _ReclaimEntry(entry);
                return default;
            }

            return OwnerLiveToken.Issue(entry);
        }

        /// <summary> 이미 지문이 있는 소유자만 조회한다. 없으면 발급하지 않는다. </summary>
        internal bool TryFingerprint(Component owner, out AssetOwnerId ownerId) {
            ownerId = AssetOwnerId.None;
            if (disposed || owner == null) return false;
            if (!leashTable.TryGetValue(owner, out LeashEntry entry)) return false;
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
        internal ICSharpAssetLeash<TKey, TAsset> Leash(object owner, Component anchor) {
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
            entry.Leash ??= new CSharpAssetLeash(this, entry);

            if (entry.Probe == null) {
                if (entry.AnchorAttachFailed) {
                    HLogger.Warning(
                        $"[AssetLeash] Re-anchoring this owner to '{anchor.gameObject.name}' after an earlier" +
                        " probe attach failed. Its lifetime bound changes to this anchor.");
                }
                _AttachProbe(anchor, entry);
            }
            else if (entry.Probe.gameObject != anchor.gameObject) {
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
            if (!leashTable.TryGetValue(owner, out LeashEntry entry)) return 0;
            return _ReclaimEntry(entry);
        }

        /// <summary>
        /// 소유자를 잃은 점유의 수동 회수. 반환값은 회수한 key 수
        /// 프로브가 GameObject 파괴만 보므로 Destroy(component) 로 죽은 소유자의 유일한 출구
        /// GC 된 순수 C# 소유자는 약한 표에서 쌍이 사라져 여기 걸리지 않는다. 앵커가 상한이다
        /// </summary>
        internal int ReclaimDeadOwners() {
            if (disposed) return 0;

            // 대상 선수집. _ReclaimEntry 의 NotifyReleased 가 외부 구독자를 깨우고,
            // 그 구독자가 Fingerprint 를 부르면 leashTable.Add 가 일어난다. 순회 중 회수 금지.
            var deadEntries = new List<LeashEntry>();
            foreach (KeyValuePair<object, LeashEntry> pair in leashTable) {
                // 파괴된 Unity 소유자만 대상. 관리 래퍼는 살아 있어 쌍이 표에 남는다.
                if (!(pair.Key is UnityEngine.Object unityOwner) || unityOwner != null) continue;

                LeashEntry entry = pair.Value;
                if (entry == null || entry.Reclaimed) continue;

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

            int released = provider.ReleaseOwnerId(entry.Id);
            AssetOwnerIdGenerator.NotifyReleased(entry.Id);
            return released;
        }
        #endregion

        #region Public - Dispose
        public void Dispose() {
            if (disposed) return;
            // 아래 NotifyReleased 는 public static 이벤트를 쏘므로 외부 구독자가 재진입할 수 있다.
            // 이 플래그를 먼저 세우면 재진입 경로인 Fingerprint 와 Leash 가 모두 거부되어
            // 순회 도중 leashTable 에 항목이 추가되지 않는다. _ReclaimEntry 도 함께 막힌다.
            disposed = true;

            // GC 된 소유자의 항목은 약한 표에서 이미 사라져 여기 걸리지 않는다.
            // 그 몫의 프로브는 앵커 파괴 시 핸들러가 돌지만 disposed 를 보고 즉시 반환한다.
            foreach (KeyValuePair<object, LeashEntry> pair in leashTable) {
                LeashEntry entry = pair.Value;
                if (entry == null || entry.Reclaimed) continue;

                entry.Reclaimed = true;
                _DetachProbe(entry);
                AssetOwnerIdGenerator.NotifyReleased(entry.Id);
            }
        }
        #endregion

        #region Private
        LeashEntry _EnsureEntry(object owner) {
            if (leashTable.TryGetValue(owner, out LeashEntry entry)) {
                // 회수된 뒤 같은 소유자가 다시 요청하면 새 지문 제공.
                // 폐기된 id 를 재사용하면 이미 끝난 수명 위에 점유가 증가.
                if (!entry.Reclaimed) return entry;

                entry.Id = AssetOwnerIdGenerator.NewId(owner);
                entry.Leash = null;
                entry.Reclaimed = false;
                entry.AnchorAttachFailed = false;
                return entry;
            }

            entry = new LeashEntry { Id = AssetOwnerIdGenerator.NewId(owner) };
            leashTable.Add(owner, entry);
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
 * 2026-09-08 (수정 6) :: AssetLeash 를 CSharpAssetLeash 로
 *
 * 변경 ::
 * IAssetLeash -> ICSharpAssetLeash, 중첩 구현 AssetLeash -> CSharpAssetLeash.
 * 파일과 .meta 도 함께 옮겼다(GUID 보존). 참조 14 파일을 갱신했다.
 *
 * 이유 ::
 * 이 타입은 순수 C# 소유자 전용인데 이름만 봐서는 Component 소유자도 쓰는 것으로
 * 읽힌다. 반납 의무가 이쪽에만 붙으므로 적용 범위가 이름에서 드러나야 한다.
 * 사람과 에이전트 모두 파일명 단계에서 걸러낼 수 있게 하려는 것이다.
 *
 * 결과 ::
 * 타입 이름에 CSharp 이 있으면 순수 C# 소유자 전용이라는 규칙이 선다.
 *
 * 주의 ::
 * IAssetSource 는 개명 대상이 아니다. GetAsync(Component) / Release(Component) /
 * ReleaseOwner(Component) 를 직접 들고 있어 Component 경로가 본체다. Leash 하나만
 * 순수 C# 용이므로 여기에 CSharp 을 붙이면 나머지 멤버를 잘못 표기하게 된다.
 * 로그 태그 [AssetLeash] 는 그대로 둔다. Fingerprint 등 Component 경로도 이 태그로
 * 남기므로 계층 이름이지 타입 이름이 아니다.
 *
 * =========================================================
 * 2026-09-08 (수정 5) :: liveEntries 제거. 판정을 약한 표 하나로
 *
 * 변경 ::
 * 강한 목록 liveEntries 를 지웠다. 필드 / _EnsureEntry 의 Add 2 곳 /
 * _ReclaimEntry 의 Remove / Dispose 의 순회가 함께 사라졌다.
 * ReclaimDeadOwners 와 Dispose 가 leashTable 을 직접 훑는다. HashSet 할당도 없앴다.
 *
 * 이유 ::
 * 소유자 사망 경로 다섯 중 넷은 이미 이벤트이거나 약한 표만으로 판정된다.
 * 목록이 유일하게 일하던 칸은 "GC 된 순수 C# 소유자를 앵커 파괴 전에 코드로 회수"
 * 하나였고, 그 경로의 호출부는 테스트 샘플 하나다. ICSharpAssetLeash 헤더가 약속하는
 * 보증도 "using 은 즉시 반납, anchor 는 최후 보증" 둘뿐이라 목록은 계약에 없는
 * 세 번째 보증이었다. 대가로 소유자 전원이 항목 하나씩을 잔류시키고,
 * _ReclaimEntry 의 List.Remove 가 O(n) 이라 씬 정리가 O(N^2) 였다.
 *
 * 결과 ::
 * 잔류 상태가 약한 표 하나로 줄었다. 소유자가 죽으면 표가 스스로 쌍을 버린다.
 * 워처의 ORPHAN 표시와 Orphan Clean 버튼, 앵커 파괴 시 자동 회수는 그대로다.
 * 셋 다 이 목록을 보지 않고 캐시 점유와 프로브 이벤트로 동작하기 때문이다.
 *
 * 주의 ::
 * GC 된 순수 C# 소유자는 이제 ReclaimOrphans() 로 잡히지 않는다. 앵커가 죽어야
 * 회수된다. 점유 누수는 아니다. provider Dispose 는 assetCache.ReleaseAll 이 비운다.
 * 되살릴 근거는 이 항목이 아니라 그때의 필요다. 순수 C# 소유자가 실제로 쓰이고
 * 앵커 수명이 너무 길어 조기 회수가 필요해진 사례가 먼저 있어야 한다. 그때는
 * 목록을 되살리지 말고 MemoryAssetCache.ownerTable 과의 뺄셈을 먼저 검토한다.
 * 이미 있는 역인덱스라 새 잔류 상태를 만들지 않는다.
 * 제거 결정은 2026-09-08 사용자 승인으로 진행했다.
 *
 * =========================================================
 * 2026-09-08 (수정 4) :: 지문 표 이름을 leashTable 로
 *
 * 변경 ::
 * 필드 table 을 leashTable 로 바꿨다. 파일 안 6 곳.
 *
 * 이유 ::
 * MemoryAssetCache 에도 table 이 있다. 그쪽은 key 로 에셋을 찾는 강한 Dictionary 고
 * 이쪽은 소유자로 항목을 찾는 약한 CWT 다. 고아 판정을 논할 때 두 표의 뺄셈을
 * 이야기하게 되는데 이름이 같으면 어느 쪽 표인지가 문장에서 사라진다.
 *
 * 결과 ::
 * 소속을 붙이지 않아도 이름만으로 어느 계층의 표인지 드러난다.
 *
 * 주의 ::
 * private 필드라 외부 영향 없음. 헤더의 "지문 테이블" 서술은 그대로 둔다.
 *
 * =========================================================
 * 2026-09-08 (수정 3) :: CSharpAssetLeash.Release 의 신원 표기를 통일
 *
 * 변경 ::
 * ReleaseForOwner 에 넘기던 entry.Id 를 issuedId 로 바꿨다.
 *
 * 이유 ::
 * 같은 클래스의 Dispose 와 _RejectIfClosed 는 issuedId 로 판정하는데 Release 만
 * entry.Id 를 썼다. 바로 위 _RejectIfClosed 가 둘의 동일성을 통과시킨 뒤라 값은
 * 반드시 같지만, 읽는 쪽이 "여기만 다른 값일 수 있나" 를 확인하게 만든다.
 *
 * 결과 ::
 * 이 클래스가 신원을 꺼내는 지점이 issuedId 하나로 모인다.
 *
 * 주의 ::
 * 동작 변화 없음. 가드를 걷어내면 둘이 갈라지므로 _RejectIfClosed 선행이 전제다.
 *
 * =========================================================
 * 2026-09-08 (수정 2) :: Fingerprint 가 토큰만 돌려준다
 *
 * 변경 ::
 * 반환형을 AssetOwnerId 에서 OwnerLiveToken 으로 바꾸고 out 파라미터를 없앴다.
 * 실패 출구 셋은 default 를 돌려준다. IsValid 와 IsLive 가 함께 false 다.
 * OwnerLiveToken.issuedId 를 int 에서 AssetOwnerId 로 바꿨다.
 *
 * 이유 ::
 * 두 값을 함께 돌려주면 호출부가 둘을 따로 들고 다닐 수 있다. 어긋난 조합을
 * 없애려던 직전 변경이 정작 발급 지점에서는 그 조합을 여전히 표현 가능하게 뒀다.
 * int 로 들면 "0 초과가 유효" 라는 규칙이 AssetOwnerId 밖에 복제된다.
 *
 * 결과 ::
 * 신원이 토큰 안에만 존재한다. 형제 타입 CSharpAssetLeash 도 AssetOwnerId issuedId 를
 * 들고 있어 두 타입의 관용구가 같아졌다.
 *
 * 주의 ::
 * IsValid 검사로 걸러내던 자리가 IsLive 검사로 바뀌었다. IsLive 는 발급 이후의
 * 회수까지 보므로 판정이 더 좁다. 발급 직후에는 둘의 결과가 같다.
 *
 * =========================================================
 * 2026-09-08 (수정) :: IsLive 선형 탐색을 OwnerLiveToken 으로 대체
 *
 * 변경 ::
 * OwnerLiveToken 중첩 구조체 신설. Fingerprint 가 out 으로 함께 돌려준다.
 * 호출처 0 건이 된 IsLive(AssetOwnerId) 를 제거했다.
 * LeashEntry 와 CSharpAssetLeash 를 private 에서 internal 로 올렸다.
 *
 * 이유 ::
 * IsLive 는 로드 성공마다 liveEntries 를 선형 탐색했다. 그런데 진입부의 Fingerprint 가
 * 이미 그 항목을 손에 쥐고 있었다. 쥔 것을 버리고 정수로 다시 찾는 구조였다.
 * 색인을 더하면 수명 내내 남지만, 토큰은 호출과 함께 사라진다.
 *
 * 결과 ::
 * 판정이 필드 비교 두 번으로 끝난다. 자료구조가 늘지 않았다.
 * 부활 감지가 명시적이 됐다. 옛 판정은 "목록에 없다" 로 우연히 맞았다.
 *
 * 주의 ::
 * 접근성 연쇄는 컴파일러가 강제한다. OwnerLiveToken 이 LeashEntry 를 필드로 담아 CS0051,
 * LeashEntry 가 CSharpAssetLeash 를 필드로 담아 CS0052 가 이어진다. 바깥 클래스가 internal 이라
 * 어셈블리 밖으로는 새지 않는다. 좁히려 하지 말 것.
 * Destroy(component) 사각은 그대로다. 토큰도 그 경우 살아있다고 답한다.
 *
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
 * - CSharpAssetLeash 가 발급 시점 신원(issuedId)을 기억한다. entry 가 회수 후 되살아나면
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
 * - CSharpAssetLeash 가 owner 참조를 버리고 LeashEntry 만 든다.
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
 * 프로브 핸들러와 CSharpAssetLeash 는 owner 를 절대 캡처하면 안 된다. 하나라도 캡처하면
 * 앵커가 순수 객체를 살려두어 ConditionalWeakTable 의 약한 키가 무의미해진다.
 * =========================================================
 */
#endif
