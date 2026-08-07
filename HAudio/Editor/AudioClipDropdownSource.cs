#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 오디오 클립 목록을 HInspector 드롭다운 등록소에 공급합니다.
 *
 * 주요 기능 ::
 * AudioAuthoringSettingsSO 의 카탈로그 목록을 읽어 {uid, "UI/600003_Click"} 항목으로 만든다.
 * 등록 ID 는 AudioCatalogSO.DROPDOWN_SOURCE_ID — 런타임 쪽 [HDropdown] 이 같은 상수를 쓴다.
 *
 * 사용법 ::
 * 직접 호출할 일이 없다. [InitializeOnLoadMethod] 로 에디터 시작·도메인 리로드마다 등록된다.
 *
 * 주의 ::
 * 1. 등록소는 정적이라 도메인 리로드마다 비워진다 — 그래서 등록이 InitializeOnLoadMethod 다.
 * 2. 항목은 캐시한다. 인스펙터는 리페인트마다 목록을 물어보므로 매번 카탈로그를 훑으면
 *    에디터가 눈에 띄게 느려진다. 무효화는 에셋 임포트 후크가 담당한다.
 * 3. 설정 에셋이 없으면 빈 목록을 준다. 드로어가 "Missing (값)" 으로 붉게 표시하므로
 *    조용히 넘어가지 않는다.
 * =========================================================
 */

using System.Collections.Generic;
using UnityEditor;
using HInspector.Editor;
using HAudio.Core;

namespace HAudio.Editor {
    public static class AudioClipDropdownSource {
        #region Fields
        static List<HDropdownOption> cache;
        #endregion

        #region Registration
        [InitializeOnLoadMethod]
        private static void _Register() {
            HDropdownSourceRegistry.Register(AudioCatalogSO.DROPDOWN_SOURCE_ID, _GetOptions);
        }

        /// <summary> 카탈로그·설정이 바뀌면 다음 조회에서 다시 만든다. </summary>
        public static void Invalidate() {
            cache = null;
        }
        #endregion

        #region Build
        private static IReadOnlyList<HDropdownOption> _GetOptions() {
            if (cache != null) return cache;

            var built = new List<HDropdownOption>();
            var seenUids = new HashSet<int>();

            AudioAuthoringSettingsSO settings = AudioAuthoringSettingsSO.Find();
            if (settings == null) {
                cache = built;
                return cache;
            }

            IReadOnlyList<AudioCatalogSO> catalogs = settings.Catalogs;
            for (int k = 0; k < catalogs.Count; k++) {
                AudioCatalogSO catalog = catalogs[k];
                if (!catalog) continue;

                IReadOnlyList<AudioCatalogSO.Entry> entries = catalog.Entries;
                if (entries == null) continue;

                for (int j = 0; j < entries.Count; j++) {
                    AudioCatalogSO.Entry entry = entries[j];
                    if (entry == null) continue;

                    string token = entry.Token;
                    if (string.IsNullOrWhiteSpace(token)) continue;

                    int uid = entry.Uid;
                    // 기존 카탈로그 에셋은 uid 필드가 없어 0 으로 역직렬화된다 — token 에서 되살린다.
                    if (uid == 0 && !AudioCatalogSO.TryParseUid(token, out uid)) continue;
                    if (uid == 0) continue;

                    // 같은 uid 가 여러 카탈로그에 있으면 먼저 만난 쪽만 남긴다.
                    // 목록에 중복이 뜨면 저작자가 어느 쪽을 고른 것인지 알 수 없다.
                    if (!seenUids.Add(uid)) continue;

                    built.Add(new HDropdownOption(uid, $"{entry.Major}/{token}"));
                }
            }

            built.Sort((a, b) => a.Value.CompareTo(b.Value));

            cache = built;
            return cache;
        }
        #endregion
    }

    /// <summary> 카탈로그·설정 에셋이 바뀌면 드롭다운 캐시를 버린다. </summary>
    internal sealed class AudioAuthoringAssetWatcher : AssetPostprocessor {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths) {

            // 카탈로그·설정은 둘 다 .asset 이다. 확장자로만 거른다 — 파일명 규칙에 의존하면
            // 저작자가 이름을 바꿨을 때 조용히 갱신되지 않는다.
            if (_HasAsset(importedAssets) || _HasAsset(deletedAssets) || _HasAsset(movedAssets)) {
                AudioClipDropdownSource.Invalidate();
            }
        }

        private static bool _HasAsset(string[] paths) {
            if (paths == null) return false;

            for (int k = 0; k < paths.Length; k++) {
                if (paths[k] != null && paths[k].EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }
    }
}
#endif

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.08.06 신규 생성
 *
 * # 설계 결정
 * - 라벨에 Major 를 앞세워 "UI/600003_Click" 형태로 보낸다. HDropdownField 가 '/' 를
 *   하위 메뉴로 접으므로 별도 API 없이 분류가 생긴다.
 * - uid 오름차순 정렬. uid 는 엑셀 넘버링과 같은 축이라 저작자가 기억하는 순서다.
 * - uid 중복은 먼저 만난 쪽만 남긴다. enum 생성기의 이름 충돌 정책(실패)과 다른 이유는,
 *   여기는 저작 UI 라 목록이 비는 것보다 일부라도 보이는 편이 낫기 때문이다.
 *   중복 자체는 enum 생성 시점에 걸린다.
 *
 * =============================================================================
 */
#endif
