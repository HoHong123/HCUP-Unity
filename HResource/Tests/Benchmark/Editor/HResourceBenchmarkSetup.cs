#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * HResource 벤치마크 실행 전 준비와 실행 후 정리를 맡습니다.
 *
 * 특징 / 지원기능 ::
 * + Setup : 설정 JSON, 크기별 더미 에셋, key 용 더미 에셋, 임시 Addressables 그룹을 만들고 콘텐츠를 빌드합니다
 * + Cleanup : 임시 그룹과 임시 폴더를 지웁니다
 *
 * 주의사항 ::
 * 더미 에셋은 무작위 바이트입니다. 0 으로 채우면 번들 압축으로 크기가 사라져 크기 영향을 볼 수 없습니다.
 * 그룹은 에셋마다 번들 하나(PackSeparately)로 묶어 요청마다 실제 번들 로드가 일어나게 합니다.
 * 실행이 중간에 끊기면 Assets/HResourceBenchmarkTemp 와 HResourceBenchmark 그룹이 남습니다. 다음 실행이 덮어쓰고 정리합니다.
 * 플레이어 실행에서는 이 에디터 클래스를 찾지 못한다는 경고가 한 번 남습니다. 동작에는 영향이 없습니다.
 *
 * 사용 ::
 * HResourceBenchmarkTests 의 PrebuildSetup / PostBuildCleanup 이 이름으로 부릅니다. 직접 호출하지 않습니다.
 * =========================================================
 */
#endif

using System;
using System.IO;
using HDiagnosis.Logger;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.TestTools;

namespace HResource.Benchmark.Editor {
    public sealed class HResourceBenchmarkSetup : IPrebuildSetup, IPostBuildCleanup {
        #region 상수
        const string TEMP_ROOT = "Assets/HResourceBenchmarkTemp";
        const string PAYLOAD_FOLDER = TEMP_ROOT + "/Payloads";
        const string RESOURCES_FOLDER = TEMP_ROOT + "/Resources/HResourceBenchmark";
        const string CONFIG_FILE = RESOURCES_FOLDER + "/config.json";
        const string GROUP_NAME = "HResourceBenchmark";
        const string BYTES_EXTENSION = ".bytes";
        const int BYTES_PER_KB = 1024;
        const int RANDOM_SEED = 20260923;
        #endregion

        #region IPrebuildSetup
        public void Setup() {
            HResourceBenchmarkConfig config = HResourceBenchmarkSettingsSO.LoadConfig();
            config.Validate();

            _WriteConfig(config);
            _WritePayloads(config);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            _RegisterAddressables(config);
            _BuildAddressables();

            // 에디터 PlayMode 수치는 이 모드에 따라 번들 로드인지 AssetDatabase 직접 로드인지가 갈린다.
            string playModeBuilder = AddressableAssetSettingsDefaultObject.Settings.ActivePlayModeDataBuilder?.Name ?? "(none)";
            HLogger.Log("[HResourceBenchmark] Prepared payloads, keys and the '" + GROUP_NAME + "' Addressables group. " +
                "Editor play mode builder: " + playModeBuilder);
        }
        #endregion

        #region IPostBuildCleanup
        public void Cleanup() {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null) {
                AddressableAssetGroup group = settings.FindGroup(GROUP_NAME);
                if (group != null) settings.RemoveGroup(group);
            }

            AssetDatabase.DeleteAsset(TEMP_ROOT);
            AssetDatabase.SaveAssets();
            HLogger.Log("[HResourceBenchmark] Removed the temporary Addressables group and " + TEMP_ROOT + ".");
        }
        #endregion

        #region Private - Files
        static void _WriteConfig(HResourceBenchmarkConfig config) {
            Directory.CreateDirectory(RESOURCES_FOLDER);
            File.WriteAllText(CONFIG_FILE, JsonUtility.ToJson(config, true));
        }

        static void _WritePayloads(HResourceBenchmarkConfig config) {
            Directory.CreateDirectory(PAYLOAD_FOLDER);
            var random = new System.Random(RANDOM_SEED);

            for (int k = 0; k < config.PayloadSizesKB.Length; k++) {
                int sizeKB = config.PayloadSizesKB[k];
                _WriteRandomBytes(_PayloadPath(sizeKB), sizeKB, random);
            }

            int keyCount = config.MaxKeyCount();
            for (int k = 0; k < keyCount; k++) {
                _WriteRandomBytes(_KeyPath(k), HResourceBenchmarkConfig.KEY_PAYLOAD_KB, random);
            }
        }

        static void _WriteRandomBytes(string path, int sizeKB, System.Random random) {
            var bytes = new byte[sizeKB * BYTES_PER_KB];
            random.NextBytes(bytes);
            File.WriteAllBytes(path, bytes);
        }

        static string _PayloadPath(int sizeKB) => PAYLOAD_FOLDER + "/payload_" + sizeKB + "kb" + BYTES_EXTENSION;

        static string _KeyPath(int index) => PAYLOAD_FOLDER + "/key_" + index + BYTES_EXTENSION;
        #endregion

        #region Private - Addressables
        static void _RegisterAddressables(HResourceBenchmarkConfig config) {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) {
                throw new InvalidOperationException(
                    "[HResourceBenchmark] Addressables settings were not found. " +
                    "Create them from Window > Asset Management > Addressables > Groups before running the benchmark.");
            }

            AddressableAssetGroup group = settings.FindGroup(GROUP_NAME)
                ?? settings.CreateGroup(GROUP_NAME, false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            group.GetSchema<BundledAssetGroupSchema>().BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;

            for (int k = 0; k < config.PayloadSizesKB.Length; k++) {
                int sizeKB = config.PayloadSizesKB[k];
                _AddEntry(settings, group, _PayloadPath(sizeKB), HResourceBenchmarkConfig.PayloadAddress(sizeKB));
            }

            int keyCount = config.MaxKeyCount();
            for (int k = 0; k < keyCount; k++) {
                _AddEntry(settings, group, _KeyPath(k), HResourceBenchmarkConfig.KeyAddress(k));
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            AssetDatabase.SaveAssets();
        }

        static void _AddEntry(AddressableAssetSettings settings, AddressableAssetGroup group, string assetPath, string address) {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) {
                throw new InvalidOperationException(
                    "[HResourceBenchmark] '" + assetPath + "' was not imported. Check disk space and write access to " + TEMP_ROOT + ".");
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
        }

        static void _BuildAddressables() {
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (!string.IsNullOrEmpty(result.Error)) {
                throw new InvalidOperationException(
                    "[HResourceBenchmark] Addressables content build failed: " + result.Error + ". " +
                    "Fix the build error in the Addressables Groups window and run again.");
            }
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-23 (최초 설계) :: 벤치마크 준비와 정리
 *
 * 변경 ::
 * 실행 직전에 설정 JSON, 더미 에셋, 임시 Addressables 그룹을 만들고 콘텐츠를 빌드한다. 실행 뒤 그룹과 폴더를 지운다.
 *
 * 이유 ::
 * 벤치 전용 에셋과 그룹을 프로젝트에 상주시키지 않기 위해서다. 사용자 Addressables 설정에는 실행 동안만 그룹이 있다.
 *
 * 주의 ::
 * 정리 후에도 Library 의 Addressables 빌드 산출물에는 벤치 번들이 남는다. 다음 콘텐츠 빌드가 덮어쓴다.
 * 그룹 추가와 제거로 AddressableAssetSettings 에셋이 다시 직렬화되어 diff 가 생길 수 있다.
 * =========================================================
 */
#endif
