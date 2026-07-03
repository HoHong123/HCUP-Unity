#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 배포 로그 단일 엔트리 구조체입니다.
 * =========================================================
 */
#endif

namespace HDeploy.Deploy {
    public readonly struct DeployLogEntry {
        public readonly DeployLogSeverity Level;
        public readonly string Message;

        public DeployLogEntry(DeployLogSeverity level, string message) {
            Level = level;
            Message = message;
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.07.03 Deploy 디렉토리 재배치
 *
 * # 코드 리펙토링
 * - Editor 루트 → Editor/Deploy/ 이동, namespace HDeploy → HDeploy.Deploy (디렉토리 기반 네임스페이스 컨벤션).
 *
 * =============================================================================
 * @Jason - PKH 2026.07.03 DeployLogEntry 베이스 코드 생성
 *
 * # 목적
 * - DeployLog가 축적하는 불변 로그 엔트리. (DeployLog 중첩 타입에서 파일 분리 — 컨벤션 준수)
 *
 * =============================================================================
 */
#endif
