#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * HResource 벤치마크의 사용자 입력값을 담는 설정 에셋입니다.
 *
 * 특징 / 지원기능 ::
 * 목표 fps(기본 60), 오브젝트 수 단계, 리소스 크기(KB) 단계, key 수 단계, 반복 횟수를 인스펙터에서 고칩니다.
 * + 메뉴 HCUP / Resource / Benchmark Settings 가 에셋을 찾거나 이 스크립트 옆에 만듭니다
 *
 * 주의사항 ::
 * 에셋이 없으면 HResourceBenchmarkConfig 의 기본값으로 돕니다.
 * 리소스 크기 단계마다 그 크기의 더미 에셋이 하나씩 만들어집니다. 큰 값은 준비 시간과 디스크를 씁니다.
 *
 * 사용 ::
 * 메뉴로 에셋을 열고 값을 고친 뒤 Test Runner 에서 HResourceBenchmarkTests 를 실행합니다.
 * =========================================================
 */
#endif

using System.IO;
using HDiagnosis.Logger;
using UnityEditor;
using UnityEngine;

namespace HResource.Benchmark.Editor {
    public sealed class HResourceBenchmarkSettingsSO : ScriptableObject {
        #region 상수
        const string ASSET_NAME = "HResourceBenchmarkSettings.asset";
        const string MENU_PATH = "HCUP/Resource/Benchmark Settings";
        #endregion

        #region Fields
        [Tooltip("과부하 판정 기준 fps. 예산 = 1000 / fps (ms)")]
        [Min(1)]
        [SerializeField]
        int targetFrameRate = HResourceBenchmarkConfig.DEFAULT_TARGET_FRAME_RATE;

        [Tooltip("BurstLifecycle 에서 한 프레임에 요청하는 오브젝트 수 단계")]
        [SerializeField]
        int[] ownerCounts = { 100, 1000, 5000, 10000 };

        [Tooltip("요청하는 리소스 크기(KB) 단계. 크기마다 더미 에셋 하나를 만든다")]
        [SerializeField]
        int[] payloadSizesKB = { 1, 256, 4096 };

        [Tooltip("MultiKey 에서 오브젝트 하나가 요청하는 서로 다른 key 수 단계")]
        [SerializeField]
        int[] keyCounts = { 10, 100, 500 };

        [Tooltip("같은 조건의 반복 횟수. 보고서는 중앙값을 쓴다")]
        [Min(1)]
        [SerializeField]
        int repeat = 3;
        #endregion

        #region Public - Config
        public HResourceBenchmarkConfig ToConfig() {
            return new HResourceBenchmarkConfig {
                TargetFrameRate = targetFrameRate,
                OwnerCounts = ownerCounts,
                PayloadSizesKB = payloadSizesKB,
                KeyCounts = keyCounts,
                Repeat = repeat,
            };
        }

        /// <summary> 프로젝트의 설정 에셋 값, 없으면 기본값 </summary>
        public static HResourceBenchmarkConfig LoadConfig() {
            HResourceBenchmarkSettingsSO settings = FindOrNull();
            if (settings != null) return settings.ToConfig();

            HLogger.Warning(
                "[HResourceBenchmark] No settings asset found. Running with defaults. " +
                "Create one from the menu " + MENU_PATH + " to change the frame budget or payload sizes.");
            return new HResourceBenchmarkConfig();
        }

        public static HResourceBenchmarkSettingsSO FindOrNull() {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(HResourceBenchmarkSettingsSO));
            if (guids.Length < 1) return null;
            if (guids.Length > 1) {
                HLogger.Warning(
                    "[HResourceBenchmark] Found " + guids.Length + " settings assets. Using the first one. " +
                    "Delete the extra assets so the benchmark reads a single source.");
            }
            return AssetDatabase.LoadAssetAtPath<HResourceBenchmarkSettingsSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        #endregion

        #region Menu
        [MenuItem(MENU_PATH)]
        static void _SelectOrCreate() {
            HResourceBenchmarkSettingsSO settings = FindOrNull();
            if (settings == null) settings = _CreateNextToScript();

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        static HResourceBenchmarkSettingsSO _CreateNextToScript() {
            HResourceBenchmarkSettingsSO settings = CreateInstance<HResourceBenchmarkSettingsSO>();
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(settings));
            string folder = Path.GetDirectoryName(Path.GetDirectoryName(scriptPath)).Replace('\\', '/');

            AssetDatabase.CreateAsset(settings, folder + "/" + ASSET_NAME);
            AssetDatabase.SaveAssets();
            return settings;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-23 (최초 설계) :: 벤치마크 입력 에셋
 *
 * 변경 ::
 * fps 예산과 리소스 크기 등 사용자 입력을 인스펙터에서 받는 설정 에셋을 만들었다.
 *
 * 이유 ::
 * 한계는 프레임 예산과 리소스 크기에 따라 달라진다. 코드를 고치지 않고 조건을 바꿔 다시 잴 수 있어야 한다.
 *
 * 주의 ::
 * 메뉴로 만든 에셋은 Benchmark 폴더(이 스크립트의 상위 폴더)에 생긴다. 프로젝트마다 하나만 둔다.
 * =========================================================
 */
#endif
