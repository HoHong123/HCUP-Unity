#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetOwnerId 의 수명과 캐시 점유를 함께 보는 진단 창입니다.
 *
 * 특징 / 지원기능 ::
 * 탭 2개로 같은 자료를 두 방향에서 봅니다.
 * + Owner Tracker : 소유자별로 무엇을 몇 개 잡고 있는지. 수명 정보와 점유를 한 줄에 둔다
 * + Resource Ownership : 리소스별로 누가 잡고 있는지. 모든 점유는 소유자를 갖는다
 * + Orphan Clean : 목록에 잡힌 orphan 의 점유를 툴바 버튼으로 강제 해제한다
 *
 * 주의사항 ::
 * 두 축은 AssetLeashManager 가 묶습니다. 소유자를 회수하면 지문 폐기와 점유 해제가
 * 함께 일어나므로, 정상 경로에서는 워처 행과 점유가 어긋나지 않습니다.
 * 어긋나는 경우가 ORPHAN 입니다. 소유자는 죽었는데 프로브가 발화하지 못한 상태이며,
 * Destroy(component) 단독이 그 유일한 경로입니다.
 * 점유 자료는 AssetCacheDiagnosticsRegistry 에 등록된 캐시에서만 옵니다.
 * 플레이 중이 아니면 등록된 캐시가 없어 비어 있는 것이 정상입니다.
 *
 * 사용 ::
 * 메뉴 HCUP / Resource / Owner Watcher
 * =========================================================
 */

using System;
using System.Collections.Generic;
using System.Linq;
using HDiagnosis.Logger;
using HResource.Cache;
using UnityEditor;
using UnityEngine;

namespace HResource.Editor.Subscription {
    public sealed class AssetOwnerIdWatcherWindow : EditorWindow {
        #region Types
        enum WatcherTab {
            OwnerTracker = 0,
            ResourceOwnership = 1,
        }

        sealed class CacheView {
            public string Label;
            public readonly List<AssetOccupancySnapshot> Snapshots = new();
        }
        #endregion

        #region 상수
        const double REPAINT_INTERVAL = 0.25d;

        const float WIDTH_OWNER_ID = 70f;
        const float WIDTH_OWNER_NAME = 200f;
        const float WIDTH_CLASS = 160f;
        const float WIDTH_CONTAINER = 180f;
        const float WIDTH_ALIVE = 44f;
        const float WIDTH_HOLDS = 60f;
        const float WIDTH_TOTAL = 70f;
        const float INDENT = 24f;
        #endregion

        #region Fields
        readonly List<IAssetCacheDiagnostics> caches = new();
        readonly List<CacheView> cacheViews = new();

        /// <summary> ownerId -> 그 소유자가 잡고 있는 항목 표시 문자열 </summary>
        readonly Dictionary<int, List<string>> ownerKeyTable = new();

        /// <summary> ownerId -> 그 소유자가 잡고 있는 key 수 </summary>
        readonly Dictionary<int, int> ownerTotalTable = new();
        readonly List<int> staleTombstoneBuffer = new();

        readonly HashSet<int> expandedOwners = new();
        readonly HashSet<string> expandedKeys = new();
        readonly List<int> orphanOwnerIds = new();

        WatcherTab tab = WatcherTab.OwnerTracker;
        Vector2 ownerScrollPosition;
        Vector2 resourceScrollPosition;
        string searchText = string.Empty;
        bool showOnlyUnityObjects;
        bool showOnlyAlive = true;
        double nextRepaintTime;
        #endregion

        #region Menu
        [MenuItem("HCUP/Resource/Owner Watcher")]
        public static void Open() {
            AssetOwnerIdWatcherWindow window = GetWindow<AssetOwnerIdWatcherWindow>();
            window.titleContent = new GUIContent("OwnerId Watcher");
            window.Show();
        }
        #endregion

        #region 유니티 라이프사이클 함수
        private void OnEnable() {
            EditorApplication.update += _OnEditorUpdate;
        }

        private void OnDisable() {
            EditorApplication.update -= _OnEditorUpdate;
        }

        private void OnGUI() {
            _RefreshOccupancy();
            _DrawToolbar();

            switch (tab) {
            case WatcherTab.OwnerTracker:
                _DrawOwnerTracker();
                break;

            case WatcherTab.ResourceOwnership:
                _DrawResourceOwnership();
                break;
            }
        }
        #endregion

        #region Private - 갱신
        // 창이 열려 있는 동안만 주기적으로 다시 그린다. 종전에는 자동 갱신이 없어
        // Refresh 를 누르기 전까지 값이 멈춰 있었고, 반영이 안 되는 것으로 오해를 샀다.
        private void _OnEditorUpdate() {
            if (EditorApplication.timeSinceStartup < nextRepaintTime) return;

            nextRepaintTime = EditorApplication.timeSinceStartup + REPAINT_INTERVAL;
            Repaint();
        }

        private void _RefreshOccupancy() {
            AssetCacheDiagnosticsRegistry.Collect(caches);

            while (cacheViews.Count < caches.Count) cacheViews.Add(new CacheView());
            while (cacheViews.Count > caches.Count) cacheViews.RemoveAt(cacheViews.Count - 1);

            ownerKeyTable.Clear();
            ownerTotalTable.Clear();

            for (int k = 0; k < caches.Count; k++) {
                CacheView view = cacheViews[k];
                view.Label = caches[k].CacheLabel;
                caches[k].CaptureOccupancy(view.Snapshots);

                _IndexByOwner(view.Snapshots);
            }

            _CollectOrphanOwners();
        }

        private void _IndexByOwner(List<AssetOccupancySnapshot> snapshots) {
            for (int s = 0; s < snapshots.Count; s++) {
                AssetOccupancySnapshot snapshot = snapshots[s];
                IReadOnlyList<AssetOwnerOccupancy> owners = snapshot.Owners;

                for (int o = 0; o < owners.Count; o++) {
                    AssetOwnerOccupancy occupancy = owners[o];

                    if (!ownerKeyTable.TryGetValue(occupancy.OwnerId, out List<string> keys)) {
                        keys = new List<string>();
                        ownerKeyTable[occupancy.OwnerId] = keys;
                    }
                    keys.Add(snapshot.Key);

                    // 점유는 유무라 소유자 총계는 곧 잡고 있는 key 수다.
                    ownerTotalTable.TryGetValue(occupancy.OwnerId, out int total);
                    ownerTotalTable[occupancy.OwnerId] = total + 1;
                }
            }
        }

        // 점유는 있는데 소유자 기록이 없는 id. 회수 창구가 사라진 누수다.
        private void _CollectOrphanOwners() {
            orphanOwnerIds.Clear();
            IReadOnlyDictionary<int, AssetOwnerIdWatchRegistry.Entry> table = AssetOwnerIdWatchRegistry.Table;

            foreach (KeyValuePair<int, int> pair in ownerTotalTable) {
                if (table.ContainsKey(pair.Key)) continue;
                orphanOwnerIds.Add(pair.Key);
            }
            orphanOwnerIds.Sort();

            _ForgetStaleTombstones();
        }

        /// <summary> 점유가 사라진 id 의 묘비는 보여줄 곳이 없다. 표가 무한정 자라지 않게 버린다. </summary>
        private void _ForgetStaleTombstones() {
            IReadOnlyDictionary<int, string> tombstones = AssetOwnerIdWatchRegistry.Tombstones;
            if (tombstones.Count < 1) return;

            staleTombstoneBuffer.Clear();
            foreach (KeyValuePair<int, string> pair in tombstones) {
                if (ownerTotalTable.ContainsKey(pair.Key)) continue;
                staleTombstoneBuffer.Add(pair.Key);
            }

            for (int k = 0; k < staleTombstoneBuffer.Count; k++) {
                AssetOwnerIdWatchRegistry.ForgetTombstone(staleTombstoneBuffer[k]);
            }
        }
        #endregion

        #region Private - Toolbar
        private void _DrawToolbar() {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {
                tab = (WatcherTab)GUILayout.Toolbar(
                    (int)tab,
                    new[] { "Owner Tracker", "Resource Ownership" },
                    EditorStyles.toolbarButton,
                    GUILayout.Width(280f));

                GUILayout.Space(8f);
                GUILayout.Label("Search", GUILayout.Width(50f));
                searchText = GUILayout.TextField(searchText, EditorStyles.toolbarTextField, GUILayout.ExpandWidth(true));

                if (tab == WatcherTab.OwnerTracker) {
                    showOnlyUnityObjects = GUILayout.Toggle(showOnlyUnityObjects, "Unity Only", EditorStyles.toolbarButton, GUILayout.Width(90f));
                    showOnlyAlive = GUILayout.Toggle(showOnlyAlive, "Alive Only", EditorStyles.toolbarButton, GUILayout.Width(90f));
                }

                // 순수 C# 소유자의 죽음은 GC 가 돌아야 관측된다. 이 버튼이 그것을 강제한다.
                if (GUILayout.Button("GC Probe", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                    AssetOwnerIdWatchRegistry.CollectAndPrune();

                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                    AssetOwnerIdWatchRegistry.Clear();

                // 목록이 비면 대상 없는 확인창이 뜨므로 비활성
                using (new EditorGUI.DisabledScope(orphanOwnerIds.Count < 1)) {
                    if (GUILayout.Button("Orphan Clean", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                        _CleanOrphans();
                }
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
                GUILayout.Label($"caches {caches.Count}", GUILayout.Width(90f));
                GUILayout.Label($"owners with occupancy {ownerTotalTable.Count}", GUILayout.Width(200f));
                GUILayout.Label($"orphan {orphanOwnerIds.Count}", GUILayout.Width(90f));
                GUILayout.Label(caches.Count > 0 ? string.Empty : "no live cache registered. enter play mode.");
            }
        }
        #endregion

        #region Private - Orphan Clean
        /// <summary>
        /// 목록의 orphan 점유를 강제 해제. 되돌릴 수 없음. 대상 선별은 이 창의 책임
        /// 해제 대상은 점유뿐. 남은 leash 엔트리는 앵커 파괴나 ReclaimOrphans 가 회수
        /// </summary>
        private void _CleanOrphans() {
            if (orphanOwnerIds.Count < 1) return;

            bool isConfirmed = EditorUtility.DisplayDialog(
                "Orphan Clean",
                $"Force release the occupancy held by {orphanOwnerIds.Count} orphan owner(s)?\n" +
                "Their owners are gone, so nothing else will ever release it.\n" +
                "This cannot be undone.",
                "Clean", "Cancel");
            if (!isConfirmed) return;

            int ownerCount = orphanOwnerIds.Count;
            int releasedCount = 0;

            for (int k = 0; k < orphanOwnerIds.Count; k++) {
                for (int c = 0; c < caches.Count; c++) {
                    releasedCount += caches[c].ForceReleaseOwner(orphanOwnerIds[k]);
                }
            }

            HLogger.Log(
                $"[AssetOwnerIdWatcher] Orphan Clean released {releasedCount} key(s) from {ownerCount} owner(s).");

            // 이 패스에서 목록 재수집 금지. 그리는 중 행 수가 바뀌면 IMGUI 레이아웃 어긋남
            // 갱신은 다음 OnGUI 패스 선두의 _RefreshOccupancy 담당
            Repaint();
        }
        #endregion

        #region Private - Owner Tracker
        private void _DrawOwnerTracker() {
            _DrawOwnerHeader();

            using (EditorGUILayout.ScrollViewScope scope = new(ownerScrollPosition)) {
                ownerScrollPosition = scope.scrollPosition;

                foreach (AssetOwnerIdWatchRegistry.Entry entry in _FilterOwnerEntries())
                    _DrawOwnerEntry(entry);

                for (int k = 0; k < orphanOwnerIds.Count; k++)
                    _DrawOrphanEntry(orphanOwnerIds[k]);
            }
        }

        private void _DrawOwnerHeader() {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
                GUILayout.Label("OwnerId", EditorStyles.boldLabel, GUILayout.Width(WIDTH_OWNER_ID));
                GUILayout.Label("Owner", EditorStyles.boldLabel, GUILayout.Width(WIDTH_OWNER_NAME));
                GUILayout.Label("Class", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CLASS));
                GUILayout.Label("Container", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CONTAINER));
                GUILayout.Label("Alive", EditorStyles.boldLabel, GUILayout.Width(WIDTH_ALIVE));
                GUILayout.Label("Holds", EditorStyles.boldLabel, GUILayout.Width(WIDTH_HOLDS));
                GUILayout.Label("CreatedAt", EditorStyles.boldLabel);
            }
        }

        private IEnumerable<AssetOwnerIdWatchRegistry.Entry> _FilterOwnerEntries() {
            IEnumerable<AssetOwnerIdWatchRegistry.Entry> entries = AssetOwnerIdWatchRegistry.Table.Values
                .OrderBy(entry => entry.OwnerId);

            if (!string.IsNullOrWhiteSpace(searchText)) {
                string query = searchText.Trim();

                entries = entries.Where(entry =>
                    (entry.OwnerId.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrWhiteSpace(entry.OwnerDisplayName) && entry.OwnerDisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrWhiteSpace(entry.ClassName) && entry.ClassName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrWhiteSpace(entry.ContainerName) && entry.ContainerName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            if (showOnlyUnityObjects)
                entries = entries.Where(entry => entry.IsUnityObject);

            if (showOnlyAlive)
                entries = entries.Where(entry => entry.IsAlive);

            return entries;
        }

        private void _DrawOwnerEntry(AssetOwnerIdWatchRegistry.Entry entry) {
            ownerTotalTable.TryGetValue(entry.OwnerId, out int holds);
            bool isExpanded = expandedOwners.Contains(entry.OwnerId);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
                if (GUILayout.Button(isExpanded ? "v" : ">", EditorStyles.label, GUILayout.Width(14f)))
                    _ToggleOwner(entry.OwnerId);

                GUILayout.Label(entry.OwnerId.ToString(), GUILayout.Width(WIDTH_OWNER_ID - 14f));

                using (new EditorGUI.DisabledScope(!entry.IsUnityObject || entry.UnityOwner == null)) {
                    if (GUILayout.Button(entry.OwnerDisplayName ?? "(null)", GUILayout.Width(WIDTH_OWNER_NAME))) {
                        EditorGUIUtility.PingObject(entry.UnityOwner);
                        Selection.activeObject = entry.UnityOwner;
                    }
                }

                GUILayout.Label(entry.ClassName ?? "(null)", GUILayout.Width(WIDTH_CLASS));
                GUILayout.Label(entry.ContainerName ?? "(null)", GUILayout.Width(WIDTH_CONTAINER));
                GUILayout.Label(entry.IsAlive ? "Y" : "N", GUILayout.Width(WIDTH_ALIVE));
                GUILayout.Label(holds.ToString(), EditorStyles.boldLabel, GUILayout.Width(WIDTH_HOLDS));
                GUILayout.Label(entry.CreatedAt ?? "(null)");
            }

            if (isExpanded) _DrawOwnerKeys(entry.OwnerId);
        }

        private void _DrawOrphanEntry(int ownerId) {
            ownerTotalTable.TryGetValue(ownerId, out int holds);
            bool isExpanded = expandedOwners.Contains(ownerId);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
                if (GUILayout.Button(isExpanded ? "v" : ">", EditorStyles.label, GUILayout.Width(14f)))
                    _ToggleOwner(ownerId);

                GUILayout.Label(ownerId.ToString(), GUILayout.Width(WIDTH_OWNER_ID - 14f));
                GUILayout.Label("(ORPHAN)", EditorStyles.boldLabel, GUILayout.Width(WIDTH_OWNER_NAME));

                // 지울 때 남긴 묘비가 있으면 무엇이 샜는지까지 보여준다.
                bool hasTombstone = AssetOwnerIdWatchRegistry.Tombstones.TryGetValue(ownerId, out string lastKnown);
                GUILayout.Label(
                    hasTombstone ? lastKnown : "owner record is gone",
                    GUILayout.Width(WIDTH_CLASS + WIDTH_CONTAINER));
                GUILayout.Label("-", GUILayout.Width(WIDTH_ALIVE));
                GUILayout.Label(holds.ToString(), EditorStyles.boldLabel, GUILayout.Width(WIDTH_HOLDS));
                GUILayout.Label("no live handle. use Orphan Clean in the toolbar");
            }

            if (isExpanded) _DrawOwnerKeys(ownerId);
        }

        private void _DrawOwnerKeys(int ownerId) {
            if (!ownerKeyTable.TryGetValue(ownerId, out List<string> keys)) return;

            for (int k = 0; k < keys.Count; k++) {
                using (new EditorGUILayout.HorizontalScope()) {
                    GUILayout.Space(INDENT);
                    GUILayout.Label(keys[k]);
                }
            }
        }

        private void _ToggleOwner(int ownerId) {
            if (!expandedOwners.Remove(ownerId)) expandedOwners.Add(ownerId);
        }
        #endregion

        #region Private - Resource Ownership
        private void _DrawResourceOwnership() {
            using (EditorGUILayout.ScrollViewScope scope = new(resourceScrollPosition)) {
                resourceScrollPosition = scope.scrollPosition;

                for (int k = 0; k < cacheViews.Count; k++)
                    _DrawCacheView(cacheViews[k]);
            }
        }

        private void _DrawCacheView(CacheView view) {
            EditorGUILayout.LabelField($"{view.Label}    entries {view.Snapshots.Count}", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
                GUILayout.Label("Key", EditorStyles.boldLabel, GUILayout.Width(WIDTH_OWNER_NAME + WIDTH_CLASS));
                GUILayout.Label("Total", EditorStyles.boldLabel, GUILayout.Width(WIDTH_TOTAL));
                GUILayout.Label("Owners", EditorStyles.boldLabel);
            }

            for (int k = 0; k < view.Snapshots.Count; k++)
                _DrawSnapshot(view.Snapshots[k]);

            EditorGUILayout.Space();
        }

        private void _DrawSnapshot(AssetOccupancySnapshot snapshot) {
            if (!_MatchesSearch(snapshot.Key)) return;

            bool isExpanded = expandedKeys.Contains(snapshot.Key);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
                if (GUILayout.Button(isExpanded ? "v" : ">", EditorStyles.label, GUILayout.Width(14f)))
                    _ToggleKey(snapshot.Key);

                GUILayout.Label(snapshot.Key, GUILayout.Width(WIDTH_OWNER_NAME + WIDTH_CLASS - 14f));
                GUILayout.Label(snapshot.TotalCount.ToString(), EditorStyles.boldLabel, GUILayout.Width(WIDTH_TOTAL));
                GUILayout.Label(snapshot.Owners.Count.ToString());
            }

            if (isExpanded) _DrawSnapshotOwners(snapshot);
        }

        private void _DrawSnapshotOwners(AssetOccupancySnapshot snapshot) {
            IReadOnlyList<AssetOwnerOccupancy> owners = snapshot.Owners;

            for (int k = 0; k < owners.Count; k++) {
                AssetOwnerOccupancy occupancy = owners[k];
                bool isOrphan = orphanOwnerIds.Contains(occupancy.OwnerId);

                using (new EditorGUILayout.HorizontalScope()) {
                    GUILayout.Space(INDENT);
                    GUILayout.Label($"owner {occupancy.OwnerId}", GUILayout.Width(WIDTH_OWNER_NAME));
                    GUILayout.Label(isOrphan ? "(ORPHAN)" : _DescribeOwner(occupancy.OwnerId));
                }
            }
        }

        private void _ToggleKey(string key) {
            if (!expandedKeys.Remove(key)) expandedKeys.Add(key);
        }

        private string _DescribeOwner(int ownerId) {
            if (!AssetOwnerIdWatchRegistry.Table.TryGetValue(ownerId, out AssetOwnerIdWatchRegistry.Entry entry))
                return "(unknown)";

            return $"{entry.ClassName}  /  {entry.ContainerName}";
        }

        private bool _MatchesSearch(string key) {
            if (string.IsNullOrWhiteSpace(searchText)) return true;

            return key.IndexOf(searchText.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }
        #endregion
    }
}

/* =============================================================================
 *  Dev Log
 * =========================================================
 *
 * 2026-09-07 (수정) :: 로그를 HLogger 로 통일
 * =========================================================
 * 변경 ::
 * Orphan Clean 의 Debug.Log 를 HLogger.Log 로 바꿨다.
 *
 * 이유 ::
 * 이 어셈블리의 다른 진단 코드(AssetCacheLeakReporter)가 HLogger 를 쓴다. 경로가 갈려 있었다.
 *
 * 결과 ::
 * 에디터 진단 로그가 한 경로로 모인다.
 *
 * 주의 ::
 * asmdef 는 이미 HCUP.HDiagnosis 를 참조하고 있어 참조 추가는 없다.
 *
 * =========================================================
 * 2026-09-06 (수정) :: Orphan Clean 버튼 추가
 * =========================================================
 * 변경 ::
 * 툴바 Clear 옆에 Orphan Clean 을 넣었다.
 * orphan 행의 "ClearCache 밖에 없다" 안내를 툴바 버튼 안내로 교체했다.
 *
 * 이유 ::
 * 목록에 보이는 누수를 창 안에서 정리할 수단이 없었다. 캐시를 통째로 비우는 길뿐이었다.
 *
 * 결과 ::
 * 확인창 1회 뒤 목록의 orphan 을 전부 해제하고 해제한 key 수를 로그로 남긴다.
 * 정리 직후 목록을 다시 뜨지 않고 Repaint 만 건다. 갱신은 다음 OnGUI 패스가 맡는다.
 *
 * 주의 ::
 * orphan 이 0 이면 버튼이 비활성이다. 되돌릴 수 없다.
 * 정리 대상 선별은 이 창의 책임이다. 캐시는 생존을 판정하지 않는다.
 *
 * =========================================================
 * =========================================================
 * 2026-09-04 (수정) :: GC Probe 버튼과 ORPHAN 정체 표시
 * =========================================================
 * 변경 ::
 * - 툴바에 GC Probe 추가. 순수 C# 소유자의 죽음을 즉시 판정하게 한다.
 * - ORPHAN 행이 레지스트리 묘비를 읽어 마지막 정체를 함께 보여준다.
 * - 점유가 사라진 묘비를 _CollectOrphanOwners 에서 버린다.
 *
 * 이유 ::
 * ORPHAN 행이 id 와 개수만 보여주면 무엇이 샜는지 알 수 없어 진단이 되지 않았다.
 *
 * 주의 ::
 * ORPHAN 행은 Alive Only / Unity Only 필터를 타지 않는다. 누수는 필터로 숨길 수 없어야 한다.
 * =========================================================
 * =============================================================================
 * @Jason - PKH 2026.09.04 익명 점유 표시 제거
 *
 * # 변경
 * - Anonymous 열과 (anonymous) 행을 제거했다.
 *
 * # 이유
 * - MemoryAssetCache 에서 익명 축을 제거해 AnonymousDependency 가 사라졌다.
 *   모든 점유가 소유자를 가지므로 표시할 익명이 존재하지 않는다.
 *
 * =============================================================================
 * @Jason - PKH 2026.09.04 Owner Tracker / Resource Ownership 두 탭으로 재구성
 *
 * # 변경
 * - 탭 2개 도입. 기존 소유자 목록을 Owner Tracker 로 흡수하고 Holds 열과 펼침 key 목록을 붙였다.
 * - Resource Ownership 탭 신설. key 별 총 참조와 소유자별 횟수를 표시한다.
 * - 점유는 있는데 소유자 기록이 없는 id 를 ORPHAN 행으로 드러낸다.
 * - EditorApplication.update 로 자동 리페인트 (0.25초 간격). Refresh 버튼은 제거했다.
 * - showOnlyUnityObjects 기본값을 false 로 바꿨다.
 *
 * # 이유
 * - 종전 창은 소유자의 수명만 보여주고 무엇을 잡고 있는지는 전혀 보여주지 못했다.
 *   자료는 MemoryAssetCache 의 table 과 ownerTable 에 이미 양방향으로 있었고 경로만 없었다.
 * - ORPHAN 은 이 창이 새로 잡아내는 상태다. 워처 행이 사라져도 점유는 남을 수 있는데
 *   (Unity 소유자를 그냥 파괴하거나 살아있는 채로 NotifyReleased 를 부른 경우)
 *   종전에는 그 누수를 볼 방법이 아예 없었다.
 * - Unity Only 기본값이 true 라 순수 C# 소유자가 조용히 숨겨졌고 실제로 오진을 유발했다.
 * - 자동 리페인트가 없어 값이 멈춰 보였다. Refresh 를 눌러야만 갱신되는 것을
 *   "반영이 안 된다" 로 읽는 것이 자연스럽다.
 *
 * # 주의
 * - 점유 자료는 AssetCacheDiagnosticsRegistry 에 등록된 캐시에서만 온다.
 *   플레이 중이 아니면 비어 있는 것이 정상이며 툴바에 그 사실을 적는다.
 * - 두 탭은 같은 스냅샷에서 나온다. Resource Ownership 이 원본이고 Owner Tracker 는
 *   그것을 뒤집은 것이라 두 뷰가 서로 어긋날 수 없다.
 *
 * =============================================================================
 * @Jason - PKH 2026.08.06 namespace 와 메뉴 경로 정정
 *
 * # 변경
 * - namespace 를 HResource.Editor.Subscription 으로, 메뉴를 HCUP/Resource/Owner Watcher 로 정정.
 *
 * =============================================================================
 */
#endif
