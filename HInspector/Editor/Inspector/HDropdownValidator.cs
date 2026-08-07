#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * [HDropdown] 필드의 값이 아직 목록에 있는지 전수 점검합니다.
 *
 * 주요 기능 ::
 * 프리팹 · ScriptableObject · 열려 있는 씬을 훑어 목록에서 사라진 값을 보고합니다.
 *
 * 사용법 ::
 * HCUP/Inspector/Validate Dropdown References 메뉴.
 *
 * 왜 필요한가 ::
 * 직렬화 필드의 값은 .prefab / .unity YAML 에 들어가고 컴파일러는 그것을 보지 않는다.
 * 원본 항목(오디오라면 카탈로그의 클립)이 사라져도 컴파일 오류가 나지 않는다 —
 * 게임을 돌려 그 버튼을 눌러 봐야 소리가 안 나는 것으로 안다.
 * 이 점검이 "컴파일 오류" 의 데이터판 등가물이다.
 *
 * 주의 ::
 * 1. 닫혀 있는 씬은 점검하지 못한다. 씬을 여는 것은 부작용(더티 상태·리소스 로드)이 크고,
 *    자동으로 열었다 닫는 도구는 저작자의 작업 중인 씬을 건드릴 위험이 있다.
 *    결과 로그 마지막 줄에 점검하지 못한 씬 수를 반드시 표기한다 — 조용한 축소는 금물이다.
 * 2. 값 0 은 AllowNone 인 필드에서 "선택 없음" 이므로 결함이 아니다.
 * =========================================================
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using HDiagnosis.Logger;

namespace HInspector.Editor {
    public static class HDropdownValidator {
        #region Nested
        private readonly struct Finding {
            public readonly string Context;
            public readonly string Member;
            public readonly string SourceId;
            public readonly int Value;
            public readonly bool SourceMissing;

            public Finding(string context, string member, string sourceId, int value, bool sourceMissing) {
                Context = context;
                Member = member;
                SourceId = sourceId;
                Value = value;
                SourceMissing = sourceMissing;
            }
        }
        #endregion

        #region Fields
        // FieldInfo 수집은 타입당 1회면 충분하다. 프리팹 수천 개를 훑을 때 타입마다
        // 계층을 다시 걷으면 점검 자체가 느려진다.
        static readonly Dictionary<Type, List<(FieldInfo field, HDropdownAttribute attribute)>> fieldCache = new();
        #endregion

        #region Menu
        [MenuItem("HCUP/Inspector/Validate Dropdown References")]
        public static void Validate() {
            fieldCache.Clear();

            var findings = new List<Finding>();
            int scannedObjects = 0;

            scannedObjects += _ScanPrefabs(findings);
            scannedObjects += _ScanScriptableObjects(findings);
            scannedObjects += _ScanOpenScenes(findings);

            int unopenedScenes = _CountUnopenedScenes();

            _Report(findings, scannedObjects, unopenedScenes);
        }
        #endregion

        #region Scan
        private static int _ScanPrefabs(List<Finding> findings) {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            int scanned = 0;

            for (int k = 0; k < guids.Length; k++) {
                string path = AssetDatabase.GUIDToAssetPath(guids[k]);
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null) continue;

                MonoBehaviour[] components = root.GetComponentsInChildren<MonoBehaviour>(true);
                for (int j = 0; j < components.Length; j++) {
                    if (components[j] == null) continue;
                    _Inspect(components[j], path, findings);
                    scanned++;
                }
            }

            return scanned;
        }

        private static int _ScanScriptableObjects(List<Finding> findings) {
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" });
            int scanned = 0;

            for (int k = 0; k < guids.Length; k++) {
                string path = AssetDatabase.GUIDToAssetPath(guids[k]);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null) continue;

                _Inspect(asset, path, findings);
                scanned++;
            }

            return scanned;
        }

        private static int _ScanOpenScenes(List<Finding> findings) {
            int scanned = 0;

            for (int k = 0; k < SceneManager.sceneCount; k++) {
                Scene scene = SceneManager.GetSceneAt(k);
                if (!scene.isLoaded) continue;

                GameObject[] roots = scene.GetRootGameObjects();
                for (int j = 0; j < roots.Length; j++) {
                    MonoBehaviour[] components = roots[j].GetComponentsInChildren<MonoBehaviour>(true);
                    for (int i = 0; i < components.Length; i++) {
                        if (components[i] == null) continue;
                        _Inspect(components[i], $"(scene) {scene.name}", findings);
                        scanned++;
                    }
                }
            }

            return scanned;
        }

        private static int _CountUnopenedScenes() {
            string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });

            var openPaths = new HashSet<string>(StringComparer.Ordinal);
            for (int k = 0; k < SceneManager.sceneCount; k++) {
                Scene scene = SceneManager.GetSceneAt(k);
                if (scene.isLoaded) openPaths.Add(scene.path);
            }

            int unopened = 0;
            for (int k = 0; k < guids.Length; k++) {
                string path = AssetDatabase.GUIDToAssetPath(guids[k]);
                if (!openPaths.Contains(path)) unopened++;
            }

            return unopened;
        }
        #endregion

        #region Inspect
        private static void _Inspect(UnityEngine.Object target, string context, List<Finding> findings) {
            var fields = _GetFields(target.GetType());
            if (fields.Count < 1) return;

            for (int k = 0; k < fields.Count; k++) {
                (FieldInfo field, HDropdownAttribute attribute) = fields[k];

                object raw;
                try { raw = field.GetValue(target); }
                catch { continue; }

                if (raw is not int value) continue;
                if (value == 0 && attribute.AllowNone) continue;

                if (!HDropdownSourceRegistry.IsRegistered(attribute.SourceId)) {
                    findings.Add(new Finding(context, $"{target.GetType().Name}.{field.Name}", attribute.SourceId, value, true));
                    continue;
                }

                if (HDropdownSourceRegistry.TryGetLabel(attribute.SourceId, value, out _)) continue;

                findings.Add(new Finding(context, $"{target.GetType().Name}.{field.Name}", attribute.SourceId, value, false));
            }
        }

        private static List<(FieldInfo, HDropdownAttribute)> _GetFields(Type type) {
            if (fieldCache.TryGetValue(type, out var cached)) return cached;

            var collected = new List<(FieldInfo, HDropdownAttribute)>();

            // 상속 계층을 직접 걷는다. GetFields(NonPublic) 는 상속받은 private/protected 를
            // 돌려주지 않는다 — BaseSfxAddon.overrideClickUid 가 정확히 그 경우다.
            for (Type cursor = type; cursor != null && cursor != typeof(UnityEngine.Object); cursor = cursor.BaseType) {
                FieldInfo[] fields = cursor.GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                for (int k = 0; k < fields.Length; k++) {
                    FieldInfo field = fields[k];
                    if (field.FieldType != typeof(int)) continue;

                    // Unity 가 직렬화하지 않는 필드는 YAML 에 없으므로 점검 대상이 아니다.
                    bool serialized =
                        (field.IsPublic && field.GetCustomAttribute<NonSerializedAttribute>() == null) ||
                        field.GetCustomAttribute<SerializeField>() != null;
                    if (!serialized) continue;

                    var attribute = field.GetCustomAttribute<HDropdownAttribute>(true);
                    if (attribute == null) continue;

                    collected.Add((field, attribute));
                }
            }

            fieldCache[type] = collected;
            return collected;
        }
        #endregion

        #region Report
        private static void _Report(List<Finding> findings, int scannedObjects, int unopenedScenes) {
            var sb = new StringBuilder();
            sb.AppendLine($"[HDropdown] Validation :: objects={scannedObjects}, broken={findings.Count}");

            for (int k = 0; k < findings.Count; k++) {
                Finding f = findings[k];
                string reason = f.SourceMissing
                    ? $"source '{f.SourceId}' is not registered"
                    : $"value {f.Value} is not in source '{f.SourceId}'";

                sb.AppendLine($"  - {f.Context} :: {f.Member} — {reason}");
            }

            if (unopenedScenes > 0) {
                // 점검하지 못한 범위를 반드시 밝힌다. 조용히 줄인 결과는 "전부 확인했다" 로 읽힌다.
                sb.AppendLine($"  ! {unopenedScenes} scene(s) were not checked because they are closed. Open them and run again.");
            }

            if (findings.Count > 0) {
                HLogger.Warning(sb.ToString());
                return;
            }

            HLogger.Log(sb.ToString());
        }
        #endregion
    }
}
#endif

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.08.06 신규 생성
 *
 * # 목적
 * - enum 이 직렬화 필드에 주지 못하는 "참조가 끊겼다" 는 신호를 만든다.
 *
 * # 설계 결정
 * - HInspector 에 두고 소스 ID 를 가리지 않는다. 오디오 전용으로 만들면 다음 도메인
 *   (아이템·스킬·대사)이 같은 도구를 다시 만들게 된다.
 * - 닫힌 씬은 열지 않는다. 자동으로 여는 도구는 저작자가 작업 중인 씬을 건드릴 수 있다.
 *   대신 못 본 씬 수를 결과에 남긴다.
 * - 타입별 FieldInfo 캐시. 프리팹 전수 스캔에서 타입마다 계층을 다시 걷으면 느려진다.
 * - 상속 계층을 DeclaredOnly 로 직접 걷는다. GetFields(NonPublic) 는 상속받은
 *   private/protected 를 돌려주지 않아 BaseSfxAddon.overrideClickUid 를 놓친다.
 *
 * =============================================================================
 */
#endif
