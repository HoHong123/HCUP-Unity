#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- Vercel 배포 머신 종속 설정 클래스입니다.
 *
 * 특징 / 지원기능 ::
 * UserSettings/VercelDeployUserSettings.asset에 저장된다(.gitignore 대상 — 미커밋).
 * + 배포 레포 절대경로, git 타임아웃(로컬/원격)
 *
 * 주의사항 ::
 * 팀 공유 값(브랜치명·템플릿 등)은 넣지 않는다 — VercelDeployProjectSettings 담당.
 * 값 변경 후 SaveSettings()를 호출해야 파일에 기록된다.
 * =========================================================
 */
#endif

using UnityEditor;

namespace HDeploy.Vercel {
    [FilePath("UserSettings/VercelDeployUserSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class VercelDeployUserSettings : ScriptableSingleton<VercelDeployUserSettings> {
        #region 변수
        // 배포 전용 레포(루트 = index.html + Build/)의 절대경로. 머신마다 다르므로 미커밋.
        public string DeployRepoAbsolutePath = string.Empty;

        public int LocalGitTimeoutSeconds = 15;
        public int RemoteGitTimeoutSeconds = 120;
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
 * @Jason - PKH 2026.07.03 VercelDeployUserSettings 베이스 코드 생성
 *
 * # 목적
 * - 머신 종속 설정 분리. UserSettings는 Unity 기본 .gitignore 대상이라 절대경로가 레포에 새지 않음.
 *
 * =============================================================================
 */
#endif
