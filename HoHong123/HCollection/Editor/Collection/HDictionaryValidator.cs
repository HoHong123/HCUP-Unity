#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * HDictionary의 중복 키를 하드 에러로 취급해 플레이 모드 진입/빌드/씬 저장을 차단한다.
 *
 * 차단 지점 ::
 * 1. Play Mode - EditorApplication.playModeStateChanged (ExitingEditMode에서 취소)
 * 2. Build - IPreprocessBuildWithReport (BuildFailedException으로 취소)
 * 3. Scene/Asset Save - AssetModificationProcessor.OnWillSaveAssets (경로 필터링)
 *
 * 검증 범위 ::
 * 씬 경로(.unity) - 로드된 Scene의 모든 GameObject/MonoBehaviour를 reflection 순회
 * 에셋 경로(.asset/.prefab) - 메인 에셋 및 prefab 하위 MonoBehaviour를 reflection 순회
 * 필드 판정은 IHDictionary 인터페이스 할당 가능성으로 수행하여 제네릭 타입 파라미터에
 * 무관하게 동작한다.
 *
 * 로그 ::
 * Debug.LogError로 출력. 메시지는 씬/에셋 경로 + GameObject 계층 경로 + 필드명 +
 * 중복 개수를 포함하여 사용자가 문제 위치를 즉시 식별할 수 있도록 한다.
 *
 * 주의 ::
 * OnWillSaveAssets는 반환 배열에 포함되지 않은 경로를 저장하지 않는다. 즉 중복이 있는
 * 씬/에셋은 save 호출이 조용히 무시된다. 사용자가 "저장 안 됨"을 인지할 수 있도록
 * LogError로 명시적 에러 출력을 병행한다.
 * =========================================================
 */
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HCollection.Editor {
    #region Core Validator
    [InitializeOnLoad]
    internal static class HDictionaryValidator {
        #region Constants
        const string LOG_TAG = "[HDictionary]";
        const BindingFlags MEMBER_FLAGS =
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.DeclaredOnly;
        #endregion

        #region Static Init
        static HDictionaryValidator() {
            EditorApplication.playModeStateChanged += _OnPlayModeStateChanged;
        }
        #endregion

        #region Play Mode Block
        private static void _OnPlayModeStateChanged(PlayModeStateChange state) {
            if (state != PlayModeStateChange.ExitingEditMode) return;

            List<string> errors = new List<string>();
            ScanAllLoadedScenes(errors);

            if (errors.Count == 0) return;

            Debug.LogError($"{LOG_TAG} Play mode cancelled due to duplicate keys:\n  - " + string.Join("\n  - ", errors));
            EditorApplication.isPlaying = false;
        }
        #endregion

        #region Public Scan API
        public static void ScanAllLoadedScenes(List<string> errors) {
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++) {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                ScanScene(scene, errors);
            }
        }

        public static void ScanScene(Scene scene, List<string> errors) {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++) {
                _ScanGameObjectRecursive(roots[i], scene.name, errors);
            }
        }

        public static void ScanAssetAtPath(string assetPath, List<string> errors) {
            Object mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (mainAsset == null) return;

            if (mainAsset is GameObject prefabRoot) {
                _ScanGameObjectRecursive(prefabRoot, assetPath, errors);
                return;
            }

            ScanObject(mainAsset, assetPath, errors);
        }

        public static void ScanObject(Object target, string context, List<string> errors) {
            if (target == null) return;

            System.Type currentType = target.GetType();
            while (currentType != null && currentType != typeof(object)) {
                FieldInfo[] fields = currentType.GetFields(MEMBER_FLAGS);
                for (int i = 0; i < fields.Length; i++) {
                    if (!typeof(IHDictionary).IsAssignableFrom(fields[i].FieldType)) continue;

                    object rawValue = fields[i].GetValue(target);
                    IHDictionary dictionary = rawValue as IHDictionary;
                    if (dictionary == null) continue;
                    if (!dictionary.HasDuplicateKeys()) continue;

                    errors.Add($"{context}.{fields[i].Name} → {dictionary.DuplicateKeyCount()} duplicate key(s)");
                }
                currentType = currentType.BaseType;
            }
        }
        #endregion

        #region Internal Scan Helpers
        private static void _ScanGameObjectRecursive(GameObject root, string scopeContext, List<string> errors) {
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            for (int i = 0; i < behaviours.Length; i++) {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null) continue;
                string context = $"{scopeContext}:{_GetHierarchyPath(behaviour.gameObject)} ({behaviour.GetType().Name})";
                ScanObject(behaviour, context, errors);
            }
        }

        private static string _GetHierarchyPath(GameObject gameObject) {
            Transform transform = gameObject.transform;
            if (transform.parent == null) return gameObject.name;
            return _GetHierarchyPath(transform.parent.gameObject) + "/" + gameObject.name;
        }
        #endregion
    }
    #endregion

    #region Build Block
    // Preprocess는 currently-open 씬 검사만 수행 (early-fail 용도).
    // 전체 빌드 씬 리스트는 IProcessSceneWithReport가 각 씬이 로드될 때 개별 검사한다.
    internal class HDictionaryBuildPreprocessor : IPreprocessBuildWithReport {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) {
            List<string> errors = new List<string>();
            HDictionaryValidator.ScanAllLoadedScenes(errors);
            if (errors.Count == 0) return;

            string joined = "Build cancelled due to duplicate keys in HDictionary (currently open scene):\n  - "
                + string.Join("\n  - ", errors);
            Debug.LogError($"[HDictionary] {joined}");
            throw new BuildFailedException(joined);
        }
    }

    internal class HDictionaryBuildSceneProcessor : IProcessSceneWithReport {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report) {
            // report가 null이면 Play Mode 진입 중 스크립트 재컴파일 맥락일 수 있으므로
            // Build 전용 로직만 실행 (Play Mode는 별도 훅에서 처리)
            if (report == null) return;

            List<string> errors = new List<string>();
            HDictionaryValidator.ScanScene(scene, errors);
            if (errors.Count == 0) return;

            string joined = $"Build cancelled due to duplicate keys in scene '{scene.path}':\n  - "
                + string.Join("\n  - ", errors);
            Debug.LogError($"[HDictionary] {joined}");
            throw new BuildFailedException(joined);
        }
    }
    #endregion

    #region Save Block
    internal class HDictionarySaveProcessor : UnityEditor.AssetModificationProcessor {
        public static string[] OnWillSaveAssets(string[] paths) {
            if (paths == null || paths.Length == 0) return paths;

            List<string> allowed = new List<string>(paths.Length);
            for (int k = 0; k < paths.Length; k++) {
                string path = paths[k];
                if (_ShouldBlockSave(path, out string errorMessage)) {
                    Debug.LogError($"[HDictionary] Save blocked for '{path}':\n  - {errorMessage}");
                    continue;
                }
                allowed.Add(path);
            }
            return allowed.ToArray();
        }

        private static bool _ShouldBlockSave(string path, out string errorMessage) {
            errorMessage = null;
            if (string.IsNullOrEmpty(path)) return false;

            List<string> errors = new List<string>();

            if (path.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase)) {
                Scene scene = SceneManager.GetSceneByPath(path);
                if (scene.IsValid() && scene.isLoaded) {
                    HDictionaryValidator.ScanScene(scene, errors);
                }
            } else if (path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase)
                       || path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase)) {
                HDictionaryValidator.ScanAssetAtPath(path, errors);
            }

            if (errors.Count == 0) return false;

            errorMessage = string.Join("\n  - ", errors);
            return true;
        }
    }
    #endregion
}
#endif
