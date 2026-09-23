#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 벤치마크를 플레이어에서 돌릴 때 빌드와 실행을 나눕니다.
 *
 * 특징 / 지원기능 ::
 * 명령줄에 -hresourceBenchmarkPlayerPath <폴더> 가 있을 때만 동작합니다.
 * + 그 폴더에 개발 빌드로 만들고 자동 실행과 에디터 연결을 끕니다
 *
 * 주의사항 ::
 * 인자가 없으면 빌드 옵션을 건드리지 않습니다. Test Runner 의 Run on player 는 평소대로 돕니다.
 * 나눠 만든 플레이어는 결과를 에디터로 보내지 않습니다. 벤치 보고서는 플레이어의 persistentDataPath 에 남습니다.
 *
 * 사용 ::
 * Unity.exe -batchmode -projectPath <프로젝트> -runTests -testPlatform StandaloneWindows64
 *   -testFilter HResource.Benchmark -hresourceBenchmarkPlayerPath <폴더>
 * 그 뒤 <폴더>/HResourceBenchmark.exe -batchmode -nographics -hresourceBenchmarkQuitWhenDone 로 실행합니다.
 * 마지막 인자가 없으면 플레이어가 테스트 뒤에도 남고, 단일 인스턴스 설정 때문에 다음 실행이 막힙니다.
 * =========================================================
 */
#endif

using System;
using System.IO;
using HDiagnosis.Logger;
using HResource.Benchmark.Editor;
using UnityEditor;
using UnityEditor.TestTools;

[assembly: TestPlayerBuildModifier(typeof(HResourceBenchmarkPlayerBuildModifier))]

namespace HResource.Benchmark.Editor {
    public sealed class HResourceBenchmarkPlayerBuildModifier : ITestPlayerBuildModifier {
        #region 상수
        const string PLAYER_PATH_ARGUMENT = "-hresourceBenchmarkPlayerPath";
        const string PLAYER_EXECUTABLE = "HResourceBenchmark.exe";
        #endregion

        #region ITestPlayerBuildModifier
        public BuildPlayerOptions ModifyOptions(BuildPlayerOptions playerOptions) {
            if (!_TryGetPlayerFolder(out string folder)) return playerOptions;

            playerOptions.options |= BuildOptions.Development;
            playerOptions.options &= ~(BuildOptions.AutoRunPlayer | BuildOptions.ConnectToHost | BuildOptions.WaitForPlayerConnection | BuildOptions.ConnectWithProfiler);
            playerOptions.locationPathName = Path.Combine(folder, PLAYER_EXECUTABLE);

            HLogger.Log("[HResourceBenchmark] Building the benchmark player to " + playerOptions.locationPathName + " without running it.");
            return playerOptions;
        }
        #endregion

        #region Private
        static bool _TryGetPlayerFolder(out string folder) {
            folder = null;
            string[] arguments = Environment.GetCommandLineArgs();
            for (int k = 0; k < arguments.Length - 1; k++) {
                if (!string.Equals(arguments[k], PLAYER_PATH_ARGUMENT, StringComparison.OrdinalIgnoreCase)) continue;

                folder = arguments[k + 1];
                return !string.IsNullOrWhiteSpace(folder);
            }
            return false;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-23 (최초 설계) :: 벤치 플레이어 빌드와 실행 분리
 *
 * 변경 ::
 * 명령줄 인자가 있을 때만 테스트 플레이어를 지정 폴더에 개발 빌드로 만들고 자동 실행하지 않게 했다.
 *
 * 이유 ::
 * 기본 흐름은 빌드 직후 플레이어를 띄운다. 이 프로젝트의 플레이어 설정은 전체 화면이라 사용자 화면을 덮는다.
 * 나눠 두면 -batchmode -nographics 로 창 없이 실행해 CPU 비용만 잴 수 있다.
 *
 * 주의 ::
 * 어셈블리 특성이라 이 어셈블리가 컴파일되는 동안 모든 플레이어 테스트 빌드가 이 클래스를 거친다.
 * 그래서 인자가 없을 때는 옵션을 그대로 돌려준다.
 * =========================================================
 */
#endif
