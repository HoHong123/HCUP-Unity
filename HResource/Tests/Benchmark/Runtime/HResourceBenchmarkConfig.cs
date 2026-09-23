#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * HResource 벤치마크 실행 설정입니다. 에디터 설정 에셋이 JSON 으로 내보내고 런타임이 읽습니다.
 *
 * 특징 / 지원기능 ::
 * 목표 fps(프레임 예산), 오브젝트 수 단계, 리소스 크기 단계, key 수 단계, 반복 횟수를 담습니다.
 * + 더미 에셋의 Addressables 주소 규칙도 이 파일 한 곳이 정합니다
 *
 * 주의사항 ::
 * 에디터와 플레이어가 같은 값을 쓰도록 Resources 의 임시 JSON 으로 전달합니다.
 * 그 JSON 은 실행 직전 HResourceBenchmarkSetup 이 만들고 실행 뒤 지웁니다.
 *
 * 사용 ::
 * HResourceBenchmarkConfig config = HResourceBenchmarkConfig.Load();
 * =========================================================
 */
#endif

using System;
using UnityEngine;

namespace HResource.Benchmark {
    [Serializable]
    public sealed class HResourceBenchmarkConfig {
        #region 상수
        public const string RESOURCES_PATH = "HResourceBenchmark/config";
        public const string ADDRESS_PREFIX = "hresource-benchmark/";
        // 다중 key 시나리오의 에셋 크기. 크기 영향은 payload 시나리오가 따로 본다.
        public const int KEY_PAYLOAD_KB = 1;
        public const int DEFAULT_TARGET_FRAME_RATE = 60;
        const double MILLISECONDS_PER_SECOND = 1000d;
        #endregion

        #region Fields
        public int TargetFrameRate = DEFAULT_TARGET_FRAME_RATE;
        public int[] OwnerCounts = { 100, 1000, 5000, 10000 };
        public int[] PayloadSizesKB = { 1, 256, 4096 };
        public int[] KeyCounts = { 10, 100, 500 };
        public int Repeat = 3;
        #endregion

        #region Properties
        public double FrameBudgetMs => MILLISECONDS_PER_SECOND / TargetFrameRate;
        #endregion

        #region Public - Address
        public static string PayloadAddress(int sizeKB) => ADDRESS_PREFIX + "payload_" + sizeKB + "kb";

        public static string KeyAddress(int index) => ADDRESS_PREFIX + "key_" + index;
        #endregion

        #region Public - Load
        public static HResourceBenchmarkConfig Load() {
            TextAsset json = Resources.Load<TextAsset>(RESOURCES_PATH);
            if (json == null) {
                throw new InvalidOperationException(
                    "[HResourceBenchmark] Config was not found in Resources/" + RESOURCES_PATH + ". " +
                    "Run the benchmark through the Test Runner so HResourceBenchmarkSetup can prepare it.");
            }

            HResourceBenchmarkConfig config = JsonUtility.FromJson<HResourceBenchmarkConfig>(json.text);
            config.Validate();
            return config;
        }
        #endregion

        #region Public - Validate
        public void Validate() {
            if (TargetFrameRate < 1) {
                throw new InvalidOperationException(
                    "[HResourceBenchmark] TargetFrameRate must be 1 or more. Fix it in the benchmark settings asset.");
            }
            if (Repeat < 1) {
                throw new InvalidOperationException(
                    "[HResourceBenchmark] Repeat must be 1 or more. Fix it in the benchmark settings asset.");
            }
            _RequirePositive(OwnerCounts, nameof(OwnerCounts));
            _RequirePositive(PayloadSizesKB, nameof(PayloadSizesKB));
            _RequirePositive(KeyCounts, nameof(KeyCounts));
        }

        public int MaxKeyCount() {
            int max = 0;
            for (int k = 0; k < KeyCounts.Length; k++) {
                if (KeyCounts[k] > max) max = KeyCounts[k];
            }
            return max;
        }
        #endregion

        #region Private - Validate
        static void _RequirePositive(int[] values, string fieldName) {
            if (values == null || values.Length < 1) {
                throw new InvalidOperationException(
                    "[HResourceBenchmark] " + fieldName + " is empty. Add at least one value in the benchmark settings asset.");
            }
            for (int k = 0; k < values.Length; k++) {
                if (values[k] > 0) continue;
                throw new InvalidOperationException(
                    "[HResourceBenchmark] " + fieldName + " has a value below 1 at index " + k + ". " +
                    "Use positive values in the benchmark settings asset.");
            }
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-23 (최초 설계) :: 벤치마크 설정 전달 형식
 *
 * 변경 ::
 * 에디터 설정 에셋과 런타임 벤치마크 사이의 값 전달 형식을 JSON 직렬화 클래스로 만들었다.
 *
 * 이유 ::
 * 같은 벤치를 에디터 PlayMode 와 플레이어 양쪽에서 돌린다. 플레이어는 에디터 에셋을 읽을 수 없으므로
 * 실행 직전에 Resources 로 내보내는 값 하나가 두 환경의 공통 입력이 된다.
 *
 * 주의 ::
 * 테스트 발견(discovery)은 준비 단계보다 먼저 일어나므로 NUnit 파라미터로 값을 풀지 않는다.
 * 각 테스트가 Load 한 뒤 안에서 단계를 돈다.
 * =========================================================
 */
#endif
