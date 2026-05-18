#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대사 출력 노드. 화자 정보 + 텍스트 + 포트레이트 확장 필드 보유.
 *
 * 특징 / 지원기능 ::
 * + speakerKey / localizationUID / speedMultiplier / overrideBlipToken — 텍스트 핸들링
 * + speakerPoseKey / speakerSlotOverride+speakerSlot / speakerFacing / autoHighlight — 포트레이트
 * + 각 필드가 빈 문자열이면 DialogueDirector._BuildLine 폴백 우선순위 적용
 *
 * 주의사항 ::
 * rawText는 로컬라이제이션 완료 후 번역된 결과이어야 함 (직접 입력 시).
 * overrideBlipToken은 AudioClip 아님 — IBlipSfxService DI 토큰.
 * localizationUID — HTextLocalizer.GetText(uid)로 해석. 에디터 미리보기는 catalog.editorLocalizationSO 경유.
 * =========================================================
 */
#endif

using HAudio;
using HInspector;
using HWindows.NodeWindow;
using UnityEngine;

namespace HDialogue {
    public sealed class DialogueLineNode : BaseNode {
        #region Fields
        [HTitle("Dialogue")]
        [SerializeField]
        string speakerKey;
        [SerializeField]
        string localizationUID;
        [SerializeField]
        float speedMultiplier = 1f;
        [SerializeField]
        AudioClips overrideBlipToken;

        [HTitle("Portrait")]
        [SerializeField]
        string speakerPoseKey;
        [SerializeField]
        bool speakerSlotOverride;
        [SerializeField]
        StageSlot speakerSlot;
        [SerializeField]
        FacingDirection speakerFacing = FacingDirection.Left;
        [SerializeField]
        bool autoHighlight = true;
        #endregion

        #region Properties
        public string SpeakerKey => speakerKey;
        public string LocalizationUID => localizationUID;
        public float SpeedMultiplier => speedMultiplier;
        public AudioClips OverrideBlipToken => overrideBlipToken;

        public string SpeakerPoseKey => speakerPoseKey;
        public StageSlot? SpeakerSlot => speakerSlotOverride ? speakerSlot : (StageSlot?)null;
        public FacingDirection SpeakerFacing => speakerFacing;
        public bool AutoHighlight => autoHighlight;
        #endregion

        public override string ClipboardMagic => "HGRAPH_DIALOGUE_LINE_NODE_V1";

#if UNITY_EDITOR
        public override string GetInspectorSummary(NodeCatalogSO catalog) {
            string base_ = base.GetInspectorSummary(catalog);
            string speaker = string.IsNullOrEmpty(speakerKey) ? "(no speaker)" : speakerKey;
            string preview = localizationUID?.Length > 30 ? localizationUID[..30] + "…" : localizationUID ?? "";
            return $"{base_}\n  [{speaker}] {preview}";
        }
#endif
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: rawText → localizationUID 로컬리제이션 연동
 *
 * # 변경
 * - `string rawText` → `string localizationUID`.
 * - `[TextArea(3,6)]` attribute 제거 (UID는 단일 행 문자열).
 * - `RawText` 프로퍼티 → `LocalizationUID`.
 * - GetInspectorSummary: rawText → localizationUID 참조.
 *
 * # 이유
 * - 카탈로그 각 라인 노드는 로컬리제이션 UID만 보유. 텍스트 해석은
 *   런타임에 HTextLocalizer.GetText(uid), 에디터 미리보기는 catalog.EditorTryGetLocalizedText(uid).
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: speakerSlot 타입 FacingDirection → StageSlot
 *
 * # 변경
 * - `FacingDirection speakerSlot` → `StageSlot speakerSlot`.
 * - `FacingDirection? SpeakerSlot` → `StageSlot? SpeakerSlot`.
 * - speakerFacing(FacingDirection) 방향 필드는 유지.
 *
 * # 이유
 * - AgentReview Warning #7. 슬롯 위치(StageSlot)와 방향(FacingDirection) 개념 분리.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: speakerSlotKey(string) → speakerSlotOverride(bool) + speakerSlot(FacingDirection)
 *
 * # 변경
 * - `string speakerSlotKey` 제거 → `bool speakerSlotOverride` + `FacingDirection speakerSlot` 두 필드로 교체.
 * - `SpeakerSlotKey (string)` 프로퍼티 제거 → `SpeakerSlot (FacingDirection?)`: override=false이면 null 반환.
 *
 * # 이유
 * - Unity SerializeField에서 FacingDirection?를 직접 직렬화할 수 없어 bool+enum 쌍 패턴 사용.
 * - null = "슬롯 미지정" 의미가 LineStageContext.SpeakerSlot?과 계약 일치.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueLineNode 베이스 코드 생성
 *
 * # 목적
 * - 대사 출력 노드 — HCUP-2.3.0 Phase 1
 *
 * # 설계 결정
 * - speedMultiplier 기본값 1f: Inspector 미설정 시 정상 속도 보장 (0이면 타이핑 멈춤)
 * - speakerFacing 기본값 Left: 대부분의 화자가 화면 왼쪽 슬롯에 배치되는 관습
 * - autoHighlight 기본값 true: 명시 해제 전까지 화자 하이라이트 자동 적용
 * - GetInspectorSummary override: 그래프 뷰에서 화자 + 텍스트 프리뷰 제공 (Phase 5 확장 예정)
 *
 * # 사용 흐름
 * - DialogueDirector._ProcessLineNode → _BuildLine(node) + _BuildLineStageContext(node)
 * - SpeakerKey / SpeedMultiplier / OverrideBlipToken 폴백: node → catalog.Default → ""
 *
 * =============================================================================
 */
#endif
