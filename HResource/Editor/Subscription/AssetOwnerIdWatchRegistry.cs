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
        // 소유자가 죽어 행을 지울 때 마지막 정체를 남긴다. ORPHAN 행이 id 만 보여주면
        // "무엇이 샜는지" 를 알 수 없어 진단이 되지 않는다.
        static readonly Dictionary<int, string> tombstones = new();

        const string PLAIN_OWNER_CONTAINER = "(Non-Unity Owner)";

        // 전수 스캔 간격(초). 창이 0.25 초마다 그리므로 표시 지연은 최대 1 초다.
        // 진단 표시의 지연을 감수하고 에디터 프레임 부하를 줄이는 쪽을 택했다.
        const double SCAN_INTERVAL = 1d;

        static double nextScanTime;
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

        #region Public - Control
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
            ScanNow();
        }

        /// <summary>
        /// 주기 게이트를 건너뛰고 지금 판정한다. 창을 열었을 때와 GC Probe 가 부른다.
        /// _EditorUpdate 를 부르면 스로틀에 걸려 조용히 아무 일도 하지 않는다.
        /// </summary>
        public static void ScanNow() {
            nextScanTime = EditorApplication.timeSinceStartup + SCAN_INTERVAL;
            _ScanOnce();
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
        /// <summary>
        /// 주기 스캔의 게이트. 창이 없으면 판정을 볼 곳이 없고, 간격 안이면 결과가 버려진다.
        /// 즉시 정리가 필요한 경로는 이 게이트를 지나지 않고 ScanNow 를 부른다.
        /// </summary>
        static void _EditorUpdate() {
            if (!EditorWindow.HasOpenInstances<AssetOwnerIdWatcherWindow>()) return;

            if (EditorApplication.timeSinceStartup < nextScanTime) return;
            nextScanTime = EditorApplication.timeSinceStartup + SCAN_INTERVAL;

            _ScanOnce();
        }

        /// <summary> 전수 판정 1회. 게이트를 거치지 않으므로 호출자가 빈도를 책임진다 </summary>
        static void _ScanOnce() {
            if (table.Count < 1) return;

            // 1 초에 한 번, 창이 열려 있을 때만 도는 경로다. 필드로 재사용할 만큼 잦지 않다.
            List<int> removeBuffer = null;

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
                (removeBuffer ??= new List<int>()).Add(pair.Key);
            }

            if (removeBuffer == null) return;

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
 * 2026-09-08 (수정 3) :: 수동 Register / Unregister 제거
 *
 * 변경 ::
 * public static Register / Unregister 두 줄을 지웠다. 둘은 _OnIdCreated /
 * _OnIdReleased 를 그대로 부르는 별칭이었다.
 * 남은 멤버에 맞춰 region 이름을 Public - Register 에서 Public - Control 로 바꿨다.
 *
 * 이유 ::
 * 표는 _Subscribe 가 AssetOwnerIdGenerator 의 이벤트에 걸어 두어 자동으로 채워진다.
 * 손으로 부르는 경로는 호출부가 0 건이면서, 부르면 생성기가 발급한 적 없는 id 를
 * 진단 표에 심는다. 생성기의 사실을 비추는 창에 거짓 행을 만드는 구멍이었다.
 *
 * 결과 ::
 * 표에 행을 넣는 경로가 생성기 이벤트 하나로 좁아진다.
 *
 * 주의 ::
 * 제거 결정은 2026-09-08 사용자 승인으로 진행했다. 되살릴 일이 생기면 이 항목이
 * 근거가 아니라 그때의 필요가 근거다. 자동 경로로 못 채우는 사례가 먼저 있어야 한다.
 *
 * =========================================================
 * 2026-09-08 (수정 2) :: removeBuffer 를 지역으로 내림
 *
 * 변경 ::
 * static 필드였던 removeBuffer 를 _ScanOnce 지역 변수로 옮기고 지연 할당했다.
 * table.Count 조기 반환은 _EditorUpdate 에서 _ScanOnce 로 옮겼다. 게이트를 건너뛰는
 * ScanNow 경로도 같은 검사를 받게 하려는 것이다.
 *
 * 이유 ::
 * 한 메서드 안에서만 쓰고 버리는 데이터가 도메인 리로드까지 전역에 남아 있었다.
 * 스캔이 1 초에 한 번, 창이 열렸을 때만 도는 경로가 되면서 재사용 이득도 사라졌다.
 *
 * 결과 ::
 * 제거 대상이 없는 스캔은 할당을 하지 않는다. null 여부가 곧 "제거할 것 없음" 이라
 * removeBuffer.Count 검사는 null 검사로 바뀌었다.
 *
 * 주의 ::
 * 순회 중 table 을 건드리지 않는다는 제약은 그대로다. 버퍼가 지역이 됐다고
 * 즉시 제거로 바꾸면 InvalidOperationException 이 난다.
 *
 * =========================================================
 * 2026-09-08 (수정) :: 전수 스캔에 게이트와 주기를 도입
 *
 * 변경 ::
 * _EditorUpdate 를 게이트로 두고 판정 본문을 _ScanOnce 로 분리했다.
 * 창이 열려 있을 때만, 1 초 간격으로 스캔한다.
 * ScanNow 신설. 게이트를 건너뛰고 즉시 판정한다.
 *
 * 이유 ::
 * 판정이 매 에디터 프레임 전수 순회였다. 창은 0.25 초마다 그리므로 대부분 버려졌다.
 * 창 상태는 EditorWindow.HasOpenInstances 로 직접 묻는다. 카운터를 두면 그 짝을
 * 도메인 리로드와 플레이 모드 전환에서 맞추는 부담이 생긴다.
 *
 * 결과 ::
 * 창을 닫아 두면 스캔이 돌지 않는다. 이벤트 구독은 그대로라 표는 계속 쌓이고,
 * 창을 열면 ScanNow 가 그 사이의 변화를 한 번에 수렴시킨다.
 *
 * 주의 ::
 * CollectAndPrune 은 _EditorUpdate 가 아니라 ScanNow 를 부른다. 게이트를 타면
 * GC Probe 버튼이 스로틀에 걸려 조용히 아무 일도 하지 않는다.
 * 창이 닫힌 동안에는 죽은 Unity 래퍼가 표에 남는다. Entry.UnityOwner 가 강한 참조라서다.
 * 탭이 가려진 도킹 창은 OnDisable 이 오지 않아 스캔이 계속 돈다.
 *
 * =========================================================
 * 2026-09-04 (수정) :: 순수 C# 소유자의 죽음을 관측 가능하게
 *
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