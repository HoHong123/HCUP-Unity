#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대화 그래프 카탈로그 ScriptableObject.
 *
 * 특징 / 지원기능 ::
 * + NodeCatalogSO 상속 — 노드/엣지/루트 그래프 데이터 자동 보유
 * + catalogTag        - Cutscene이면 DialogueDirector가 자동 진행
 * + RootNode          - RootUID로 실제 노드 인스턴스 조회
 * + registry / layout - 카탈로그 전용 무대 데이터; null이면 DialogueManager 씬 기본값 폴백
 * + [에디터 전용] editorLocalizationSO — LocalizationUID 미리보기용 LocalizationSO 참조
 *
 * 주의사항 ::
 * 노드/엣지 mutation은 NodeCatalogSO.Internal* 경유 (Editor asmdef 전용).
 * editorLocalizationSO는 에디터 전용 (#if UNITY_EDITOR) — 빌드에 포함되지 않음.
 * registry / layout null 허용 — null이면 DialogueManager 씬 기본값 폴백.
 * =========================================================
 */
#endif

using UnityEngine;
using HInspector;
using HWindows.NodeWindow;
#if UNITY_EDITOR
using HLocalization;
#endif

namespace HDialogue {
    [CreateAssetMenu(menuName = "HWindows/Dialogue/Dialogue Catalog")]
    public sealed class DialogueCatalogSO : NodeCatalogSO {
        #region Fields
        [SerializeField]
        DialogueCatalogTag catalogTag = DialogueCatalogTag.Normal;

        [HTitle("Audio")]
        [SerializeField]
        string bgmKey;

        [HTitle("Stage")]
        [SerializeField]
        CharacterRegistrySO registry;
        [SerializeField]
        StageLayoutSO layout;

#if UNITY_EDITOR
        [HTitle("Localization (Editor Test Only)")]
        [SerializeField]
        LocalizationSO editorLocalizationSO;
#endif
        #endregion

        #region Properties
        public DialogueCatalogTag CatalogTag => catalogTag;
        public string BgmKey => bgmKey;
        public CharacterRegistrySO Registry => registry;
        public StageLayoutSO Layout => layout;

        public BaseNode RootNode {
            get {
                if (!HasRoot) return null;
                Nodes.TryGetValue(RootUID, out BaseNode node);
                return node;
            }
        }
        #endregion

#if UNITY_EDITOR
        public bool EditorTryGetLocalizedText(string uid, out string text) {
            if (editorLocalizationSO != null && !string.IsNullOrEmpty(uid))
                return editorLocalizationSO.TryGetText(uid, out text);
            text = null;
            return false;
        }
#endif
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: bgmKey — 카탈로그 단위 BGM 토큰 추가
 *
 * # 추가
 * - string bgmKey SerializeField + BgmKey 프로퍼티 추가.
 * - [HTitle("Audio")] 그룹으로 묶음.
 *
 * # 이유
 * - DialogueAudioController가 OnCatalogStart 수신 시 catalog.BgmKey로 PlayBGM 호출.
 * - 빈 문자열이면 AudioController가 무동작(BGM 없는 카탈로그).
 *
 * =============================================================================
 * @Jason - PKH 2026.05.18 (수정) :: registry / layout — 카탈로그 전용 무대 데이터 추가
 *
 * # 추가
 * - CharacterRegistrySO registry, StageLayoutSO layout SerializeField 추가.
 * - 공개 프로퍼티 Registry, Layout 추가.
 * - [HTitle("Stage")] 그룹으로 묶음. using HInspector 추가.
 *
 * # 이유
 * - 1 DialogueCatalogSO = 1 StageLayoutSO = 1 CharacterRegistrySO 규칙.
 * - DialogueManager.PlayCatalog 진입 시 카탈로그의 registry/layout으로 stageDirector 재바인드.
 * - null 허용 — null이면 DialogueManager 씬 기본값 폴백.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: defaultSpeakerKey / defaultBlipToken 제거 + 로컬리제이션 연동 재설계
 *
 * # 변경
 * - defaultSpeakerKey / defaultBlipToken 필드·프로퍼티 제거.
 *   화자·블립 폴백은 카탈로그 레벨이 아닌 라인 노드 자체에서 관리.
 * - localizationTable(string) 제거.
 * - editorLocalizationSO(LocalizationSO) 추가 (#if UNITY_EDITOR) — DialogueLineNode.LocalizationUID 미리보기 전용.
 * - EditorTryGetLocalizedText(uid, out text) 공개 (#if UNITY_EDITOR):
 *   DialogueLineNodePreviewDrawer 등 에디터 코드가 LocalizationSO 타입 없이 텍스트 조회 가능.
 *   → HCUP.HDialogue.Editor.asmdef에 HCUP.HLocalization 참조 불필요.
 * - HCUP.HDialogue.asmdef에 HCUP.HLocalization 참조 추가.
 * - `#if UNITY_EDITOR using HLocalization; #endif` 조건부 using.
 *
 * # 이유
 * - 카탈로그 SO는 그래프 데이터 보관이 역할. 화자/블립 런타임 기본값은 Director 또는 노드 책임.
 * - 로컬리제이션 연동: 각 라인 노드가 LocalizationUID를 가지고, HTextLocalizer.GetText(uid)로 해석.
 *   카탈로그는 에디터 미리보기용 LocalizationSO 참조만 보유.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueCatalogSO 베이스 코드 생성
 *
 * # 목적
 * - 대화 그래프 카탈로그 SO — 8종 노드 + 엣지를 단일 에셋으로 보관
 * - HCUP-2.3.0 Phase 0
 *
 * # 설계 결정
 * - NodeCatalogSO 상속: 그래프 데이터(Nodes/Edges/RootUID) 재사용
 * - sealed: NodeCatalogSO.CreateAssetMenu와 DialogueCatalogSO.CreateAssetMenu
 *   양립을 위해 파생 금지 (서브클래스에서 menuName 오염 방지)
 * - defaultBlipToken string: AudioClip 직접 참조 금지(BaseNode 규칙) 준수
 * - localizationTable string: 통합 결정 보류 — 테이블 이름만 보관, 구현체 없음
 *
 * =============================================================================
 */
#endif
