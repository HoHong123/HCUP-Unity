#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- WebGL 빌드 실행·검증 서비스 클래스입니다.
 *
 * 특징 / 지원기능 ::
 * 출력 폴더 정리 → BuildPipeline.BuildPlayer(WebGL) → 산출물 검증까지 담당한다.
 * + 씬 목록은 EditorBuildSettings의 enabled 씬 사용
 * + 산출물 검증 : index.html + Build/의 .wasm/.data/.framework.js/.loader.js 각 1개
 *
 * 주의사항 ::
 * BuildPlayer는 동기 실행 — 빌드 동안 에디터가 블로킹된다. 메인 스레드에서만 호출할 것.
 * =========================================================
 */
#endif

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace HDeploy.Deploy {
    public static class WebGLBuildService {
        #region 상수
        const string ARTIFACT_BUILD_FOLDER = "Build";
        const string ARTIFACT_INDEX_FILE = "index.html";

        const int MIN_OUTPUT_PATH_DEPTH = 2;

        static readonly string[] REQUIRED_ARTIFACT_PATTERNS = { "*.wasm", "*.data", "*.framework.js", "*.loader.js" };
        static readonly string[] FORBIDDEN_ROOT_FOLDERS = { "Assets", "Library", "Packages", "ProjectSettings", "UserSettings", "Temp", "Logs", ".git" };
        #endregion

        #region 일반 함수
        /// <summary> 출력 폴더를 비우고 WebGL 빌드 후 산출물 구성을 검증. 실패 시 로그를 남기고 false. </summary>
        public static bool BuildAndValidate(string outputAbsolutePath, DeployLog log) {
            // 아래에서 출력 폴더를 재귀 삭제하므로, 삭제 전에 경로가 안전한지 먼저 확정한다.
            if (_ValidateOutputPath(ref outputAbsolutePath, log) == false) return false;

            string[] scenePaths = _GetEnabledScenePaths();
            if (scenePaths.Length == 0) {
                log.Error("No enabled scene in EditorBuildSettings. Add at least one scene.");
                return false;
            }

            // 해시 파일명 특성상 이전 빌드 파일이 스테일로 남으므로 출력 폴더를 통째로 비운다.
            if (Directory.Exists(outputAbsolutePath)) Directory.Delete(outputAbsolutePath, true);
            Directory.CreateDirectory(outputAbsolutePath);

            log.Info($"WebGL build start :: {outputAbsolutePath}");
            BuildPlayerOptions options = new BuildPlayerOptions {
                scenes = scenePaths,
                locationPathName = outputAbsolutePath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors > 0) {
                log.Error($"Build failed :: result={report.summary.result}, errors={report.summary.totalErrors}");
                return false;
            }

            log.Info($"Build succeeded :: {report.summary.totalSize / (1024 * 1024)}MB, {report.summary.totalTime.TotalSeconds:F1}s");
            return _ValidateArtifacts(outputAbsolutePath, log);
        }

        /// <summary> 출력 경로가 프로젝트 루트 하위의 전용 폴더인지 검증. 재귀 삭제 대상이므로 통과 전에는 삭제하지 않는다. </summary>
        private static bool _ValidateOutputPath(ref string outputAbsolutePath, DeployLog log) {
            if (string.IsNullOrWhiteSpace(outputAbsolutePath)) {
                log.Error("Build output path is empty. Set a relative folder such as 'Build/WebGL'.");
                return false;
            }

            string projectRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
            string fullPath;
            try {
                fullPath = Path.GetFullPath(outputAbsolutePath);
            }
            catch (System.ArgumentException e) {
                log.Error($"Build output path is malformed :: {outputAbsolutePath} ({e.Message})");
                return false;
            }

            string rootWithSeparator = projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                                  + Path.DirectorySeparatorChar;
            if (fullPath.StartsWith(rootWithSeparator, System.StringComparison.OrdinalIgnoreCase) == false) {
                log.Error($"Build output path escapes the project root :: '{fullPath}' is not under '{projectRoot}'. Abort.");
                return false;
            }

            string relative = fullPath.Substring(rootWithSeparator.Length);
            string[] segments = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                                               System.StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < MIN_OUTPUT_PATH_DEPTH) {
                log.Error($"Build output path is too shallow :: '{relative}' — use at least {MIN_OUTPUT_PATH_DEPTH} segments (e.g. 'Build/WebGL') so a stray value cannot wipe a top-level folder.");
                return false;
            }

            foreach (string forbidden in FORBIDDEN_ROOT_FOLDERS) {
                if (string.Equals(segments[0], forbidden, System.StringComparison.OrdinalIgnoreCase)) {
                    log.Error($"Build output path targets a protected folder :: '{segments[0]}/' cannot be used as build output. Abort.");
                    return false;
                }
            }

            outputAbsolutePath = fullPath;
            return true;
        }

        private static string[] _GetEnabledScenePaths() {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
        }

        private static bool _ValidateArtifacts(string outputAbsolutePath, DeployLog log) {
            string indexPath = Path.Combine(outputAbsolutePath, ARTIFACT_INDEX_FILE);
            if (File.Exists(indexPath) == false) {
                log.Error($"Artifact missing :: {ARTIFACT_INDEX_FILE} not found in build output.");
                return false;
            }

            string buildFolderPath = Path.Combine(outputAbsolutePath, ARTIFACT_BUILD_FOLDER);
            if (Directory.Exists(buildFolderPath) == false) {
                log.Error($"Artifact missing :: {ARTIFACT_BUILD_FOLDER}/ not found in build output.");
                return false;
            }

            foreach (string pattern in REQUIRED_ARTIFACT_PATTERNS) {
                int count = Directory.GetFiles(buildFolderPath, pattern).Length;
                if (count != 1) {
                    log.Error($"Artifact invalid :: expected exactly 1 '{pattern}' in {ARTIFACT_BUILD_FOLDER}/, found {count}.");
                    return false;
                }
            }

            log.Info("Artifact validation passed (index.html + 4 build files).");
            return true;
        }
        #endregion
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
 * @Jason - PKH 2026.07.03 WebGLBuildService 베이스 코드 생성
 *
 * # 목적
 * - Dev 배포 시퀀스의 빌드 단계. 폴더 정리 → BuildPlayer → 산출물 4종+index.html 검증.
 *
 * # 설계 결정
 * - Name Files As Hashes(ON) 전제 — 스테일 해시 파일 방지 위해 출력 폴더 전체 삭제 후 빌드.
 * - 압축 미사용(현 프로젝트 설정) 기준 산출물 확장자 검증. Brotli 전환 시 패턴 보완 필요.
 *
 * =============================================================================
 */
#endif
