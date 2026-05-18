#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueLineNode 바디 UIElements 미리보기 빌더.
 *
 * 특징 / 지원기능 ::
 * + Build(node, container, registry?) — 화자·방향·UID(→로컬라이즈) 텍스트·슬롯·포즈 + 포트레이트 스트립
 * + 포트레이트 스트립: portrait.* 이벤트 Sprite → AssetPreview 썸네일 표시
 * + _StripTags: 미리보기 텍스트에서 각도 괄호 태그 제거
 *
 * 주의사항 ::
 * AssetPreview.GetAssetPreview 는 비동기 — 첫 호출 시 null 가능 (다음 리페인트에서 갱신).
 * registry null 이면 포트레이트 스트립 생략.
 * =========================================================
 */
#endif

using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace HDialogue.Editor {
    public static class DialogueLineNodePreviewDrawer {
        #region Public
        public static void Build(DialogueLineNode node, VisualElement container, CharacterRegistrySO registry = null) {
            container.Clear();

            _BuildSpeakerRow(node, container);
            _BuildTextPreview(node, container);
            _BuildMetaRow(node, container);

            if (registry != null) {
                IReadOnlyList<PortraitEventInstruction> events =
                    DialogueLinePortraitTimelineBuilder.Build(node, registry);
                if (events.Count > 0) _BuildPortraitStrip(events, registry, container);
            }
        }
        #endregion

        #region Private — Row Builders
        private static void _BuildSpeakerRow(DialogueLineNode node, VisualElement container) {
            var row = new VisualElement();
            row.AddToClassList("hdialogue-meta-row");

            string speaker = string.IsNullOrEmpty(node.SpeakerKey) ? "(no speaker)" : $"[{node.SpeakerKey}]";
            var speakerLabel = new Label(speaker);
            speakerLabel.AddToClassList("hdialogue-speaker-label");
            row.Add(speakerLabel);

            string arrow = node.SpeakerFacing == FacingDirection.Left ? " ←" : " →";
            var facingLabel = new Label(arrow);
            facingLabel.AddToClassList("hdialogue-facing-label");
            row.Add(facingLabel);

            container.Add(row);
        }

        private static void _BuildTextPreview(DialogueLineNode node, VisualElement container) {
            string uid = node.LocalizationUID ?? string.Empty;

            string previewText = uid;
            if (!string.IsNullOrEmpty(uid)) {
                string path = UnityEditor.AssetDatabase.GetAssetPath(node);
                if (!string.IsNullOrEmpty(path)) {
                    DialogueCatalogSO catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueCatalogSO>(path);
                    if (catalog != null && catalog.EditorTryGetLocalizedText(uid, out string localized))
                        previewText = localized;
                }
            }

            string plain = _StripTags(previewText);
            if (plain.Length > 45) plain = plain[..45] + "…";
            string display = string.IsNullOrEmpty(uid)
                ? "(no uid)"
                : string.IsNullOrEmpty(plain) ? $"[{uid}]" : $"\"{plain}\"";

            var label = new Label(display);
            label.AddToClassList("hdialogue-text-preview");
            container.Add(label);
        }

        private static void _BuildMetaRow(DialogueLineNode node, VisualElement container) {
            bool hasSlot = node.SpeakerSlot.HasValue;
            bool hasPose = !string.IsNullOrEmpty(node.SpeakerPoseKey);
            if (!hasSlot && !hasPose) return;

            var row = new VisualElement();
            row.AddToClassList("hdialogue-meta-row");

            if (hasSlot) {
                var lbl = new Label($"Slot: {node.SpeakerSlot.Value}");
                lbl.AddToClassList("hdialogue-meta-label");
                row.Add(lbl);
            }
            if (hasPose) {
                var lbl = new Label($"Pose: {node.SpeakerPoseKey}");
                lbl.AddToClassList("hdialogue-meta-label");
                row.Add(lbl);
            }
            container.Add(row);
        }

        private static void _BuildPortraitStrip(
            IReadOnlyList<PortraitEventInstruction> events,
            CharacterRegistrySO registry,
            VisualElement container) {
            var strip = new VisualElement();
            strip.AddToClassList("hdialogue-portrait-strip");

            for (int k = 0; k < events.Count; k++) {
                PortraitEventInstruction ins = events[k];
                var thumb = new VisualElement();
                thumb.AddToClassList("hdialogue-portrait-thumb");

                Sprite sprite = DialogueLinePortraitTimelineBuilder.ResolveSprite(ins, registry);
                if (sprite != null) {
                    Texture2D tex = AssetPreview.GetAssetPreview(sprite);
                    if (tex != null) thumb.style.backgroundImage = new StyleBackground(tex);
                }

                var verbLabel = new Label(ins.Verb.ToString().ToLowerInvariant());
                verbLabel.AddToClassList("hdialogue-portrait-verb");
                thumb.Add(verbLabel);

                strip.Add(thumb);
            }
            container.Add(strip);
        }
        #endregion

        #region Private — Tag Strip
        private static string _StripTags(string text) {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new StringBuilder(text.Length);
            bool inTag = false;
            for (int k = 0; k < text.Length; k++) {
                char c = text[k];
                if (c == '<') { inTag = true; continue; }
                if (c == '>') { inTag = false; continue; }
                if (!inTag) sb.Append(c);
            }
            return sb.ToString();
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: _BuildTextPreview — LocalizationUID 미리보기 연동
 *
 * # 변경
 * - `node.RawText` → `node.LocalizationUID` 참조.
 * - AssetDatabase.GetAssetPath(node)로 부모 카탈로그 SO 조회.
 * - catalog.EditorTryGetLocalizedText(uid, out text): 성공 시 번역 텍스트 표시,
 *   실패(SO 미연결 / UID 없음) 시 UID 자체를 `[uid]` 형식으로 표시.
 * - UID 없으면 "(no uid)" 표시.
 *
 * # 이유
 * - DialogueLineNode.rawText 제거 → LocalizationUID 기반 연동으로 전환.
 * - HCUP.HDialogue.Editor.asmdef에 HLocalization 참조 불필요 (EditorTryGetLocalizedText가 캡슐화).
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueLineNodePreviewDrawer 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 5 — DialogueLineNode 노드 바디 UIElements 미리보기
 *
 * # 설계 결정
 * - 4분할 빌드: SpeakerRow / TextPreview / MetaRow / PortraitStrip (각 독립 메서드)
 * - AssetPreview 비동기: 썸네일이 처음엔 null → 다음 리페인트에서 자동 갱신 (허용)
 * - _StripTags: <event=...> / <pause=...> 등 HCUP 커스텀 태그 + TMP 태그 동시 제거
 * - text[..45]: C# 8 range 연산자 (Unity 6 / C# 9 이하 지원)
 *
 * =============================================================================
 */
#endif
