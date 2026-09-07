#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HResource.Subscription;

namespace HResource.Editor.Subscription {
    [InitializeOnLoad]
    public static class AssetOwnerIdWatchRegistry {
        #region Public - Types
        [Serializable]
        public sealed class Entry {
            public int OwnerId;
            public UnityEngine.Object UnityOwner;
            public string ClassName;
            public string ContainerName;
            public string OwnerDisplayName;
            public string SourceTypeName;
            public string CreatedAt;
            public bool IsUnityObject;
            public bool IsAlive;

            // 비 Unity 소유자의 생사를 관측하는 유일한 수단.
            // 순수 C# 객체에는 파괴 이벤트가 없어 이 약한 참조 말고는 죽음을 알 방법이 없다.
            // 강한 참조로 바꾸면 이 창이 소유자를 살려두어, 누수를 관측하려다 누수를 만든다.
            [NonSerialized]
            public WeakReference PlainOwnerRef;
        }
        #endregion

        #region Fields
        static readonly Dictionary<int, Entry> table = new();
        static readonly List<int> removeBuffer = new();
        // 소유자가 죽어 행을 지울 때 마지막 정체를 남긴다. ORPHAN 행이 id 만 보여주면
        // "무엇이 샜는지" 를 알 수 없어 진단이 되지 않는다.
        static readonly Dictionary<int, string> tombstones = new();

        const string PLAIN_OWNER_CONTAINER = "(Non-Unity Owner)";
        #endregion

        #region Properties
        public static IReadOnlyDictionary<int, Entry> Table => table;
        public static IReadOnlyDictionary<int, string> Tombstones => tombstones;
        #endregion

        #region Constructors
        // 에디트 모드 구독. 도메인 로드마다 1회.
        static AssetOwnerIdWatchRegistry() {
            _Subscribe();
            EditorApplication.playModeStateChanged += _OnPlayModeStateChanged;
            EditorApplication.update += _EditorUpdate;
        }
        #endregion

        #region Subscription
        // AssetOwnerIdGenerator._ResetStatics 는 SubsystemRegistration 에서 OnIdCreated/OnIdReleased 를
        // null 로 비운다. 그 훅은 "런타임 구독자가 플레이 세션을 넘어 잔존하는 것" 을 막기 위한 것인데,
        // [InitializeOnLoad] 로 붙는 이 워처는 스스로 재구독할 방법이 없어 함께 끊겨 있었다.
        //
        // RuntimeInitializeLoadType 은 SubsystemRegistration → AfterAssembliesLoaded → BeforeSplashScreen
        // → BeforeSceneLoad → AfterSceneLoad 순서가 보장되므로, AfterAssembliesLoaded 에서 재구독하면
        // 리셋 이후임이 결정적이다. (Domain Reload 비활성 상태에서도 이 콜백은 진입할 때마다 실행된다.)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void _ResubscribeAfterRuntimeReset() {
            _Subscribe();
        }

        // 도메인 리로드 여부에 따라 두 진입점이 겹칠 수 있으므로 항상 해제 후 구독한다.
        private static void _Subscribe() {
            AssetOwnerIdGenerator.OnIdCreated -= _OnIdCreated;
            AssetOwnerIdGenerator.OnIdCreated += _OnIdCreated;

            AssetOwnerIdGenerator.OnIdReleased -= _OnIdReleased;
            AssetOwnerIdGenerator.OnIdReleased += _OnIdReleased;
        }
        #endregion

        #region Public - Register
        public static void Register(int ownerId, object owner) => _OnIdCreated(ownerId, owner);
        public static void Unregister(int ownerId) => _OnIdReleased(ownerId);
        public static void Clear() {
            table.Clear();
            tombstones.Clear();
        }

        /// <summary> 점유가 없어진 id 의 묘비를 버린다. 창이 ORPHAN 을 집계할 때 부른다. </summary>
        public static void ForgetTombstone(int ownerId) => tombstones.Remove(ownerId);

        /// <summary>
        /// GC 를 한 번 돌린 뒤 죽은 소유자를 즉시 정리한다.
        /// 약한 참조는 수집이 일어나야 죽었다고 답하므로, 강제하지 않으면 판정이 늦는다.
        /// 매 프레임 부르면 안 되는 비용이라 창의 명시적 버튼에서만 호출한다.
        /// </summary>
        public static void CollectAndPrune() {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            _EditorUpdate();
        }
        #endregion

        #region Private - Register
        // 0 이하는 무효 신원이다. AssetOwnerId.IsValid 와 같은 규칙을 값으로 적용한다.
        private static void _OnIdCreated(int ownerId, object owner) {
            if (ownerId <= 0) return;
            Entry entry = _BuildEntry(ownerId, owner);
            table[ownerId] = entry;
        }

        private static void _OnIdReleased(int ownerId) {
            if (ownerId <= 0) return;

            // 정상 회수는 흔적을 남기지 않는다. 묘비는 "죽었는데 점유가 남았다" 를 위한 것이다.
            table.Remove(ownerId);
            tombstones.Remove(ownerId);
        }

        #endregion

        #region Private - Build
        static Entry _BuildEntry(int ownerId, object owner) {
            Entry entry = new Entry {
                OwnerId = ownerId,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
            };

            if (owner == null) {
                entry.ClassName = "(null)";
                entry.ContainerName = "(null)";
                entry.OwnerDisplayName = "(null)";
                entry.SourceTypeName = "(null)";
                entry.IsUnityObject = false;
                entry.IsAlive = false;
                return entry;
            }

            Type ownerType = owner.GetType();
            entry.ClassName = ownerType.Name;
            entry.SourceTypeName = ownerType.FullName ?? ownerType.Name;
            entry.OwnerDisplayName = owner.ToString();

            if (owner is UnityEngine.Object unityObject) {
                entry.UnityOwner = unityObject;
                entry.IsUnityObject = true;
                entry.IsAlive = unityObject != null;

                switch (unityObject) {
                case Component component:
                    entry.ContainerName = component.gameObject ? component.gameObject.name : "(Missing GameObject)";
                    entry.OwnerDisplayName = component.name;
                    break;

                case GameObject gameObject:
                    entry.ContainerName = gameObject.name;
                    entry.OwnerDisplayName = gameObject.name;
                    break;

                default:
                    entry.ContainerName = unityObject.name;
                    entry.OwnerDisplayName = unityObject.name;
                    break;
                }
            }
            else {
                entry.IsUnityObject = false;
                entry.IsAlive = true;
                // 약한 참조만 잡는다. 이후 생사 판정의 유일한 근거이며 수명은 붙잡지 않는다.
                entry.PlainOwnerRef = new WeakReference(owner);
                entry.ContainerName = PLAIN_OWNER_CONTAINER;
            }

            return entry;
        }
        #endregion

        #region Private - Update
        static void _EditorUpdate() {
            if (table.Count < 1) return;

            removeBuffer.Clear();

            foreach (KeyValuePair<int, Entry> pair in table) {
                Entry entry = pair.Value;
                if (entry == null) continue;

                // Unity 객체는 fake-null 로, 순수 객체는 약한 참조로 판정한다.
                // 순수 객체 쪽은 GC 가 돌아야 답이 바뀌므로 CollectAndPrune 이 그것을 강제한다.
                bool isAlive = entry.IsUnityObject
                    ? entry.UnityOwner != null
                    : entry.PlainOwnerRef != null && entry.PlainOwnerRef.IsAlive;

                entry.IsAlive = isAlive;
                if (isAlive) continue;

                // 지우기 전에 정체를 남긴다. 점유가 남아 있으면 창이 ORPHAN 행에 이 이름을 붙인다.
                // 남기지 않으면 ORPHAN 은 id 와 개수만 보여주어 무엇이 샜는지 알 수 없다.
                tombstones[pair.Key] = _DescribeTombstone(entry);
                removeBuffer.Add(pair.Key);
            }

            if (removeBuffer.Count < 1) return;

            for (int k = 0; k < removeBuffer.Count; k++) {
                table.Remove(removeBuffer[k]);
            }
        }

        static string _DescribeTombstone(Entry entry) {
            string className = string.IsNullOrWhiteSpace(entry.ClassName) ? "(unknown)" : entry.ClassName;

            // 순수 객체의 컨테이너는 자리표시자라 붙여도 정보가 늘지 않는다.
            if (string.IsNullOrWhiteSpace(entry.ContainerName)) return className;
            if (entry.ContainerName == PLAIN_OWNER_CONTAINER) return className + "  (plain C# owner)";
            return className + "  in " + entry.ContainerName;
        }

        static void _OnPlayModeStateChanged(PlayModeStateChange state) {
            // 2차 경로. 에디터 어셈블리에서 RuntimeInitializeOnLoadMethod 가 호출되지 않는 환경이면
            // AfterAssembliesLoaded 재구독이 통째로 누락되므로, 여기서 한 번 더 건다.
            // 이 시점은 Awake 이후일 수 있어 초기 발급을 놓칠 수 있다 - 1차 경로의 대체가 아니라 보완이다.
            if (state == PlayModeStateChange.EnteredPlayMode) {
                _Subscribe();
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode ||
                state == PlayModeStateChange.ExitingPlayMode) {
                Clear();
            }
        }
        #endregion
    }
}

/* =========================================================
 * Dev Log
 * =========================================================
 *
 * =========================================================
 * 2026-09-04 (수정) :: 순수 C# 소유자의 죽음을 관측 가능하게
 * =========================================================
 * 변경 ::
 * - Entry 에 WeakReference PlainOwnerRef 추가. 비 Unity 소유자만 채운다.
 * - _EditorUpdate 가 두 축을 함께 판정한다. 종전에는 !IsUnityObject 를 continue 로 건너뛰었다.
 * - 행을 지울 때 tombstone 에 마지막 정체를 남긴다. 정상 회수(NotifyReleased)는 남기지 않는다.
 * - CollectAndPrune 추가. GC 를 강제한 뒤 즉시 판정한다. 창의 GC Probe 버튼이 부른다.
 *
 * 이유 ::
 * 순수 객체는 IsAlive 가 true 로 하드코딩되고 정리 루프에서도 제외되어, Dispose 없이
 * 버려진 소유자가 건강한 소유자와 화면상 완전히 동일하게 보였다. 실제로는 캐시에 점유가
 * 남아 ClearCache 외에는 내릴 수단이 없는 영구 누수 상태다.
 *
 * 결과 ::
 * 버려진 순수 소유자가 Unity 소유자와 같은 경로로 ORPHAN 행에 올라간다.
 * 묘비 덕분에 ORPHAN 행이 id 와 개수만이 아니라 무엇이 샜는지까지 보여준다.
 *
 * 주의 ::
 * 약한 참조는 GC 가 돌아야 죽었다고 답한다. 자동 갱신만으로는 판정이 늦으므로 확인이
 * 필요하면 GC Probe 를 눌러야 한다. 강한 참조로 바꾸면 이 창이 소유자를 살려두어
 * 누수를 관측하려다 누수를 만든다.
 * =========================================================
 */
#endif