#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- Vercel 배포 시퀀스 오케스트레이션 클래스입니다.
 *
 * 특징 / 지원기능 ::
 * Dev/Release 배포의 전체 시퀀스를 조립한다. 창은 본 클래스의 진입점 2개만 호출.
 * + DeployDevAsync : preflight → WebGL 빌드 → 산출물 교체 → 커밋 → dev push
 * + PromoteReleaseAsync : 재빌드 없이 origin/dev → release 브랜치 merge(--no-ff) push
 * + 커밋 메시지 템플릿 치환({version}/{timestamp}/{devBranch}/{releaseBranch})
 *
 * 주의사항 ::
 * DeployDevAsync는 내부에서 BuildPlayer(동기)를 호출 — 메인 스레드에서만 호출할 것.
 * Play 모드/컴파일 중에는 시작을 거부한다.
 * =========================================================
 */
#endif

using System;
using System.IO;
using System.Threading.Tasks;
using HDeploy.Deploy;
using HDeploy.Git;
using UnityEditor;

namespace HDeploy.Vercel {
    public sealed class VercelDeployService {
        #region 상수
        const string TIMESTAMP_FORMAT = "yyyy-MM-dd HH:mm";
        const string VERSION_PLACEHOLDER = "{version}";
        const string TIMESTAMP_PLACEHOLDER = "{timestamp}";
        const string DEV_BRANCH_PLACEHOLDER = "{devBranch}";
        const string RELEASE_BRANCH_PLACEHOLDER = "{releaseBranch}";
        #endregion

        #region 변수
        readonly DeployLog log;
        #endregion

        #region 생성자 & 소멸자
        public VercelDeployService(DeployLog log) {
            this.log = log;
        }
        #endregion

        #region 일반 함수 - 진입점
        /// <summary> Dev 배포 : preflight → 빌드 → 산출물 교체 → 커밋 → dev push. </summary>
        public async Task<bool> DeployDevAsync() {
            if (_IsEditorReady() == false) return false;
            if (_TryCreateGitService(out DeployRepoGitService gitService) == false) return false;

            VercelDeployProjectSettings settings = VercelDeployProjectSettings.instance;
            log.Info($"=== Dev deploy start (branch: {settings.DevBranchName}) ===");

            if (await gitService.RunPreflightAsync(settings.DevBranchName) == false) return false;

            string buildOutputPath = Path.Combine(Directory.GetCurrentDirectory(), settings.BuildOutputRelativePath);
            if (WebGLBuildService.BuildAndValidate(buildOutputPath, log) == false) return false;

            if (gitService.ReplaceArtifacts(buildOutputPath) == false) {
                await gitService.RestoreArtifactsAsync();
                return false;
            }

            bool? hasChanges = await gitService.StageArtifactsAsync();
            if (hasChanges == null) {
                await gitService.RestoreArtifactsAsync();
                return false;
            }
            if (hasChanges == false) {
                log.Info("No changes against the deployed build. Nothing to push.");
                return true;
            }

            string commitMessage = _BuildMessage(settings.CommitMessageTemplate, settings);
            if (await gitService.CommitAsync(commitMessage) == false) {
                await gitService.RestoreArtifactsAsync();
                return false;
            }

            // 커밋 이후 push 실패는 원복하지 않는다 — 로컬 커밋을 보존하고 수동 재시도를 안내.
            if (await gitService.PushAsync(settings.DevBranchName) == false) {
                log.Warning("Commit is kept locally. Fix the push issue and push manually, or rerun Dev deploy.");
                return false;
            }

            log.Info($"=== Dev deploy done. Vercel will auto-deploy origin/{settings.DevBranchName}. ===");
            return true;
        }

        /// <summary> Release 승격 : 재빌드 없이 origin/{dev}를 {release}에 merge(--no-ff) 후 push. </summary>
        public async Task<bool> PromoteReleaseAsync() {
            if (_IsEditorReady() == false) return false;
            if (_TryCreateGitService(out DeployRepoGitService gitService) == false) return false;

            VercelDeployProjectSettings settings = VercelDeployProjectSettings.instance;
            log.Info($"=== Release promote start ({settings.DevBranchName} → {settings.ReleaseBranchName}) ===");

            if (await gitService.ValidateRepoAsync() == false) return false;
            if (await gitService.IsWorkingTreeCleanAsync() == false) return false;
            if (await gitService.FetchAsync() == false) return false;

            int? promotableCount = await gitService.CountPromotableCommitsAsync(settings.DevBranchName, settings.ReleaseBranchName);
            if (promotableCount == null) return false;
            if (promotableCount == 0) {
                log.Info("Nothing to promote. Release is already up to date.");
                return true;
            }

            string latestSubject = await gitService.GetRemoteLatestSubjectAsync(settings.DevBranchName);
            log.Info($"Promoting {promotableCount} commit(s). Latest :: {latestSubject}");

            if (await gitService.CheckoutAsync(settings.ReleaseBranchName) == false) return false;
            if (await gitService.PullFastForwardAsync(settings.ReleaseBranchName) == false) {
                await gitService.CheckoutAsync(settings.DevBranchName);
                return false;
            }

            string mergeMessage = _BuildMessage(settings.PromoteCommitMessageTemplate, settings);
            if (await gitService.MergeNoFastForwardAsync(settings.DevBranchName, mergeMessage) == false) {
                await gitService.CheckoutAsync(settings.DevBranchName);
                return false;
            }

            if (await gitService.PushAsync(settings.ReleaseBranchName) == false) {
                log.Warning($"Merge commit is kept locally on {settings.ReleaseBranchName}. Fix the push issue and push manually.");
                await gitService.CheckoutAsync(settings.DevBranchName);
                return false;
            }

            // 평시 브랜치(dev)로 복귀 — 다음 Dev 배포의 checkout 단계 단축.
            await gitService.CheckoutAsync(settings.DevBranchName);

            log.Info($"=== Release promote done. Vercel will auto-deploy origin/{settings.ReleaseBranchName}. ===");
            return true;
        }
        #endregion

        #region 일반 함수 - 내부
        private bool _IsEditorReady() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                log.Error("Cannot deploy while in Play mode. Stop Play mode first.");
                return false;
            }
            if (EditorApplication.isCompiling) {
                log.Error("Cannot deploy while compiling. Wait for compilation to finish.");
                return false;
            }
            return true;
        }

        private bool _TryCreateGitService(out DeployRepoGitService gitService) {
            VercelDeployUserSettings userSettings = VercelDeployUserSettings.instance;
            VercelDeployProjectSettings projectSettings = VercelDeployProjectSettings.instance;

            bool hasRepoPath = string.IsNullOrEmpty(userSettings.DeployRepoAbsolutePath) == false;
            bool hasBranchNames = string.IsNullOrEmpty(projectSettings.DevBranchName) == false
                && string.IsNullOrEmpty(projectSettings.ReleaseBranchName) == false;
            if (hasRepoPath == false || hasBranchNames == false) {
                log.Error("Settings incomplete. Set deploy repo path and branch names first.");
                gitService = null;
                return false;
            }

            gitService = new DeployRepoGitService(
                userSettings.DeployRepoAbsolutePath,
                userSettings.LocalGitTimeoutSeconds,
                userSettings.RemoteGitTimeoutSeconds,
                log);
            return true;
        }

        private static string _BuildMessage(string template, VercelDeployProjectSettings settings) {
            return template
                .Replace(VERSION_PLACEHOLDER, PlayerSettings.bundleVersion)
                .Replace(TIMESTAMP_PLACEHOLDER, DateTime.Now.ToString(TIMESTAMP_FORMAT))
                .Replace(DEV_BRANCH_PLACEHOLDER, settings.DevBranchName)
                .Replace(RELEASE_BRANCH_PLACEHOLDER, settings.ReleaseBranchName);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.07.03 VercelDeployService 베이스 코드 생성
 *
 * # 목적
 * - Dev 배포·Release 승격의 전체 시퀀스 조립. 창(UI)과 저수준 서비스(빌드/git) 사이의 유일한 통로.
 *
 * # 사용 흐름
 * - VercelDeployWindow 버튼 → DeployDevAsync / PromoteReleaseAsync → DeployLog로 진행 보고.
 *
 * # 설계 결정
 * - 커밋 이후 push 실패는 원복하지 않음 — 로컬 커밋 보존이 재시도에 안전 (reset --hard 자동 실행 배제).
 * - Release 승격은 재빌드 없음 — Dev에서 검증된 바이너리가 그대로 승격 (CD 정합성).
 *
 * =============================================================================
 */
#endif
