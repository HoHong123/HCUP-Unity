#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 배포 레포 git 시나리오 서비스 클래스입니다.
 *
 * 특징 / 지원기능 ::
 * 배포 전용 레포(루트 = index.html + Build/)에 대한 고수준 git 작업을 제공한다.
 * + Preflight : 레포 검증 → 클린 확인 → fetch → checkout → pull --ff-only
 * + 산출물 교체(ReplaceArtifacts) / 스테이징·커밋·push / dev → release 승격(merge --no-ff)
 * + 실패 시 워킹트리 원복(RestoreArtifactsAsync)
 *
 * 주의사항 ::
 * dirty 레포는 자동 stash 없이 중단한다. push는 force 금지.
 * 모든 함수는 실패 시 로그를 남기고 false/null을 반환한다 — 호출자가 즉시 중단할 것.
 * =========================================================
 */
#endif

using System.IO;
using System.Threading.Tasks;
using HDeploy.Deploy;

namespace HDeploy.Git {
    public sealed class DeployRepoGitService {
        #region 상수
        const string ARTIFACT_BUILD_FOLDER = "Build";
        const string ARTIFACT_INDEX_FILE = "index.html";
        const string GIT_FOLDER = ".git";
        #endregion

        #region 변수
        readonly string repoPath;
        readonly int localTimeoutSeconds;
        readonly int remoteTimeoutSeconds;
        readonly DeployLog log;
        #endregion

        #region 생성자 & 소멸자
        public DeployRepoGitService(string repoPath, int localTimeoutSeconds, int remoteTimeoutSeconds, DeployLog log) {
            this.repoPath = repoPath;
            this.localTimeoutSeconds = localTimeoutSeconds;
            this.remoteTimeoutSeconds = remoteTimeoutSeconds;
            this.log = log;
        }
        #endregion

        #region 일반 함수 - Preflight
        /// <summary> Dev 배포 전제조건 일괄 검사. 레포 검증 → 클린 → fetch → checkout → pull --ff-only. </summary>
        public async Task<bool> RunPreflightAsync(string branchName) {
            if (await ValidateRepoAsync() == false) return false;
            if (await IsWorkingTreeCleanAsync() == false) return false;
            if (await FetchAsync() == false) return false;
            if (await CheckoutAsync(branchName) == false) return false;
            if (await PullFastForwardAsync(branchName) == false) return false;

            log.Info("Preflight passed.");
            return true;
        }

        /// <summary> 경로 존재 + git 작업 트리 여부 검증. </summary>
        public async Task<bool> ValidateRepoAsync() {
            if (string.IsNullOrEmpty(repoPath) || Directory.Exists(repoPath) == false) {
                log.Error($"Deploy repo path not found :: '{repoPath}'. Set a valid path in settings.");
                return false;
            }

            GitResult result = await _RunLocalAsync("rev-parse --is-inside-work-tree");
            if (result.IsSuccess == false) {
                log.Error($"Not a git work tree :: {repoPath} :: {result.StandardError}");
                return false;
            }
            return true;
        }

        /// <summary> 워킹트리 클린 여부. dirty면 변경 목록을 로그로 남기고 false (자동 stash 금지). </summary>
        public async Task<bool> IsWorkingTreeCleanAsync() {
            GitResult result = await _RunLocalAsync("status --porcelain");
            if (result.IsSuccess == false) {
                log.Error($"git status failed :: {result.StandardError}");
                return false;
            }

            if (string.IsNullOrEmpty(result.StandardOutput) == false) {
                log.Error($"Deploy repo is dirty. Clean it manually first.\n{result.StandardOutput}");
                return false;
            }
            return true;
        }

        public async Task<bool> FetchAsync() {
            GitResult result = await _RunRemoteAsync("fetch origin");
            if (result.IsSuccess == false) {
                log.Error($"git fetch failed (network/credential?) :: {result.StandardError}");
                return false;
            }
            return true;
        }

        public async Task<bool> CheckoutAsync(string branchName) {
            GitResult result = await _RunLocalAsync($"checkout {branchName}");
            if (result.IsSuccess == false) {
                log.Error($"git checkout {branchName} failed :: {result.StandardError}");
                return false;
            }
            return true;
        }

        public async Task<bool> PullFastForwardAsync(string branchName) {
            GitResult result = await _RunRemoteAsync($"pull --ff-only origin {branchName}");
            if (result.IsSuccess == false) {
                log.Error($"git pull --ff-only {branchName} failed. Resolve branch divergence manually :: {result.StandardError}");
                return false;
            }
            return true;
        }
        #endregion

        #region 일반 함수 - 산출물 교체
        /// <summary> 빌드 산출물(index.html + Build/)을 레포 루트에 교체 복사. 그 외 파일은 불변. </summary>
        public bool ReplaceArtifacts(string buildOutputAbsolutePath) {
            string sourceIndexPath = Path.Combine(buildOutputAbsolutePath, ARTIFACT_INDEX_FILE);
            string sourceBuildPath = Path.Combine(buildOutputAbsolutePath, ARTIFACT_BUILD_FOLDER);
            if (File.Exists(sourceIndexPath) == false || Directory.Exists(sourceBuildPath) == false) {
                log.Error($"Build output is incomplete :: '{buildOutputAbsolutePath}' must contain {ARTIFACT_INDEX_FILE} and {ARTIFACT_BUILD_FOLDER}/.");
                return false;
            }

            // 삭제 전 sanity check — 배포 레포가 맞는지(.git + index.html) 확인 후에만 삭제 진행.
            string targetGitPath = Path.Combine(repoPath, GIT_FOLDER);
            string targetIndexPath = Path.Combine(repoPath, ARTIFACT_INDEX_FILE);
            string targetBuildPath = Path.Combine(repoPath, ARTIFACT_BUILD_FOLDER);
            if (Directory.Exists(targetGitPath) == false || File.Exists(targetIndexPath) == false) {
                log.Error($"Sanity check failed :: '{repoPath}' does not look like a deploy repo (.git + {ARTIFACT_INDEX_FILE}). Abort.");
                return false;
            }

            try {
                if (Directory.Exists(targetBuildPath)) Directory.Delete(targetBuildPath, true);
                File.Delete(targetIndexPath);

                _CopyDirectory(sourceBuildPath, targetBuildPath);
                File.Copy(sourceIndexPath, targetIndexPath);
            }
            catch (IOException exception) {
                log.Error($"Artifact replace failed (file locked?) :: {exception.Message}");
                return false;
            }

            log.Info("Artifacts replaced (index.html + Build/).");
            return true;
        }

        /// <summary> 산출물 경로만 원복 — checkout + clean. 클린 preflight 통과 후에만 안전. </summary>
        public async Task<bool> RestoreArtifactsAsync() {
            GitResult checkoutResult = await _RunLocalAsync("checkout -- .");
            GitResult cleanResult = await _RunLocalAsync($"clean -fd -- {ARTIFACT_BUILD_FOLDER} {ARTIFACT_INDEX_FILE}");
            if (checkoutResult.IsSuccess == false || cleanResult.IsSuccess == false) {
                log.Error($"Restore failed. Check the deploy repo manually :: {checkoutResult.StandardError} {cleanResult.StandardError}");
                return false;
            }

            log.Warning("Working tree restored to HEAD.");
            return true;
        }
        #endregion

        #region 일반 함수 - 커밋 / Push
        /// <summary> 산출물 경로 스테이징 후 커밋할 변경 존재 여부. 에러 시 null. </summary>
        public async Task<bool?> StageArtifactsAsync() {
            GitResult addResult = await _RunLocalAsync($"add -A -- {ARTIFACT_BUILD_FOLDER} {ARTIFACT_INDEX_FILE}");
            if (addResult.IsSuccess == false) {
                log.Error($"git add failed :: {addResult.StandardError}");
                return null;
            }

            GitResult statusResult = await _RunLocalAsync("status --porcelain");
            if (statusResult.IsSuccess == false) {
                log.Error($"git status failed :: {statusResult.StandardError}");
                return null;
            }
            return string.IsNullOrEmpty(statusResult.StandardOutput) == false;
        }

        public async Task<bool> CommitAsync(string message) {
            string escaped = message.Replace("\"", "\\\"");
            GitResult result = await _RunLocalAsync($"commit -m \"{escaped}\"");
            if (result.IsSuccess == false) {
                log.Error($"git commit failed :: {result.StandardError}");
                return false;
            }

            log.Info($"Committed :: {message}");
            return true;
        }

        public async Task<bool> PushAsync(string branchName) {
            GitResult result = await _RunRemoteAsync($"push origin {branchName}");
            if (result.IsSuccess == false) {
                log.Error($"git push {branchName} rejected. Never force-push — resolve manually :: {result.StandardError}");
                return false;
            }

            log.Info($"Pushed :: origin/{branchName}");
            return true;
        }
        #endregion

        #region 일반 함수 - 승격 (dev → release)
        /// <summary> origin/{release}..origin/{dev} 커밋 수. 에러 시 null. </summary>
        public async Task<int?> CountPromotableCommitsAsync(string devBranchName, string releaseBranchName) {
            GitResult result = await _RunLocalAsync($"rev-list origin/{releaseBranchName}..origin/{devBranchName} --count");
            if (result.IsSuccess == false) {
                log.Error($"git rev-list failed :: {result.StandardError}");
                return null;
            }

            if (int.TryParse(result.StandardOutput, out int count) == false) {
                log.Error($"Unexpected rev-list output :: '{result.StandardOutput}'");
                return null;
            }
            return count;
        }

        /// <summary> origin/{branch}의 최신 커밋 제목. 에러 시 null. </summary>
        public async Task<string> GetRemoteLatestSubjectAsync(string branchName) {
            GitResult result = await _RunLocalAsync($"log origin/{branchName} -1 --format=%s");
            if (result.IsSuccess == false) {
                log.Error($"git log failed :: {result.StandardError}");
                return null;
            }
            return result.StandardOutput;
        }

        /// <summary> origin/{source}를 현재 브랜치에 --no-ff merge. 충돌 시 즉시 abort 후 false. </summary>
        public async Task<bool> MergeNoFastForwardAsync(string sourceBranchName, string message) {
            string escaped = message.Replace("\"", "\\\"");
            GitResult result = await _RunLocalAsync($"merge --no-ff origin/{sourceBranchName} -m \"{escaped}\"");
            if (result.IsSuccess) {
                log.Info($"Merged origin/{sourceBranchName} (--no-ff).");
                return true;
            }

            log.Error($"Merge failed — aborting to keep the repo consistent :: {result.StandardError}");
            GitResult abortResult = await _RunLocalAsync("merge --abort");
            if (abortResult.IsSuccess == false) {
                log.Error($"merge --abort also failed. Check the deploy repo manually :: {abortResult.StandardError}");
            }
            return false;
        }
        #endregion

        #region 일반 함수 - 내부
        private Task<GitResult> _RunLocalAsync(string arguments) {
            return GitCommandRunner.RunAsync(repoPath, arguments, localTimeoutSeconds);
        }

        private Task<GitResult> _RunRemoteAsync(string arguments) {
            return GitCommandRunner.RunAsync(repoPath, arguments, remoteTimeoutSeconds);
        }

        private static void _CopyDirectory(string sourcePath, string targetPath) {
            Directory.CreateDirectory(targetPath);
            foreach (string filePath in Directory.GetFiles(sourcePath)) {
                File.Copy(filePath, Path.Combine(targetPath, Path.GetFileName(filePath)));
            }
            foreach (string directoryPath in Directory.GetDirectories(sourcePath)) {
                _CopyDirectory(directoryPath, Path.Combine(targetPath, Path.GetFileName(directoryPath)));
            }
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
 * - DeployLog 참조를 위해 using HDeploy.Deploy 추가.
 *
 * =============================================================================
 * @Jason - PKH 2026.07.03 DeployRepoGitService 베이스 코드 생성
 *
 * # 목적
 * - 배포 레포에 대한 고수준 git 시나리오 단일 창구. Preflight / 산출물 교체 / 커밋 push / 승격 / 원복.
 *
 * # 사용 흐름
 * - VercelDeployService가 Dev 배포·Release 승격 시퀀스에서 각 함수를 순차 호출.
 * - 모든 함수는 실패 시 false/null — 호출자는 즉시 중단.
 *
 * # 설계 결정
 * - dirty 자동 stash 금지 / push force 금지 / merge 충돌 즉시 abort — 파괴적 자동 복구 배제.
 * - 산출물 컨벤션(index.html + Build/) 고정 — dr2-web/arrowfall-web 공통 구조가 패키지 전제조건.
 *
 * =============================================================================
 */
#endif
