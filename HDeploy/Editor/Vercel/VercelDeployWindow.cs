#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- Vercel 배포 창(EditorWindow) 클래스입니다.
 *
 * 특징 / 지원기능 ::
 * 유니티 에디터 안에서 WebGL 빌드 → 배포 레포 push → Vercel 자동 배포를 수행한다.
 * + Dev Deploy : 빌드 → 산출물 교체 → dev 브랜치 push
 * + Release Deploy : 재빌드 없이 dev → release 승격(merge push)
 * + 버전(bundleVersion) 표시·수정, 설정 UI, 연결 테스트, 진행 로그 뷰
 * + 섹션 타이틀은 HInspector HTitleDrawer 시각 규격(볼드 라벨 + 1px 구분선)으로 통일
 *
 * 주의사항 ::
 * UI 표시·입력만 담당한다. 빌드/git 로직은 VercelDeployService 계층이 수행.
 * HCUP.HInspector.Editor 어셈블리를 참조한다 (HTitleDrawer).
 * =========================================================
 */
#endif

using System.Text.RegularExpressions;
using HDeploy.Deploy;
using HDeploy.Git;
using HInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HDeploy.Vercel {
    public sealed class VercelDeployWindow : EditorWindow {
        #region 상수
        const string MENU_PATH = "HCUP/Deployment/Vercel Deployment";
        const string WINDOW_TITLE = "Vercel Deploy";
        const float MIN_WINDOW_WIDTH = 420f;
        const float MIN_WINDOW_HEIGHT = 560f;
        const string REPO_PATH_PICKER_TITLE = "배포 레포 폴더 선택";
        const string SEMVER_PATTERN = @"^\d+\.\d+\.\d+$";
        const float SECTION_GAP = 8f;
        #endregion

        #region 변수
        readonly DeployLog log = new DeployLog();

        VercelDeployService deployService;
        bool isBusy;
        bool isProjectSettingsExpanded;
        string versionInput;
        Vector2 logScrollPosition;
        #endregion

        #region 유니티 라이프사이클 함수
        void OnEnable() {
            log.OnChanged += Repaint;
            deployService = new VercelDeployService(log);
            versionInput = PlayerSettings.bundleVersion;
        }

        void OnDisable() {
            log.OnChanged -= Repaint;
        }

        void OnGUI() {
            using (new EditorGUI.DisabledScope(isBusy)) {
                _DrawDeploySettings();
                EditorGUILayout.Space(SECTION_GAP);
                _DrawVersionField();
                EditorGUILayout.Space(SECTION_GAP);
                _DrawDeployButtons();
                _DrawConnectionTest();
            }
            EditorGUILayout.Space(SECTION_GAP);
            _DrawLogView();
        }
        #endregion

        #region 일반 함수 - 메뉴 / 그리기
        [MenuItem(MENU_PATH)]
        public static void Open() {
            VercelDeployWindow window = GetWindow<VercelDeployWindow>(WINDOW_TITLE);
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            window.Show();
        }

        private void _DrawDeploySettings() {
            HTitleDrawer.Draw("배포 설정");
            _DrawUserSettings();
            _DrawProjectSettings();
        }

        private void _DrawUserSettings() {
            VercelDeployUserSettings settings = VercelDeployUserSettings.instance;

            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUI.BeginChangeCheck();
                string repoPath = EditorGUILayout.TextField("배포 레포 경로 (미커밋)", settings.DeployRepoAbsolutePath);
                if (EditorGUI.EndChangeCheck()) {
                    settings.DeployRepoAbsolutePath = repoPath;
                    settings.SaveSettings();
                }

                if (GUILayout.Button("...", GUILayout.Width(28f))) _PickDeployRepoPath(settings);
            }
        }

        private void _DrawProjectSettings() {
            VercelDeployProjectSettings settings = VercelDeployProjectSettings.instance;

            isProjectSettingsExpanded = EditorGUILayout.Foldout(isProjectSettingsExpanded, "프로젝트 설정 (커밋됨)", true);
            if (isProjectSettingsExpanded == false) return;

            EditorGUI.BeginChangeCheck();
            settings.DevBranchName = EditorGUILayout.TextField("Dev 브랜치", settings.DevBranchName);
            settings.ReleaseBranchName = EditorGUILayout.TextField("Release 브랜치", settings.ReleaseBranchName);
            settings.CommitMessageTemplate = EditorGUILayout.TextField("Dev 커밋 템플릿", settings.CommitMessageTemplate);
            settings.PromoteCommitMessageTemplate = EditorGUILayout.TextField("승격 커밋 템플릿", settings.PromoteCommitMessageTemplate);
            settings.BuildOutputRelativePath = EditorGUILayout.TextField("빌드 출력 경로", settings.BuildOutputRelativePath);
            settings.DevServerUrl = EditorGUILayout.TextField("Dev 서버 URL", settings.DevServerUrl);
            settings.ReleaseServerUrl = EditorGUILayout.TextField("Release 서버 URL", settings.ReleaseServerUrl);
            if (EditorGUI.EndChangeCheck()) settings.SaveSettings();
        }

        private void _DrawVersionField() {
            HTitleDrawer.Draw("버전 (bundleVersion)");
            using (new EditorGUILayout.HorizontalScope()) {
                versionInput = EditorGUILayout.TextField("Version", versionInput);
                if (GUILayout.Button("적용", GUILayout.Width(50f))) _ApplyVersion();
            }

            bool isDirtyVersion = versionInput != PlayerSettings.bundleVersion;
            if (isDirtyVersion) {
                EditorGUILayout.HelpBox($"미적용 상태. 현재 PlayerSettings: {PlayerSettings.bundleVersion}", MessageType.Warning);
            }
        }

        private void _DrawDeployButtons() {
            VercelDeployProjectSettings settings = VercelDeployProjectSettings.instance;

            HTitleDrawer.Draw("배포");
            if (GUILayout.Button($"Dev Deploy  (빌드 → {settings.DevBranchName} push)", GUILayout.Height(32f))) {
                _RunDevDeployAsync();
            }
            if (GUILayout.Button($"Release Deploy  ({settings.DevBranchName} → {settings.ReleaseBranchName} 승격, 재빌드 없음)", GUILayout.Height(32f))) {
                _RunReleasePromoteAsync();
            }
        }

        private void _DrawConnectionTest() {
            if (GUILayout.Button("연결 테스트 (git status)")) _RunConnectionTestAsync();
        }

        private void _DrawLogView() {
            // HorizontalScope 안에서 HTitleDrawer.Draw의 내부 Space는 가로 여백으로 동작한다 —
            // 타이틀+구분선이 남은 폭을 채우고 Clear 버튼이 같은 행 우측에 배치된다.
            using (new EditorGUILayout.HorizontalScope()) {
                HTitleDrawer.Draw("Log");
                if (GUILayout.Button("Clear", GUILayout.Width(50f))) log.Clear();
            }

            logScrollPosition = EditorGUILayout.BeginScrollView(logScrollPosition, GUILayout.ExpandHeight(true));
            foreach (DeployLogEntry entry in log.Entries) {
                _DrawLogEntry(entry);
            }
            EditorGUILayout.EndScrollView();
        }

        private void _DrawLogEntry(DeployLogEntry entry) {
            Color original = GUI.contentColor;
            if (entry.Level == DeployLogSeverity.Error) GUI.contentColor = Color.red;
            if (entry.Level == DeployLogSeverity.Warning) GUI.contentColor = Color.yellow;

            EditorGUILayout.LabelField(entry.Message, EditorStyles.wordWrappedMiniLabel);
            GUI.contentColor = original;
        }
        #endregion

        #region 일반 함수 - 동작
        private void _PickDeployRepoPath(VercelDeployUserSettings settings) {
            string picked = EditorUtility.OpenFolderPanel(REPO_PATH_PICKER_TITLE, settings.DeployRepoAbsolutePath, string.Empty);
            if (string.IsNullOrEmpty(picked)) return;

            settings.DeployRepoAbsolutePath = picked;
            settings.SaveSettings();
        }

        private void _ApplyVersion() {
            if (Regex.IsMatch(versionInput, SEMVER_PATTERN) == false) {
                EditorUtility.DisplayDialog(WINDOW_TITLE, $"버전 형식이 잘못되었습니다: '{versionInput}'\nSemVer(major.minor.patch) 형식으로 입력하세요. 예: 0.2.9", "확인");
                return;
            }

            PlayerSettings.bundleVersion = versionInput;
            AssetDatabase.SaveAssets();
            log.Info($"bundleVersion applied :: {versionInput} (ProjectSettings 변경은 별도 커밋 필요)");
        }

        private async void _RunDevDeployAsync() {
            if (isBusy) return;

            VercelDeployProjectSettings settings = VercelDeployProjectSettings.instance;
            bool isConfirmed = EditorUtility.DisplayDialog(
                WINDOW_TITLE,
                $"Dev 배포를 시작할까요?\n\n버전: {PlayerSettings.bundleVersion}\n대상 브랜치: {settings.DevBranchName}\nURL: {settings.DevServerUrl}\n\n빌드 동안 에디터가 응답 없음 상태가 됩니다.",
                "배포", "취소");
            if (isConfirmed == false) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false) return;

            isBusy = true;
            try {
                bool isSuccess = await deployService.DeployDevAsync();
                if (isSuccess) _OfferOpenUrl(settings.DevServerUrl, "Dev 서버");
            }
            finally {
                isBusy = false;
                Repaint();
            }
        }

        private async void _RunReleasePromoteAsync() {
            if (isBusy) return;

            VercelDeployProjectSettings settings = VercelDeployProjectSettings.instance;
            bool isConfirmed = EditorUtility.DisplayDialog(
                WINDOW_TITLE,
                $"Release 승격을 시작할까요?\n\norigin/{settings.DevBranchName}의 검증된 빌드를 {settings.ReleaseBranchName}으로 merge-push합니다 (재빌드 없음).\nURL: {settings.ReleaseServerUrl}",
                "승격", "취소");
            if (isConfirmed == false) return;

            isBusy = true;
            try {
                bool isSuccess = await deployService.PromoteReleaseAsync();
                if (isSuccess) _OfferOpenUrl(settings.ReleaseServerUrl, "Release 서버");
            }
            finally {
                isBusy = false;
                Repaint();
            }
        }

        private async void _RunConnectionTestAsync() {
            if (isBusy) return;

            isBusy = true;
            try {
                VercelDeployUserSettings userSettings = VercelDeployUserSettings.instance;
                string repoPath = userSettings.DeployRepoAbsolutePath;
                if (string.IsNullOrEmpty(repoPath)) {
                    log.Error("Deploy repo path is empty. Set the path first.");
                    return;
                }

                log.Info($"Connection test start :: {repoPath}");

                GitResult branchResult = await GitCommandRunner.RunAsync(repoPath, "rev-parse --abbrev-ref HEAD", userSettings.LocalGitTimeoutSeconds);
                if (branchResult.IsSuccess == false) {
                    log.Error($"git rev-parse failed (exit {branchResult.ExitCode}) :: {branchResult.StandardError}");
                    return;
                }
                log.Info($"Current branch :: {branchResult.StandardOutput}");

                GitResult statusResult = await GitCommandRunner.RunAsync(repoPath, "status --porcelain", userSettings.LocalGitTimeoutSeconds);
                if (statusResult.IsSuccess == false) {
                    log.Error($"git status failed (exit {statusResult.ExitCode}) :: {statusResult.StandardError}");
                    return;
                }

                bool isClean = string.IsNullOrEmpty(statusResult.StandardOutput);
                if (isClean) log.Info("Working tree :: clean");
                else log.Warning($"Working tree :: dirty\n{statusResult.StandardOutput}");

                log.Info("Connection test done.");
            }
            finally {
                isBusy = false;
                Repaint();
            }
        }

        private void _OfferOpenUrl(string url, string serverLabel) {
            if (string.IsNullOrEmpty(url)) return;

            bool isOpenRequested = EditorUtility.DisplayDialog(WINDOW_TITLE, $"배포 완료. {serverLabel}를 브라우저로 열까요?\n{url}", "열기", "닫기");
            if (isOpenRequested) Application.OpenURL(url);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.07.04 HTitle 시각 규격 적용
 *
 * # 변경
 * - 섹션 헤더(boldLabel)를 HTitleDrawer.Draw()로 대체 :: 배포 설정 / 버전 / 배포 / Log 4개 타이틀 블록.
 * - 머신 설정·프로젝트 설정을 "배포 설정" 타이틀 아래로 통합 ("미커밋" 표기는 레포 경로 필드 라벨로 이동).
 * - 연결 테스트 버튼을 "배포" 섹션에 편입.
 * - 섹션 간 여백을 SECTION_GAP(8f) 상수로 통일 (HTitleDrawer 내부 상단 패딩 6f와 합산됨).
 * - asmdef :: HCUP.HInspector.Editor 참조 추가 — HDeploy → HInspector 패키지 의존 발생 (사용자 승인).
 *
 * =============================================================================
 * @Jason - PKH 2026.07.03 메뉴 경로 이동
 *
 * # 변경
 * - MENU_PATH : Tools/HDeploy/Vercel Deploy → HCUP/Deployment/Vercel Deployment
 *   (HCUP 메뉴 트리로 통합 — HCUP/Windows/Data Editor Window 와 동일 루트)
 *
 * =============================================================================
 * @Jason - PKH 2026.07.03 배포 버튼·버전 필드 연결
 *
 * # 추가
 * - Dev Deploy / Release Deploy 버튼 : 확인 다이얼로그(버전·브랜치·URL·블로킹 고지) → 씬 저장 → 서비스 호출.
 * - 버전 필드 : bundleVersion 표시·SemVer 검증 후 적용. 미적용 상태 HelpBox 경고.
 * - 성공 시 서버 URL 열기 제안 다이얼로그.
 *
 * =============================================================================
 * @Jason - PKH 2026.07.03 설정 UI·연결 테스트 추가
 *
 * # 추가
 * - 머신 설정(레포 경로)·프로젝트 설정(브랜치/템플릿/URL) 편집 UI. 변경 즉시 SaveSettings.
 * - 연결 테스트 버튼 : rev-parse(현재 브랜치) + status --porcelain(클린 여부) 로그 출력.
 * - isBusy 재진입 방지 + 실행 중 입력 비활성.
 *
 * =============================================================================
 * @Jason - PKH 2026.07.03 VercelDeployWindow 베이스 코드 생성
 *
 * # 목적
 * - Vercel 배포 창 스켈레톤. 메뉴 등록 + 로그 뷰만 우선 구성.
 *
 * # 사용 흐름
 * - Tools/HDeploy/Vercel Deploy 메뉴로 열기. 이후 단계에서 설정 UI·배포 버튼 연결 예정.
 *
 * =============================================================================
 */
#endif
