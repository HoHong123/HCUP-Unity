#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 오디오 저작 도구가 공유하는 설정 에셋입니다. 에디터 전용입니다.
 *
 * 주요 기능 ::
 * 1. enum 생성 대상 카탈로그 목록 — 생성기와 인스펙터 드롭다운이 이 목록 하나를 본다.
 * 2. enum 출력 위치·네임스페이스·타입명.
 *
 * 사용법 ::
 * Project 창 우클릭 → Create → HCUP/Audio/Audio Authoring Settings.
 * 프로젝트당 1개만 둔다. Sound Tools 의 Enum 탭이 자동으로 찾아 쓴다.
 *
 * 주의 ::
 * 1. 대상 카탈로그를 전체 스캔으로 구하지 않는 이유 — 전체 스캔은 HCUP 샘플·테스트
 *    카탈로그까지 잡아 게임 enum 을 오염시킨다. 특히 Samples~ 를 Samples 로 복제해
 *    쓰는 이 프로젝트의 운용에서는 확실히 재현된다.
 * 2. 생성기와 드로어가 같은 목록을 봐야 인스펙터 드롭다운과 enum 이 어긋나지 않는다.
 *    설정을 창 상태가 아니라 에셋에 둔 이유가 이것이다 (저장소에 남고 PC 를 따라간다).
 * 3. 출력 폴더에 기본값을 두지 않는다 — 저작자가 게임 어셈블리 폴더를 명시하게 강제한다.
 *    HCUP 안에 생성하면 다른 프로젝트의 enum 을 덮어쓴다.
 * =========================================================
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HDiagnosis.Logger;
using HAudio.Core;

namespace HAudio.Editor {
    [CreateAssetMenu(
        menuName = "HCUP/Audio/Audio Authoring Settings",
        fileName = "AudioAuthoringSettings")]
    public sealed class AudioAuthoringSettingsSO : ScriptableObject {
        #region Serialized
        [SerializeField]
        List<AudioCatalogSO> catalogs = new();

        [SerializeField]
        DefaultAsset outputFolder;

        [SerializeField]
        string namespaceName = string.Empty;

        [SerializeField]
        string enumTypeName = "AudioClips";
        #endregion

        #region Properties
        public IReadOnlyList<AudioCatalogSO> Catalogs => catalogs;
        public DefaultAsset OutputFolder => outputFolder;
        public string NamespaceName => namespaceName;
        public string EnumTypeName => enumTypeName;

        public string OutputFolderPath =>
            outputFolder ? AssetDatabase.GetAssetPath(outputFolder) : string.Empty;
        #endregion

        #region Public - Mutation
        public void SetOutputFolder(DefaultAsset value) => outputFolder = value;
        public void SetNamespaceName(string value) => namespaceName = value ?? string.Empty;
        public void SetEnumTypeName(string value) => enumTypeName = value ?? string.Empty;

        public void AddCatalogSlot() => catalogs.Add(null);
        public void ClearCatalogs() => catalogs.Clear();
        public void RemoveCatalogAt(int index) {
            if (index < 0 || index >= catalogs.Count) return;
            catalogs.RemoveAt(index);
        }

        public void SetCatalogAt(int index, AudioCatalogSO value) {
            if (index < 0 || index >= catalogs.Count) return;
            catalogs[index] = value;
        }

        /// <summary> 중복은 넣지 않는다. 실제로 추가했으면 true. </summary>
        public bool TryAddCatalog(AudioCatalogSO catalog) {
            if (!catalog) return false;
            if (catalogs.Contains(catalog)) return false;

            catalogs.Add(catalog);
            return true;
        }
        #endregion

        #region Public - Lookup
        /// <summary>
        /// 프로젝트의 설정 에셋을 찾는다. 없으면 null.
        /// 2개 이상이면 어느 쪽이 정본인지 알 수 없으므로 경고하고 첫 번째를 쓴다.
        /// </summary>
        public static AudioAuthoringSettingsSO Find() {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(AudioAuthoringSettingsSO)}");
            if (guids == null || guids.Length < 1) return null;

            if (guids.Length > 1) {
                HLogger.Warning(
                    $"[AudioAuthoring] Found {guids.Length} settings assets. Using the first one — " +
                    "keep exactly one per project or the enum and the inspector dropdown will disagree.");
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<AudioAuthoringSettingsSO>(path);
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
 * - AudioClipEnumPanel 의 창 로컬 상태로만 존재하던 저작 설정을 에셋으로 승격.
 *
 * # 이유
 * - 창 상태는 저장소에 남지 않는다. 다른 PC 에서 enum 을 재생성하면 다른 결과가 나오고,
 *   창 상태가 리셋되면 설정이 사라진다. enum 은 게임의 공개 API 표면이라 재현 가능해야 한다.
 * - 인스펙터 드롭다운(HDropdown "HAudio.Clips")이 같은 목록을 봐야 enum 과 어긋나지 않는다.
 *   두 소비자가 생겼으므로 설정의 소유자가 창일 수 없다.
 *
 * # 배치 결정
 * - 에디터 어셈블리에 둔다. 런타임에서 참조하지 않는 저작 전용 데이터다.
 *
 * =============================================================================
 */
#endif
