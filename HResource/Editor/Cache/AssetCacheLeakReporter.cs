#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 플레이 종료 시점에 회수되지 않은 캐시 점유를 콘솔로 알리는 에디터 검사입니다.
 *
 * 특징 / 지원기능 ::
 * 두 갈래를 나눠 보고합니다.
 * + 이미 GC 된 캐시가 점유를 들고 있었다 - Dispose 누락의 확정 증거
 * + 아직 살아있는 캐시가 점유를 들고 있다 - 회수 누락이거나 정상적인 상주
 *
 * 주의사항 ::
 * 첫 갈래만 확정 결함입니다. 그 캐시는 이미 사라져 어떤 API 로도 핸들을 되돌릴 수 없습니다.
 * 둘째 갈래는 판단이 필요합니다. 씬 수명 내내 상주하는 provider 라면 정상입니다.
 * ExitingPlayMode 시점에는 아직 GC 가 돌지 않았을 수 있어 첫 갈래가 과소 보고될 수 있습니다.
 *
 * 사용 ::
 * 별도 조작 없이 [InitializeOnLoad] 로 자동 등록됩니다.
 * =========================================================
 */

using System.Collections.Generic;
using System.Text;
using HDiagnosis.Logger;
using HResource.Cache;
using UnityEditor;

namespace HResource.Editor.Cache {
    [InitializeOnLoad]
    public static class AssetCacheLeakReporter {
        #region Fields
        static readonly List<string> leakSuspects = new();
        static readonly List<string> liveHolders = new();
        #endregion

        #region 생성자
        static AssetCacheLeakReporter() {
            EditorApplication.playModeStateChanged -= _OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += _OnPlayModeStateChanged;
        }
        #endregion

        #region Private
        private static void _OnPlayModeStateChanged(PlayModeStateChange change) {
            if (change != PlayModeStateChange.ExitingPlayMode) return;

            // GC 가 아직 돌지 않았으면 사라진 캐시가 잡히지 않는다. 판정 직전에 한 번 돌린다.
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();

            AssetCacheDiagnosticsRegistry.CollectLeakSuspects(leakSuspects);
            AssetCacheDiagnosticsRegistry.CollectLiveHolders(liveHolders);

            _ReportLeakSuspects();
            _ReportLiveHolders();
        }

        private static void _ReportLeakSuspects() {
            if (leakSuspects.Count < 1) return;

            HLogger.Error(
                $"[AssetCacheLeakReporter] {leakSuspects.Count} cache(s) were collected while still holding occupancy.\n" +
                $"Their AssetProvider was dropped without Dispose, so the loader handles can no longer be released by any API.\n" +
                $"{_Join(leakSuspects)}");
        }

        private static void _ReportLiveHolders() {
            if (liveHolders.Count < 1) return;

            HLogger.Warning(
                $"[AssetCacheLeakReporter] {liveHolders.Count} cache(s) still hold occupancy at play mode exit.\n" +
                $"This is expected for providers that live for the whole session. Check the owners in HCUP/Resource/Owner Watcher.\n" +
                $"{_Join(liveHolders)}");
        }

        private static string _Join(List<string> lines) {
            StringBuilder builder = new StringBuilder();

            for (int k = 0; k < lines.Count; k++) {
                builder.Append("  - ").Append(lines[k]).Append('\n');
            }
            return builder.ToString();
        }
        #endregion
    }
}

/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.09.04 AssetCacheLeakReporter 베이스 코드 생성
 *
 * # 목적
 * - 케이스 리포트 USR-4 의 대응. provider 미폐기로 인한 영구 누수를 플레이 종료 시점에 알린다.
 *
 * # 사용 흐름
 * - [InitializeOnLoad] 로 자동 구독. ExitingPlayMode 에서 레지스트리를 훑는다.
 * - 확정 누수는 Error, 상주 가능성이 있는 것은 Warning 으로 심각도를 갈라 보고한다.
 *
 * # 설계 결정
 * - 판정 직전에 GC.Collect 를 부른다. 에디터 전용 1회 호출이라 비용을 감수하고,
 *   이것이 없으면 방금 버려진 캐시가 아직 살아 있어 확정 누수를 놓친다.
 * - 두 갈래를 심각도로 가른다. 살아있는 캐시의 점유는 정상일 수 있으므로 Error 로 올리면
 *   경고 피로가 생기고 진짜 누수가 묻힌다.
 *
 * =============================================================================
 */
#endif
