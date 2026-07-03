#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- git 명령 1회 실행 결과 구조체입니다.
 *
 * 특징 / 지원기능 ::
 * 종료 코드·표준 출력·표준 에러·실행 명령을 불변으로 보관한다.
 * + IsSuccess : ExitCode == 0
 * + IsTimeout : 타임아웃 킬로 종료된 경우 true
 * =========================================================
 */
#endif

namespace HDeploy.Git {
    public readonly struct GitResult {
        #region 상수
        public const int TIMEOUT_EXIT_CODE = -1000;
        #endregion

        #region 변수
        public readonly int ExitCode;
        public readonly string StandardOutput;
        public readonly string StandardError;
        public readonly string Command;
        #endregion

        #region Getter/Setter
        public bool IsSuccess => ExitCode == 0;
        public bool IsTimeout => ExitCode == TIMEOUT_EXIT_CODE;
        #endregion

        #region 생성자 & 소멸자
        public GitResult(int exitCode, string standardOutput, string standardError, string command) {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
            Command = command;
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
 * @Jason - PKH 2026.07.03 GitResult 베이스 코드 생성
 *
 * # 목적
 * - GitCommandRunner의 실행 결과 전달용 불변 구조체.
 *
 * =============================================================================
 */
#endif
