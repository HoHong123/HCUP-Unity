#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 벤치마크를 플레이어에서 돌릴 때 빌드와 실행을 나눕니다.
 *
 * 특징 / 지원기능 ::
 * 명령줄에 -hresourceBenchmarkPlayerPath <폴더> 가 있을 때만 동작합니다.
 * + 그 폴더에 개발 빌드로 만들고 자동 실행, 에디터 연결, 스크립트 디버깅을 끕니다
 * + 테스트 씬만 넣습니다. 게임 씬은 빌드하지 않습니다
 * + -hresourceBenchmarkNonDevelopment 를 함께 주면 개발 빌드를 끄고 릴리스 코드 생성으로 만듭니다. GC 레코더가 무효일 수 있습니다
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
        // 개발 빌드는 스크립트를 디버그 코드 생성으로 컴파일한다(2026-09-23 측정). 릴리스 코드 생성으로 재려면 이 인자를 준다.
        const string NON_DEVELOPMENT_ARGUMENT = "-hresourceBenchmarkNonDevelopment";
        #endregion

        #region ITestPlayerBuildModifier
        public BuildPlayerOptions ModifyOptions(BuildPlayerOptions playerOptions) {
            if (!_TryGetPlayerFolder(out string folder)) return playerOptions;

            if (_HasArgument(NON_DEVELOPMENT_ARGUMENT)) playerOptions.options &= ~BuildOptions.Development;
            else playerOptions.options |= BuildOptions.Development;
            // 디버거 연결 대기와 관리 디버거를 뺀다. 이것만으로는 릴리스 코드 생성이 되지 않는다(개발 빌드는 여전히 디버그 코드 생성).
            playerOptions.options &= ~(BuildOptions.AutoRunPlayer | BuildOptions.ConnectToHost | BuildOptions.WaitForPlayerConnection | BuildOptions.ConnectWithProfiler | BuildOptions.AllowDebugging);
            playerOptions.locationPathName = Path.Combine(folder, PLAYER_EXECUTABLE);

            // 테스트 러너는 자기 테스트 씬을 맨 앞에 두고 게임 씬을 전부 덧붙인다. 벤치는 테스트 씬만 쓴다.
            // 게임 씬을 넣으면 작업본에만 있는 에셋(누락 프리팹, 폰트)이 빌드를 깨뜨릴 수 있다.
            if (playerOptions.scenes != null && playerOptions.scenes.Length > 1) {
                playerOptions.scenes = new[] { playerOptions.scenes[0] };
            }

            HLogger.Log("[HResourceBenchmark] Building the benchmark player to " + playerOptions.locationPathName + " without running it.");
            return playerOptions;
        }
        #endregion

        #region Private
        static bool _HasArgument(string argument) {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int k = 0; k < arguments.Length; k++) {
                if (string.Equals(arguments[k], argument, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

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
 * 2026-09-23 (수정 2) :: 스크립트 디버깅 주석 정정
 *
 * 변경 ::
 * AllowDebugging 을 끄는 줄의 주석을 고쳤다. 끄면 릴리스와 같은 코드 생성이 된다고 적혀 있었다.
 *
 * 이유 ::
 * 같은 날 보고서의 코드 생성 기록으로 개발 빌드가 스크립트 디버깅을 꺼도 디버그 코드 생성임을 확인했다. 주석이 그 결과와 반대였다.
 *
 * 결과 ::
 * 주석, 헤더, Dev Log 가 같은 사실을 말한다. 빌드 설정은 그대로다.
 *
 * 주의 ::
 * 릴리스 코드 생성 수치는 -hresourceBenchmarkNonDevelopment 로만 얻는다.
 *
 * =========================================================
 * 2026-09-23 (수정) :: 테스트 씬만 빌드, 스크립트 디버깅 끔, 비개발 빌드 옵션
 *
 * 변경 ::
 * 테스트 러너가 덧붙이는 게임 씬을 빼고 테스트 씬만 넣는다. AllowDebugging 을 끈다.
 * -hresourceBenchmarkNonDevelopment 가 있으면 Development 도 끈다.
 *
 * 이유 ::
 * 게임 씬이 작업본에만 있는 에셋(누락 프리팹, TMP 폰트)을 참조해 빌드가 깨졌다. 벤치는 게임 씬을 쓰지 않는다.
 * 스크립트 디버깅을 꺼도 개발 빌드는 디버그 코드 생성이었다. 릴리스 코드 생성 수치는 비개발 빌드에서만 나온다.
 *
 * 주의 ::
 * 비개발 빌드에서는 GC 레코더가 무효일 수 있다. 그때 GC 열은 "-" 로 남는다.
 *
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
