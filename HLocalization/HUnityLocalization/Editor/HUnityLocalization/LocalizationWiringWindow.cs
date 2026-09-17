#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 텍스트 컴포넌트에 LocalizeStringEvent 를 붙여 주는 에디터 창입니다.
 * 탐색 대상과 언어, 컴포넌트 종류를 창에서 고르고, 찾은 항목을 골라서 부분 배선합니다.
 *
 * 특징 / 지원기능 ::
 * + 대상은 프리팹 에셋, 씬 에셋, 하이어라키의 특정 오브젝트 중 하나를 고릅니다.
 * + 탐색 언어의 문자열을 키로 역인덱싱해 현재 텍스트와 대조합니다.
 * + 완전 일치가 없으면 포함 관계를 먼저 보고, 그다음 유사도 임계값으로 찾습니다.
 * + 편집거리 비율만으로는 진짜 포함(0.25)과 무관한 겹침(0.23)이 구분되지 않아 두 규칙을 나눠 둡니다.
 * + 찾은 항목마다 체크박스가 있어 원하는 것만 배선합니다.
 *
 * 주의사항 ::
 * 1. 키를 못 찾은 항목은 배선하지 않고 목록에만 남습니다.
 * 2. 씬 에셋을 고르면 그 씬을 엽니다. 편집 중인 씬은 먼저 저장 여부를 묻습니다.
 * 3. TMP_InputField 의 입력 버퍼 텍스트는 사용자 입력이라 대상에서 제외합니다.
 *
 * 사용법 ::
 * HCUP/Localization/Wiring Window → 대상·언어 지정 → Search → 체크 → Wire Checked
 * =========================================================
 */
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HUnityLocalization {
    public class LocalizationWiringWindow : EditorWindow {
        #region Type
        private enum TargetKind {
            PrefabAsset,
            SceneAsset,
            HierarchyObject,
        }

        private enum TextKind {
            Tmp,
            LegacyText,
            Both,
        }

        private enum MatchKind {
            None,
            Exact,
            SourceInsideValue,
            ValueInsideSource,
            Similar,
        }

        private enum SortMode {
            Default,
            KeyFirst,
            NoKeyFirst,
        }

        private class WiringCandidate {
            public bool IsChecked;
            public string ObjectPath;
            public string SourceText;
            public string Key;
            public bool IsTmp;
            public bool IsAlreadyWired;
            public bool IsExactMatch;
            public float MatchScore;
            public MatchKind Kind;
            public int OrderIndex;
        }
        #endregion

        #region 상수
        private const string MENU_PATH = "HCUP/Localization/Wiring Window";
        private const string WINDOW_TITLE = "Localization Wiring";
        private const float LABEL_WIDTH = 150f;
        private const float KEY_COLUMN_WIDTH = 260f;
        private const float RESULT_VIEW_HEIGHT = 320f;
        private const float DEFAULT_PARTIAL_THRESHOLD = 0.8f;
        private const float MIN_PARTIAL_THRESHOLD = 0.3f;
        private const float MAX_PARTIAL_THRESHOLD = 1f;
        private const int MAX_COMPARE_LENGTH = 256;
        private const int MIN_CONTAIN_LENGTH = 2;
        #endregion

        #region 변수
        private TargetKind targetKind = TargetKind.PrefabAsset;
        private TextKind textKind = TextKind.Tmp;

        private GameObject prefabAsset;
        private SceneAsset sceneAsset;
        private GameObject hierarchyObject;

        private StringTableCollection[] collections = new StringTableCollection[0];
        private string[] collectionNames = new string[0];
        private int collectionIndex;

        private Locale[] locales = new Locale[0];
        private string[] localeNames = new string[0];
        private int sourceLocaleIndex;
        private int previewLocaleIndex;

        private bool usePartialMatch;
        private float partialMatchThreshold = DEFAULT_PARTIAL_THRESHOLD;

        private readonly List<WiringCandidate> candidates = new List<WiringCandidate>();
        private readonly List<WiringCandidate> visibleCandidates = new List<WiringCandidate>();

        private SortMode sortMode = SortMode.Default;
        private bool showWithKey = true;
        private bool showWithoutKey = true;

        private Vector2 resultScroll;
        private string statusMessage = string.Empty;
        #endregion

        #region Getter/Setter
        private StringTableCollection _SelectedCollection =>
            collections.Length == 0 ? null : collections[Mathf.Clamp(collectionIndex, 0, collections.Length - 1)];

        private Locale _SourceLocale =>
            locales.Length == 0 ? null : locales[Mathf.Clamp(sourceLocaleIndex, 0, locales.Length - 1)];

        private Locale _PreviewLocale =>
            locales.Length == 0 ? null : locales[Mathf.Clamp(previewLocaleIndex, 0, locales.Length - 1)];

        private int _CheckedCount {
            get {
                int count = 0;
                for (int k = 0; k < candidates.Count; k++) {
                    if (candidates[k].IsChecked) count++;
                }
                return count;
            }
        }
        #endregion


        #region 에디터 윈도우 라이프사이클
        [MenuItem(MENU_PATH)]
        private static void _Open() {
            GetWindow<LocalizationWiringWindow>(WINDOW_TITLE).Show();
        }

        void OnEnable() {
            _ReloadLocalizationSources();
        }

        void OnGUI() {
            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            _DrawTargetSection();
            EditorGUILayout.Space(8f);
            _DrawLanguageSection();
            EditorGUILayout.Space(8f);
            _DrawActionSection();
            EditorGUILayout.Space(8f);
            _DrawResultSection();

            if (!string.IsNullOrEmpty(statusMessage)) {
                EditorGUILayout.Space(4f);
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            }
        }
        #endregion


        #region Private - 섹션 그리기
        private void _DrawTargetSection() {
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
            targetKind = (TargetKind)EditorGUILayout.EnumPopup("Target Kind", targetKind);

            switch (targetKind) {
            case TargetKind.PrefabAsset:
                prefabAsset = (GameObject)EditorGUILayout.ObjectField("Prefab", prefabAsset, typeof(GameObject), false);
                break;
            case TargetKind.SceneAsset:
                sceneAsset = (SceneAsset)EditorGUILayout.ObjectField("Scene", sceneAsset, typeof(SceneAsset), false);
                break;
            case TargetKind.HierarchyObject:
                hierarchyObject = (GameObject)EditorGUILayout.ObjectField("Root Object", hierarchyObject, typeof(GameObject), true);
                break;
            }

            textKind = (TextKind)EditorGUILayout.EnumPopup("Text Component", textKind);
        }

        private void _DrawLanguageSection() {
            EditorGUILayout.LabelField("Language", EditorStyles.boldLabel);

            if (collections.Length == 0 || locales.Length == 0) {
                EditorGUILayout.HelpBox(
                    "테이블 컬렉션 또는 Locale 이 없습니다. HUnityLocalization Import 를 먼저 실행하세요.",
                    MessageType.Warning);
                if (GUILayout.Button("Reload Localization Sources")) {
                    _ReloadLocalizationSources();
                }
                return;
            }

            collectionIndex = EditorGUILayout.Popup("Table Collection", collectionIndex, collectionNames);
            sourceLocaleIndex = EditorGUILayout.Popup("Search Language", sourceLocaleIndex, localeNames);

            EditorGUILayout.BeginHorizontal();
            previewLocaleIndex = EditorGUILayout.Popup("Preview Language", previewLocaleIndex, localeNames);
            if (GUILayout.Button("Apply", GUILayout.Width(70f))) {
                _ApplyPreviewLocale();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Matching", EditorStyles.boldLabel);
            usePartialMatch = EditorGUILayout.Toggle("Allow Partial Match", usePartialMatch);
            if (usePartialMatch) {
                partialMatchThreshold = EditorGUILayout.Slider(
                    "Similarity Threshold", partialMatchThreshold, MIN_PARTIAL_THRESHOLD, MAX_PARTIAL_THRESHOLD);
                EditorGUILayout.HelpBox(
                    "완전 일치가 없으면 포함 후보와 유사도 후보를 함께 구해 점수가 높은 쪽을 올립니다.\n"
                    + $"둘 다 이 값을 넘어야 하고, 포함 판정은 {MIN_CONTAIN_LENGTH}글자 이상부터 적용합니다. "
                    + "부분 일치는 자동 체크되지 않습니다.",
                    MessageType.None);
            }

            if (GUILayout.Button("Reload Localization Sources")) {
                _ReloadLocalizationSources();
            }
        }

        private void _DrawActionSection() {
            EditorGUILayout.LabelField("Action", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Search", GUILayout.Height(28f))) {
                _Search();
            }

            EditorGUI.BeginDisabledGroup(_CheckedCount == 0);
            if (GUILayout.Button($"Wire Checked ({_CheckedCount})", GUILayout.Height(28f))) {
                _WireChecked();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        private void _DrawResultSection() {
            _RebuildVisibleCandidates();
            EditorGUILayout.LabelField(
                $"Result ({visibleCandidates.Count} / {candidates.Count})", EditorStyles.boldLabel);
            if (candidates.Count == 0) return;

            EditorGUILayout.BeginHorizontal();
            sortMode = (SortMode)EditorGUILayout.EnumPopup("Sort", sortMode);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Filter", GUILayout.Width(LABEL_WIDTH - 4f));
            showWithKey = GUILayout.Toggle(showWithKey, "With Key", GUILayout.Width(90f));
            showWithoutKey = GUILayout.Toggle(showWithoutKey, "Without Key", GUILayout.Width(110f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Check All With Key", GUILayout.Width(150f))) {
                _SetCheckedForMatched(true);
            }
            if (GUILayout.Button("Uncheck All", GUILayout.Width(110f))) {
                _SetCheckedForMatched(false);
            }
            EditorGUILayout.EndHorizontal();

            if (visibleCandidates.Count == 0) {
                EditorGUILayout.HelpBox("필터에 걸려 표시할 항목이 없습니다.", MessageType.None);
                return;
            }

            resultScroll = EditorGUILayout.BeginScrollView(resultScroll, GUILayout.Height(RESULT_VIEW_HEIGHT));
            for (int k = 0; k < visibleCandidates.Count; k++) {
                WiringCandidate candidate = visibleCandidates[k];
                bool hasKey = !string.IsNullOrEmpty(candidate.Key);

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(!hasKey || candidate.IsAlreadyWired);
                candidate.IsChecked = EditorGUILayout.Toggle(candidate.IsChecked, GUILayout.Width(18f));
                EditorGUI.EndDisabledGroup();

                string keyLabel;
                if (candidate.IsAlreadyWired) keyLabel = "(already wired)";
                else if (!hasKey) keyLabel = "(no key)";
                else {
                    switch (candidate.Kind) {
                    case MatchKind.Exact:
                        keyLabel = candidate.Key;
                        break;
                    case MatchKind.SourceInsideValue:
                        keyLabel = $"in value {candidate.MatchScore:0.00} {candidate.Key}";
                        break;
                    case MatchKind.ValueInsideSource:
                        keyLabel = $"in text  {candidate.MatchScore:0.00} {candidate.Key}";
                        break;
                    default:
                        keyLabel = $"~{candidate.MatchScore:0.00} {candidate.Key}";
                        break;
                    }
                }
                EditorGUILayout.LabelField(keyLabel, GUILayout.Width(KEY_COLUMN_WIDTH));
                EditorGUILayout.LabelField($"{candidate.ObjectPath}  \"{candidate.SourceText}\"");
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        #endregion


        #region Private - 로컬라이제이션 소스
        private void _ReloadLocalizationSources() {
            ReadOnlyCollection<StringTableCollection> loadedCollections = LocalizationEditorSettings.GetStringTableCollections();
            collections = new StringTableCollection[loadedCollections.Count];
            collectionNames = new string[loadedCollections.Count];
            for (int k = 0; k < loadedCollections.Count; k++) {
                collections[k] = loadedCollections[k];
                collectionNames[k] = loadedCollections[k].TableCollectionName;
            }

            ReadOnlyCollection<Locale> loadedLocales = LocalizationEditorSettings.GetLocales();
            locales = new Locale[loadedLocales.Count];
            localeNames = new string[loadedLocales.Count];
            for (int k = 0; k < loadedLocales.Count; k++) {
                locales[k] = loadedLocales[k];
                localeNames[k] = $"{loadedLocales[k].Identifier.Code} ({loadedLocales[k].LocaleName})";
            }

            collectionIndex = Mathf.Clamp(collectionIndex, 0, Mathf.Max(0, collections.Length - 1));
            sourceLocaleIndex = Mathf.Clamp(sourceLocaleIndex, 0, Mathf.Max(0, locales.Length - 1));
            previewLocaleIndex = Mathf.Clamp(previewLocaleIndex, 0, Mathf.Max(0, locales.Length - 1));
        }

        private void _ApplyPreviewLocale() {
            Locale locale = _PreviewLocale;
            if (locale == null) {
                statusMessage = "Preview 로 지정할 Locale 이 없습니다.";
                return;
            }
            LocalizationSettings.SelectedLocale = locale;
            statusMessage = $"Preview locale = {locale.Identifier.Code}";
        }

        /// <summary> 탐색 언어의 번역 문자열을 키로 역인덱싱한다. 같은 문자열이 여러 키에 있으면 첫 키를 남긴다. </summary>
        private Dictionary<string, string> _GetTextToKeyTable() {
            StringTableCollection collection = _SelectedCollection;
            Locale locale = _SourceLocale;
            if (collection == null || locale == null) return null;

            StringTable sourceTable = null;
            foreach (StringTable table in collection.StringTables) {
                if (table != null && table.LocaleIdentifier.Code == locale.Identifier.Code) {
                    sourceTable = table;
                    break;
                }
            }
            if (sourceTable == null) {
                statusMessage = $"'{locale.Identifier.Code}' StringTable 이 '{collection.TableCollectionName}' 에 없습니다.";
                return null;
            }

            Dictionary<string, string> textToKey = new Dictionary<string, string>();
            foreach (SharedTableData.SharedTableEntry entry in collection.SharedData.Entries) {
                string value = sourceTable.GetEntry(entry.Id)?.Value;
                if (string.IsNullOrWhiteSpace(value)) continue;

                string trimmed = value.Trim();
                if (!textToKey.ContainsKey(trimmed)) textToKey.Add(trimmed, entry.Key);
            }
            return textToKey;
        }
        #endregion


        #region Private - 탐색
        private void _Search() {
            candidates.Clear();
            statusMessage = string.Empty;

            Dictionary<string, string> textToKey = _GetTextToKeyTable();
            if (textToKey == null) {
                statusMessage = string.IsNullOrEmpty(statusMessage) ? "탐색 언어 테이블을 읽지 못했습니다." : statusMessage;
                return;
            }

            GameObject root = _GetSearchRoot(out bool isPrefabContents);
            if (root == null) return;

            try {
                _CollectCandidates(root, textToKey);
            }
            finally {
                if (isPrefabContents) PrefabUtility.UnloadPrefabContents(root);
            }

            int exact = 0;
            int contained = 0;
            int similar = 0;
            for (int k = 0; k < candidates.Count; k++) {
                WiringCandidate candidate = candidates[k];
                if (candidate.IsAlreadyWired || string.IsNullOrEmpty(candidate.Key)) continue;

                switch (candidate.Kind) {
                case MatchKind.Exact: exact++; break;
                case MatchKind.SourceInsideValue: contained++; break;
                case MatchKind.ValueInsideSource: contained++; break;
                default: similar++; break;
                }
            }
            statusMessage =
                $"검색 {candidates.Count}건, 완전 일치 {exact}건, 포함 {contained}건, 유사 {similar}건. 체크한 항목만 배선합니다.";
        }

        /// <summary> 탐색 기준 루트를 돌려준다. 프리팹 에셋이면 사본을 열고 isPrefabContents 가 true 가 된다. </summary>
        private GameObject _GetSearchRoot(out bool isPrefabContents) {
            isPrefabContents = false;

            switch (targetKind) {
            case TargetKind.PrefabAsset:
                if (prefabAsset == null) {
                    statusMessage = "Prefab 을 지정하세요.";
                    return null;
                }
                isPrefabContents = true;
                return PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefabAsset));

            case TargetKind.SceneAsset:
                if (sceneAsset == null) {
                    statusMessage = "Scene 을 지정하세요.";
                    return null;
                }
                return _OpenSceneAndGetRoot();

            case TargetKind.HierarchyObject:
                if (hierarchyObject == null) {
                    statusMessage = "Root Object 를 지정하세요.";
                    return null;
                }
                return hierarchyObject;
            }
            return null;
        }

        private GameObject _OpenSceneAndGetRoot() {
            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            if (SceneManager.GetActiveScene().path != scenePath) {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                    statusMessage = "씬 저장을 취소했습니다.";
                    return null;
                }
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            if (roots.Length == 0) {
                statusMessage = "씬에 루트 오브젝트가 없습니다.";
                return null;
            }

            // 씬은 루트가 여러 개다. 실제 순회는 _CollectCandidates 와 _FindByPath 가 전체 루트를 훑고,
            // 여기서는 대상이 유효하다는 표시로 첫 루트만 돌려준다.
            return roots[0];
        }

        private void _CollectCandidates(GameObject root, Dictionary<string, string> textToKey) {
            if (targetKind == TargetKind.SceneAsset) {
                GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
                for (int k = 0; k < roots.Length; k++) {
                    _CollectFromRoot(roots[k], textToKey);
                }
                return;
            }
            _CollectFromRoot(root, textToKey);
        }

        private void _CollectFromRoot(GameObject root, Dictionary<string, string> textToKey) {
            if (textKind == TextKind.Tmp || textKind == TextKind.Both) {
                foreach (TextMeshProUGUI tmpText in root.GetComponentsInChildren<TextMeshProUGUI>(true)) {
                    if (_IsInputFieldBuffer(tmpText)) continue;
                    _AddCandidate(tmpText, tmpText.text, true, textToKey);
                }
            }
            if (textKind == TextKind.LegacyText || textKind == TextKind.Both) {
                foreach (Text legacyText in root.GetComponentsInChildren<Text>(true)) {
                    _AddCandidate(legacyText, legacyText.text, false, textToKey);
                }
            }
        }

        private void _AddCandidate(Component source, string rawText, bool isTmp, Dictionary<string, string> textToKey) {
            string trimmed = rawText == null ? string.Empty : rawText.Trim();
            if (trimmed.Length == 0) return;

            string key = _GetBestKey(trimmed, textToKey, out MatchKind kind, out float score);
            bool isExact = kind == MatchKind.Exact;
            bool isWired = source.GetComponent<LocalizeStringEvent>() != null;

            candidates.Add(new WiringCandidate {
                IsChecked = !isWired && isExact,
                ObjectPath = _GetHierarchyPath(source.transform),
                SourceText = trimmed,
                Key = key,
                IsTmp = isTmp,
                IsAlreadyWired = isWired,
                IsExactMatch = isExact,
                MatchScore = score,
                Kind = kind,
                OrderIndex = candidates.Count,
            });
        }
        #endregion


        #region Private - 배선
        private void _WireChecked() {
            GameObject root = _GetSearchRoot(out bool isPrefabContents);
            if (root == null) return;

            int wired = 0;
            try {
                for (int k = 0; k < candidates.Count; k++) {
                    WiringCandidate candidate = candidates[k];
                    if (!candidate.IsChecked || string.IsNullOrEmpty(candidate.Key)) continue;

                    Component target = _FindByPath(root, candidate);
                    if (target == null) continue;

                    _Attach(target, candidate.Key, isPrefabContents);
                    candidate.IsAlreadyWired = true;
                    candidate.IsChecked = false;
                    wired++;
                }

                if (isPrefabContents && wired > 0) {
                    PrefabUtility.SaveAsPrefabAsset(root, AssetDatabase.GetAssetPath(prefabAsset));
                }
            }
            finally {
                if (isPrefabContents) PrefabUtility.UnloadPrefabContents(root);
            }

            if (!isPrefabContents && wired > 0) {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
            AssetDatabase.SaveAssets();
            statusMessage = $"{wired}건 배선했습니다.";
        }

        /// <summary> 프리팹 사본은 탐색 때와 다른 인스턴스라 경로로 다시 찾는다. </summary>
        private Component _FindByPath(GameObject root, WiringCandidate candidate) {
            if (targetKind == TargetKind.SceneAsset) {
                GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
                for (int k = 0; k < roots.Length; k++) {
                    Component found = _FindInRoot(roots[k], candidate);
                    if (found != null) return found;
                }
                return null;
            }
            return _FindInRoot(root, candidate);
        }

        private Component _FindInRoot(GameObject root, WiringCandidate candidate) {
            if (candidate.IsTmp) {
                foreach (TextMeshProUGUI tmpText in root.GetComponentsInChildren<TextMeshProUGUI>(true)) {
                    if (_GetHierarchyPath(tmpText.transform) == candidate.ObjectPath) return tmpText;
                }
                return null;
            }
            foreach (Text legacyText in root.GetComponentsInChildren<Text>(true)) {
                if (_GetHierarchyPath(legacyText.transform) == candidate.ObjectPath) return legacyText;
            }
            return null;
        }

        // 배선 형태는 패키지의 LocalizeComponent_TMPro / LocalizeComponent_UGUI 와 같다.
        // 그 타입들은 internal 이라 호출할 수 없어 같은 절차를 옮겼다.
        private void _Attach(Component target, string key, bool isPrefabContents) {
            LocalizeStringEvent component = isPrefabContents
                ? target.gameObject.AddComponent<LocalizeStringEvent>()
                : Undo.AddComponent<LocalizeStringEvent>(target.gameObject);

            component.StringReference.SetReference(_SelectedCollection.TableCollectionName, key);

            System.Reflection.MethodInfo setter = target.GetType().GetProperty("text").GetSetMethod();
            UnityAction<string> call =
                System.Delegate.CreateDelegate(typeof(UnityAction<string>), target, setter) as UnityAction<string>;
            UnityEventTools.AddPersistentListener(component.OnUpdateString, call);
            component.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
        }
        #endregion


        #region Private - 보조
        /// <summary> 보이는 항목만 체크한다. 필터로 숨긴 것까지 켜면 사용자가 못 본 항목이 배선된다. </summary>
        private void _SetCheckedForMatched(bool isChecked) {
            for (int k = 0; k < visibleCandidates.Count; k++) {
                WiringCandidate candidate = visibleCandidates[k];
                if (candidate.IsAlreadyWired || string.IsNullOrEmpty(candidate.Key)) continue;
                candidate.IsChecked = isChecked;
            }
        }

        /// <summary> 필터를 적용하고 정렬한 표시용 목록을 다시 만든다. 원본 candidates 의 순서는 건드리지 않는다. </summary>
        private void _RebuildVisibleCandidates() {
            visibleCandidates.Clear();
            for (int k = 0; k < candidates.Count; k++) {
                WiringCandidate candidate = candidates[k];
                bool hasKey = !string.IsNullOrEmpty(candidate.Key);
                if (hasKey && !showWithKey) continue;
                if (!hasKey && !showWithoutKey) continue;
                visibleCandidates.Add(candidate);
            }

            if (sortMode == SortMode.Default) return;
            visibleCandidates.Sort(_CompareCandidates);
        }

        /// <summary> 키 유무를 1차 기준으로 비교하고, 같으면 발견 순서를 유지한다. </summary>
        private int _CompareCandidates(WiringCandidate left, WiringCandidate right) {
            int leftHasKey = string.IsNullOrEmpty(left.Key) ? 0 : 1;
            int rightHasKey = string.IsNullOrEmpty(right.Key) ? 0 : 1;

            if (leftHasKey != rightHasKey) {
                bool keyFirst = sortMode == SortMode.KeyFirst;
                if (keyFirst) return rightHasKey - leftHasKey;
                return leftHasKey - rightHasKey;
            }
            return left.OrderIndex - right.OrderIndex;
        }

        /// <summary> TMP_InputField 의 입력 버퍼인지. 사용자 입력이라 번역 대상이 아니다. </summary>
        private bool _IsInputFieldBuffer(TextMeshProUGUI tmpText) {
            TMP_InputField field = tmpText.GetComponentInParent<TMP_InputField>(true);
            return field != null && field.textComponent == tmpText;
        }

        /// <summary>
        /// 완전 일치를 먼저 보고, 없으면 포함 후보와 유사도 후보를 함께 구해 점수가 높은 쪽을 고른다. 못 찾으면 null.
        /// 두 규칙을 병행하는 이유는 서로 다른 것을 재기 때문이다. 포함은 조사가 붙은 부분 문장을 잡고,
        /// 편집거리 비율은 오타와 문장부호 차이를 잡는다. 둘 다 partialMatchThreshold 를 넘어야 후보가 된다.
        /// </summary>
        private string _GetBestKey(string source, Dictionary<string, string> textToKey, out MatchKind kind, out float score) {
            if (textToKey.TryGetValue(source, out string exactKey)) {
                kind = MatchKind.Exact;
                score = 1f;
                return exactKey;
            }

            kind = MatchKind.None;
            score = 0f;
            if (!usePartialMatch) return null;

            string containKey = _GetContainedKey(source, textToKey, out MatchKind containKind, out float containScore);
            if (containScore < partialMatchThreshold) containKey = null;

            string similarKey = _GetSimilarKey(source, textToKey, out float similarScore);
            if (similarScore < partialMatchThreshold) similarKey = null;

            // 같은 점수면 포함을 택한다. 문자열이 실제로 들어 있다는 사실이 편집거리 추정보다 확실하다.
            if (containKey != null && containScore >= similarScore) {
                kind = containKind;
                score = containScore;
                return containKey;
            }
            if (similarKey != null) {
                kind = MatchKind.Similar;
                score = similarScore;
                return similarKey;
            }
            return null;
        }

        /// <summary> 편집거리 비율이 가장 높은 키를 돌려준다. 임계값 판정은 호출부가 한다. </summary>
        private string _GetSimilarKey(string source, Dictionary<string, string> textToKey, out float score) {
            string bestKey = null;
            float bestScore = 0f;
            foreach (KeyValuePair<string, string> pair in textToKey) {
                float similarity = _GetSimilarity(source, pair.Key);
                if (similarity <= bestScore) continue;

                bestScore = similarity;
                bestKey = pair.Value;
            }
            score = bestScore;
            return bestKey;
        }

        /// <summary> 한쪽이 다른 쪽을 품는 키를 찾는다. 길이 차이가 가장 작은 것을 고르고, 점수는 짧은 쪽 / 긴 쪽 비율이다. </summary>
        private string _GetContainedKey(string source, Dictionary<string, string> textToKey, out MatchKind kind, out float score) {
            kind = MatchKind.None;
            score = 0f;
            if (source.Length < MIN_CONTAIN_LENGTH) return null;

            string bestKey = null;
            int bestGap = int.MaxValue;
            foreach (KeyValuePair<string, string> pair in textToKey) {
                string value = pair.Key;
                if (value.Length < MIN_CONTAIN_LENGTH) continue;

                bool sourceInside = value.Contains(source);
                bool valueInside = source.Contains(value);
                if (!sourceInside && !valueInside) continue;

                int gap = Mathf.Abs(value.Length - source.Length);
                if (gap >= bestGap) continue;

                bestGap = gap;
                bestKey = pair.Value;
                kind = sourceInside ? MatchKind.SourceInsideValue : MatchKind.ValueInsideSource;
                score = (float)Mathf.Min(source.Length, value.Length) / Mathf.Max(source.Length, value.Length);
            }
            return bestKey;
        }

        /// <summary> 두 문자열의 닮은 정도를 0~1 로 돌려준다. 1 - 편집거리 / 긴 쪽 길이. </summary>
        private float _GetSimilarity(string left, string right) {
            if (left.Length == 0 || right.Length == 0) return 0f;

            string shortLeft = left.Length > MAX_COMPARE_LENGTH ? left.Substring(0, MAX_COMPARE_LENGTH) : left;
            string shortRight = right.Length > MAX_COMPARE_LENGTH ? right.Substring(0, MAX_COMPARE_LENGTH) : right;

            int distance = _GetEditDistance(shortLeft, shortRight);
            int longer = Mathf.Max(shortLeft.Length, shortRight.Length);
            return 1f - ((float)distance / longer);
        }

        /// <summary> Levenshtein 편집거리. 행 두 개만 들고 계산해 긴 문자열에서도 메모리가 늘지 않는다. </summary>
        private int _GetEditDistance(string left, string right) {
            int[] previous = new int[right.Length + 1];
            int[] current = new int[right.Length + 1];

            for (int k = 0; k <= right.Length; k++) {
                previous[k] = k;
            }

            for (int i = 1; i <= left.Length; i++) {
                current[0] = i;
                for (int j = 1; j <= right.Length; j++) {
                    int cost = left[i - 1] == right[j - 1] ? 0 : 1;
                    current[j] = Mathf.Min(Mathf.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                }
                int[] swap = previous;
                previous = current;
                current = swap;
            }
            return previous[right.Length];
        }

        private string _GetHierarchyPath(Transform target) {
            List<string> names = new List<string>();
            for (Transform current = target; current != null; current = current.parent) {
                names.Add(current.name);
            }
            names.Reverse();
            return string.Join("/", names);
        }
        #endregion
    }
}
#endif

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.09.17 HCUP 패키지로 이관 - HUnityLocalization Editor 어셈블리
 *
 * # 변경
 * - 파일 위치: 소비 프로젝트의 Editor 폴더 → HUnityLocalization/Editor/HUnityLocalization
 * - 네임스페이스: 소비 프로젝트 전용 → HUnityLocalization
 * - 메뉴 경로: MENU_PATH 를 HCUP/Localization/Wiring Window 로 변경
 *
 * # 이유
 * - 네이티브 Localization 전용 유틸리티라 같은 파이프라인을 쓰는 다른 프로젝트에서도 그대로 쓸 수 있다.
 * - 메뉴와 네임스페이스에 소비 프로젝트 이름이 박혀 있으면 패키지가 특정 프로젝트에 종속된다.
 *
 * # 결과
 * - asmdef 에 Unity.TextMeshPro 와 HCUP.HUnityLocalization 추가. TMP 타입과 LocaleCodeMap 을 쓰기 위함이다.
 *
 * # 주의
 * - 이 어셈블리는 defineConstraints 가 걸려 com.unity.localization 미설치 환경에서는 컴파일 대상에서 빠진다.
 *
 * =============================================================================
 * @Jason - PKH 2026.09.17 Result 목록에 정렬과 필터 추가
 *
 * # 추가
 * - SortMode (Default / KeyFirst / NoKeyFirst). Default 는 발견 순서 그대로다.
 * - showWithKey / showWithoutKey 독립 토글. 키 있는 것과 없는 것을 각각 끄고 켠다.
 * - WiringCandidate.OrderIndex 와 visibleCandidates. 표시용 목록만 필터·정렬하고 원본 순서는 보존한다.
 * - 헤더가 표시 건수 / 전체 건수를 함께 보여준다.
 *
 * # 설계 결정
 * - 원본 candidates 를 정렬하지 않는다. Default 로 되돌릴 때 발견 순서를 정확히 복원해야 하고, 정렬 변경이 체크 상태를 흔들지 않아야 한다.
 * - 비교자는 키 유무를 1차 기준으로만 쓰고 동순위는 OrderIndex 로 갈라 안정 정렬이 되게 한다.
 * - Check All With Key 는 보이는 항목에만 적용한다. 필터로 숨긴 항목이 체크되면 사용자가 보지 못한 것이 배선된다.
 *
 * =============================================================================
 * @Jason - PKH 2026.09.17 포함과 유사도를 선점이 아니라 점수 경쟁으로 교체
 *
 * # 수정
 * - _GetBestKey 가 포함 후보와 유사도 후보를 모두 구한 뒤 점수가 높은 쪽을 고른다. 같으면 포함을 택한다.
 * - 포함 후보도 partialMatchThreshold 를 넘어야 후보가 된다. 종전에는 임계값이 포함 판정에 전혀 걸리지 않았다.
 * - 유사도 탐색을 _GetSimilarKey 로 분리했다.
 *
 * # 이유
 * - 직전 구현을 실측했더니 두 가지가 깨져 있었다.
 *   "네트워크 연결을 확인하세요!" 가 2글자 값 "연결"(0.13)에 물려서, 정답인 "네트워크를 확인하세요"(유사도 0.667)가 후보에도 오르지 못했다.
 *   "서버와 연결이 끊어졌습니다!" 도 같은 이유로 "연결"(0.13)을 골랐다. 임계값을 0.8 에서 0.6 으로 내려도 결과가 같았다.
 * - 원인은 포함이 유사도를 무조건 선점하고, 포함에는 점수 하한이 없던 것이다. 사용자가 요청한 "병행" 은 순차 우선순위가 아니라 경쟁이다.
 *
 * =============================================================================
 * @Jason - PKH 2026.09.17 부분 일치를 포함 규칙과 유사도로 분리
 *
 * # 변경
 * - MatchKind (None / Exact / SourceInsideValue / ValueInsideSource / Similar) 도입. 결과 목록과 상태줄이 규칙별로 나눠 표시한다.
 * - _GetBestKey 를 완전 일치 → 포함 → 유사도 순서로 재구성하고 _GetContainedKey 를 신설했다.
 * - MIN_CONTAIN_LENGTH(2) 미만 문자열은 포함 판정에서 제외한다. 한 글자가 아무것에나 물리는 것을 막는다.
 *
 * # 이유
 * - 편집거리 비율을 실측했더니 길이 차이에 지배됐다. "로그인" 이 "로그인에 실패했습니다." 안에 온전히 들어 있어도 0.250 인데,
 *   무관한 "2~12자로 입력하세요" 와 "닉네임은 2~10자여야 합니다." 가 0.235 였다. 임계값 하나로 두 경우를 가를 수 없다.
 * - 그래서 포함 여부는 임계값과 무관한 독립 규칙으로 두고, 임계값은 오타 수준의 차이에만 쓰이게 했다.
 *
 * # 주의
 * - ValueInsideSource(테이블 값이 화면 텍스트보다 짧은 경우)로 배선하면 표시 문구가 짧아진다. 자동 체크하지 않는 이유다.
 *
 * =============================================================================
 * @Jason - PKH 2026.09.17 부분 일치 탐색 옵션 추가
 *
 * # 추가
 * - usePartialMatch (Boolean) 와 partialMatchThreshold (0.3 ~ 1, 기본 0.8). 완전 일치가 없을 때만 부분 일치를 시도한다.
 * - _GetBestKey / _GetSimilarity / _GetEditDistance. 유사도는 1 - 편집거리 / 긴 쪽 길이 로 0~1 정규화한다.
 * - 결과 목록이 부분 일치에 `~0.86 key` 형태로 점수를 표시하고, 상태줄이 완전·부분 건수를 나눠 센다.
 *
 * # 설계 결정
 * - 유사도 척도로 Levenshtein 비율을 골랐다. 부분 포함과 오타를 한 척도로 잡고 0~1 로 정규화되어 임계값 하나로 조절된다.
 * - 부분 일치는 체크를 자동으로 켜지 않는다. 완전 일치만 기본 체크이고 부분 일치는 사람이 점수를 보고 켠다. 잘못된 키를 조용히 배선하는 쪽이 더 비싸다.
 * - 편집거리는 행 두 개만 유지해 계산한다. 비교 길이는 MAX_COMPARE_LENGTH 로 잘라 긴 본문에서 비용이 튀지 않게 한다.
 *
 * =============================================================================
 * @Jason - PKH 2026.09.17 LocalizationWiringWindow 베이스 코드 생성
 *
 * # 목적
 * - 메뉴 고정 방식(LocalizationWiringUtility)을 대체한다. 대상·언어·컴포넌트 종류를 창에서 고르고 항목별로 부분 배선한다.
 *
 * # 사용 흐름
 * - 창 열기 → Target(프리팹 / 씬 / 하이어라키 오브젝트) 지정 → Table Collection 과 Search Language 선택 → Search → 체크 → Wire Checked.
 *
 * # 설계 결정
 * - 탐색 결과는 컴포넌트 참조가 아니라 경로 문자열로 들고 있는다. 프리팹 사본은 UnloadPrefabContents 뒤 참조가 무효가 되므로 배선 시 경로로 다시 찾는다.
 * - 프리팹은 AddComponent + SaveAsPrefabAsset, 씬과 하이어라키는 Undo.AddComponent + MarkSceneDirty 로 갈라 처리한다. 사본에는 Undo 가 걸리지 않는다.
 * - 배선 절차는 패키지의 LocalizeComponent_TMPro / LocalizeComponent_UGUI 와 동일하다. 두 타입이 internal 이라 호출하지 못해 같은 다섯 줄을 옮겼다.
 * - 키를 못 찾은 항목은 체크 자체를 막는다. 잘못된 키로 배선하는 것보다 목록에 남겨 사람이 판단하는 편이 안전하다.
 *
 * # 주의
 * - 씬 에셋을 고르면 그 씬을 연다. 편집 중인 씬은 SaveCurrentModifiedScenesIfUserWantsTo 로 먼저 묻는다.
 *
 * =============================================================================
 */
#endif
