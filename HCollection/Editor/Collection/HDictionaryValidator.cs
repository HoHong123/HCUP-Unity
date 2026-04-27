#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * HDictionary 의 중복 키를 하드 에러로 취급해 PlayMode 진입 / Build / 씬 저장 3 게이트를 차단.
 *
 * 차단 지점 ::
 * 1. Play Mode - EditorApplication.playModeStateChanged (ExitingEditMode 에서 취소).
 * 2. Build - IPreprocessBuildWithReport + IProcessSceneWithReport (BuildFailedException).
 * 3. Scene/Asset Save - AssetModificationProcessor.OnWillSaveAssets (경로 필터링).
 *
 * 검증 범위 ::
 * 씬 (.unity) 의 모든 GameObject/MonoBehaviour + 에셋 (.asset/.prefab) 의 메인/하위 MonoBehaviour 를
 * reflection 순회. 필드 판정은 IHDictionary.IsAssignableFrom 으로 제네릭 타입 파라미터와 무관.
 *
 * 로그 ::
 * Debug.LogError. 메시지에 씬/에셋 경로 + GameObject 계층 + 필드명 + 중복 개수 포함.
 *
 * 주의 ::
 * OnWillSaveAssets 는 반환 배열에 미포함 경로를 조용히 무시한다. 사용자가 "저장 안 됨" 을
 * 인지할 수 있도록 LogError 로 명시적 출력 병행.
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

        #region Public - Scan API
        public static void ScanAllLoadedScenes(List<string> errors) {
            int sceneCount = SceneManager.sceneCount;
            for (int k = 0; k < sceneCount; k++) {
                Scene scene = SceneManager.GetSceneAt(k);
                if (!scene.isLoaded) continue;
                ScanScene(scene, errors);
            }
        }

        public static void ScanScene(Scene scene, List<string> errors) {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int k = 0; k < roots.Length; k++) {
                _ScanGameObjectRecursive(roots[k], scene.name, errors);
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
                for (int k = 0; k < fields.Length; k++) {
                    if (!typeof(IHDictionary).IsAssignableFrom(fields[k].FieldType)) continue;

                    object rawValue = fields[k].GetValue(target);
                    IHDictionary dictionary = rawValue as IHDictionary;
                    if (dictionary == null) continue;
                    if (!dictionary.HasDuplicateKeys()) continue;

                    errors.Add($"{context}.{fields[k].Name} → {dictionary.DuplicateKeyCount()} duplicate key(s)");
                }
                currentType = currentType.BaseType;
            }
        }
        #endregion

        #region Private - Internal Scan Helpers
        private static void _ScanGameObjectRecursive(GameObject root, string scopeContext, List<string> errors) {
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            for (int k = 0; k < behaviours.Length; k++) {
                MonoBehaviour behaviour = behaviours[k];
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

    #region Internal - Build Block
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

    #region Internal - Save Block
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

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 *
 * =========================================================
 * 2026-04-26 (수정 2) :: 헤더 형틀 복원 + 헤더/Dev Log #if UNITY_EDITOR 가드 적용
 * =========================================================
 * 변경 ::
 * 1. 헤더 주석을 "도입 + 차단 지점 / 검증 범위 / 로그 / 주의" 5 섹션 형틀로 복원.
 *    각 섹션 내용은 압축.
 * 2. 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드로 감쌈 (이전 "수정 1" 에서 제거했던 가드 복원).
 *
 * 이유 ::
 * 직전 "수정 1" 이 헤더를 1~3 줄로 통째 압축해 형틀 (섹션 라벨) 자체를 손상시켰다.
 * reader 가 "이 시스템이 어떤 축으로 설명되는가" 를 섹션 라벨만으로 한눈에 파악할 수
 * 있도록 형틀을 보존하면서 각 섹션 내용만 압축하는 방향이 맞다. #if UNITY_EDITOR 가드는
 * IL 영향은 없지만 IDE 가 회색조로 표시해 "이 영역은 빌드에 안 들어간다" 를 reader 의
 * 시야에 미리 인식시킨다.
 *
 * =========================================================
 * 2026-04-26 (수정) :: 헤더 주석 간략화 + Dev Log 형식 도입
 * =========================================================
 * 변경 ::
 * 1. 헤더 주석을 5 줄 (구분선 제외) 로 압축. 기존 "차단 지점 / 검증 범위 / 로그 / 주의"
 *    4 섹션을 본 Dev Log 의 "2026-04-25 (최초 설계)" 엔트리로 이관.
 * 2. 헤더 주석 자체를 감쌌던 #if UNITY_EDITOR 가드 제거 (주석은 컴파일 산출물에 들어가지
 *    않으므로 가드 의미 없음). 클래스 본체의 #if UNITY_EDITOR 가드는 그대로 유지 (Editor 전용).
 *
 * 이유 ::
 * 헤더 주석이 너무 길어 파일 첫 화면에서 코드가 안 보였다. 핵심 의도만 헤더에 남기고
 * 자세한 자료는 Dev Log 영역으로 분리.
 *
 * =========================================================
 * 2026-04-25 (최초 설계) :: HDictionaryValidator 초기 구현
 * =========================================================
 * 차단 지점 ::
 * 1. Play Mode - EditorApplication.playModeStateChanged (ExitingEditMode 에서 취소)
 * 2. Build - IPreprocessBuildWithReport (BuildFailedException 으로 취소)
 *           + IProcessSceneWithReport (각 씬 로드 시점에 개별 검사)
 * 3. Scene/Asset Save - AssetModificationProcessor.OnWillSaveAssets (경로 필터링)
 *
 * 검증 범위 ::
 * - 씬 경로 (.unity) - 로드된 Scene 의 모든 GameObject/MonoBehaviour 를 reflection 순회
 * - 에셋 경로 (.asset/.prefab) - 메인 에셋 및 prefab 하위 MonoBehaviour 를 reflection 순회
 * - 필드 판정은 IHDictionary 인터페이스 할당 가능성 (IsAssignableFrom) 으로 수행하여
 *   제네릭 타입 파라미터에 무관하게 동작. 비제네릭 마커 인터페이스로 reflection 비용 절감.
 *
 * 로그 ::
 * Debug.LogError 로 출력. 메시지는 씬/에셋 경로 + GameObject 계층 경로 + 필드명 +
 * 중복 개수를 포함하여 사용자가 문제 위치를 즉시 식별할 수 있도록 한다.
 *
 * 주의 ::
 * OnWillSaveAssets 는 반환 배열에 포함되지 않은 경로를 저장하지 않는다. 즉 중복이 있는
 * 씬/에셋은 save 호출이 조용히 무시된다. 사용자가 "저장 안 됨" 을 인지할 수 있도록
 * LogError 로 명시적 에러 출력을 병행한다.
 *
 * IProcessSceneWithReport 의 report == null 분기는 Play Mode 진입 중 스크립트 재컴파일
 * 맥락일 수 있으므로 Build 전용 로직만 실행 (Play Mode 는 별도 훅에서 처리).
 * =========================================================
 */
#endif
