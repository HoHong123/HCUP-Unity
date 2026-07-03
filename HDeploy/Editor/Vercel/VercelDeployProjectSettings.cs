#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- Vercel 배포 프로젝트 공유 설정 클래스입니다.
 *
 * 특징 / 지원기능 ::
 * ProjectSettings/VercelDeployProjectSettings.asset에 저장되어 레포에 커밋된다(팀 공유).
 * + 브랜치 매핑(dev/release), 커밋 메시지 템플릿, 빌드 출력 상대경로, 서버 URL 2종
 *
 * 주의사항 ::
 * 머신 종속 값(배포 레포 절대경로 등)은 넣지 않는다 — VercelDeployUserSettings 담당.
 * 값 변경 후 SaveSettings()를 호출해야 파일에 기록된다.
 * =========================================================
 */
#endif

using UnityEditor;
using UnityEngine;

namespace HDeploy.Vercel {
    [FilePath(
        "ProjectSettings/VercelDeployProjectSettings.asset",
        FilePathAttribute.Location.ProjectFolder)]
    public sealed class VercelDeployProjectSettings : ScriptableSingleton<VercelDeployProjectSettings> {
        #region 변수
        public string DevBranchName = "dev";
        public string ReleaseBranchName = "main";

        // {version} = bundleVersion, {timestamp} = yyyy-MM-dd HH:mm, {devBranch}/{releaseBranch} = 브랜치명
        public string CommitMessageTemplate = "[Build] 🛠️ : WebGL 빌드 배포 v{version} ({timestamp})";
        public string PromoteCommitMessageTemplate = "[Build] 🛠️ : Release 배포 v{version} — {devBranch} → {releaseBranch} ({timestamp})";

        // 프로젝트 루트 기준 상대경로. 배포 시 내용 전체가 삭제 후 재생성된다.
        public string BuildOutputRelativePath = "Builds/VercelDeploy";

        // 배포 성공 후 "서버 열기" 버튼용. 비워두면 버튼 미표시.
        public string DevServerUrl = string.Empty;
        public string ReleaseServerUrl = string.Empty;
        #endregion

        #region 일반 함수
        public void SaveSettings() {
            Save(true);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.07.03 VercelDeployProjectSettings 베이스 코드 생성
 *
 * # 목적
 * - 프로젝트 간 이식 가능한 팀 공유 설정. ScriptableSingleton + FilePath(ProjectSettings)로
 *   에셋 폴더 오염 없이 커밋 가능한 위치에 저장.
 *
 * =============================================================================
 */
#endif
