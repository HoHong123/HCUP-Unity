#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * [HDropdown] 이 그릴 항목 목록의 등록소입니다.
 *
 * 주요 기능 ::
 * 도메인이 소스 ID 로 항목 공급자를 등록하고, 드로어가 그 ID 로 목록을 받아 그립니다.
 * HInspector 는 어떤 도메인이 무엇을 등록하는지 알지 못합니다 — 의존이 한 방향입니다.
 *
 * 사용법 ::
 * [InitializeOnLoadMethod]
 * static void _Register() {
 *     HDropdownSourceRegistry.Register("HAudio.Clips", () => BuildOptions());
 * }
 *
 * 주의 ::
 * 1. 정적 상태이므로 도메인 리로드마다 비워진다. 등록은 [InitializeOnLoadMethod] 로 해야
 *    리로드 후 자동 복구된다.
 * 2. 공급자는 매 호출마다 실행된다. 비싼 스캔은 공급자 쪽에서 캐시할 것.
 * 3. 공급자가 던진 예외는 여기서 격리한다 — 인스펙터 한 필드의 사고가 인스펙터 전체의
 *    그리기를 멈추게 두지 않는다.
 * =========================================================
 */

using System;
using System.Collections.Generic;
using HDiagnosis.Logger;

namespace HInspector.Editor {
    /// <summary> 드롭다운 항목 하나. Label 에 '/' 를 넣으면 하위 메뉴로 접힌다. </summary>
    public readonly struct HDropdownOption {
        public readonly int Value;
        public readonly string Label;

        public HDropdownOption(int value, string label) {
            Value = value;
            Label = label;
        }
    }

    public static class HDropdownSourceRegistry {
        #region Fields
        static readonly Dictionary<string, Func<IReadOnlyList<HDropdownOption>>> providers =
            new Dictionary<string, Func<IReadOnlyList<HDropdownOption>>>(StringComparer.Ordinal);

        static readonly IReadOnlyList<HDropdownOption> EMPTY = Array.Empty<HDropdownOption>();
        #endregion

        #region Public - Registration
        public static void Register(string sourceId, Func<IReadOnlyList<HDropdownOption>> provider) {
            if (string.IsNullOrWhiteSpace(sourceId)) {
                HLogger.Error("[HDropdown] Register called with an empty source id.");
                return;
            }

            if (provider == null) {
                HLogger.Error($"[HDropdown] Register called with a null provider. sourceId='{sourceId}'");
                return;
            }

            // 덮어쓰기를 허용한다 — 도메인 리로드 순서에 따라 중복 등록이 정상적으로 발생한다.
            providers[sourceId] = provider;
        }

        public static void Unregister(string sourceId) {
            if (string.IsNullOrWhiteSpace(sourceId)) return;
            providers.Remove(sourceId);
        }

        public static bool IsRegistered(string sourceId) {
            return !string.IsNullOrWhiteSpace(sourceId) && providers.ContainsKey(sourceId);
        }
        #endregion

        #region Public - Query
        /// <summary> 목록을 가져온다. 소스 미등록이면 false, 공급자가 예외를 던지면 로그 후 false. </summary>
        public static bool TryGetOptions(string sourceId, out IReadOnlyList<HDropdownOption> options) {
            options = EMPTY;

            if (string.IsNullOrWhiteSpace(sourceId)) return false;
            if (!providers.TryGetValue(sourceId, out var provider)) return false;

            try {
                options = provider() ?? EMPTY;
            }
            catch (Exception e) {
                HLogger.Error($"[HDropdown] Provider threw for sourceId='{sourceId}': {e}");
                options = EMPTY;
                return false;
            }

            return true;
        }

        /// <summary> 값에 대응하는 라벨을 찾는다. 없으면 false — 참조가 끊긴 상태다. </summary>
        public static bool TryGetLabel(string sourceId, int value, out string label) {
            label = string.Empty;

            if (!TryGetOptions(sourceId, out var options)) return false;

            for (int k = 0; k < options.Count; k++) {
                if (options[k].Value != value) continue;
                label = options[k].Label;
                return true;
            }

            return false;
        }
        #endregion
    }
}
#endif

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.08.06 신규 생성
 *
 * # 목적
 * - HInspector 가 상위 도메인(HAudio 등)을 참조하지 않고도 그 도메인의 목록을 그리게 한다.
 *
 * # 설계 결정
 * - 문자열 ID 간접화. 컴파일 타임 결합이 없으므로 오타는 "소스 미등록" 으로 드러난다.
 *   드로어가 그 상태를 붉게 표시하므로 조용히 실패하지 않는다.
 * - 중복 Register 를 오류로 보지 않고 덮어쓴다. 도메인 리로드 후 재등록이 정상 경로다.
 * - 공급자 예외를 여기서 격리한다. 이 저장소에서 반복 확인된 축이다 —
 *   SceneLoader._InvokeSafely, MemoryAssetCache._NotifyRemoved 와 같은 처방.
 *
 * =============================================================================
 */
#endif
