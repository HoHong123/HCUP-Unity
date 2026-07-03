#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- git 명령 저수준 실행 유틸리티 클래스입니다.
 *
 * 특징 / 지원기능 ::
 * System.Diagnostics.Process로 git 1회 실행 후 GitResult를 반환한다.
 * + 표준 출력/에러 UTF-8 캡처, 표준 입력 즉시 닫기
 * + GIT_TERMINAL_PROMPT=0 — 자격 증명 프롬프트로 인한 무한 대기 차단
 * + 타임아웃 초과 시 프로세스 Kill 후 TIMEOUT_EXIT_CODE 반환
 *
 * 주의사항 ::
 * 결과의 성공 여부(IsSuccess)를 호출자가 반드시 검사한다. 조용한 무시 금지.
 * =========================================================
 */
#endif

using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace HDeploy.Git {
    public static class GitCommandRunner {
        #region 상수
        const string GIT_EXECUTABLE = "git";
        const string TERMINAL_PROMPT_VARIABLE = "GIT_TERMINAL_PROMPT";
        const string TERMINAL_PROMPT_DISABLED = "0";
        const int MILLISECONDS_PER_SECOND = 1000;
        #endregion

        #region 일반 함수
        /// <summary> git을 1회 실행하고 종료 코드·출력을 GitResult로 반환. 타임아웃 시 Kill 후 TIMEOUT_EXIT_CODE. </summary>
        public static async Task<GitResult> RunAsync(string workingDirectory, string arguments, int timeoutSeconds) {
            using Process process = new Process();
            process.StartInfo = _CreateStartInfo(workingDirectory, arguments);
            process.Start();
            process.StandardInput.Close();

            Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

            bool hasExited = await Task.Run(() => process.WaitForExit(timeoutSeconds * MILLISECONDS_PER_SECOND));
            if (hasExited == false) {
                process.Kill();
                return new GitResult(
                    GitResult.TIMEOUT_EXIT_CODE,
                    string.Empty,
                    $"git timed out after {timeoutSeconds}s. Check network or credential state, then retry.",
                    arguments);
            }

            string standardOutput = await standardOutputTask;
            string standardError = await standardErrorTask;
            return new GitResult(process.ExitCode, standardOutput.Trim(), standardError.Trim(), arguments);
        }

        private static ProcessStartInfo _CreateStartInfo(string workingDirectory, string arguments) {
            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = GIT_EXECUTABLE,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
            startInfo.EnvironmentVariables[TERMINAL_PROMPT_VARIABLE] = TERMINAL_PROMPT_DISABLED;
            return startInfo;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.07.03 Git 디렉토리 재배치
 *
 * # 코드 리펙토링
 * - Editor 루트 → Editor/Git/ 이동, namespace HDeploy → HDeploy.Git (디렉토리 기반 네임스페이스 컨벤션).
 *
 * =============================================================================
 * @Jason - PKH 2026.07.03 GitCommandRunner 베이스 코드 생성
 *
 * # 목적
 * - 배포 도구의 모든 git 호출 단일 통로. Process 실행·타임아웃·출력 캡처 담당.
 *
 * # 사용 흐름
 * - DeployRepoGitService가 RunAsync를 호출 → GitResult.IsSuccess 검사 후 시나리오 진행.
 *
 * =============================================================================
 */
#endif
