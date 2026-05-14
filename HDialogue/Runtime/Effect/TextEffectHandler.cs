#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- HCUP-2.2.0 TMP 버텍스 효과 핸들러 (Phase 3).
 *
 * 특징 / 지원기능 ::
 * + SetEffectRanges(IReadOnlyList<TextEffectRange>) : 효과 범위 등록
 * + ClearEffects()                                   : 효과 초기화
 * + shake   : 매 프레임 랜덤 오프셋 — 글자 진동
 * + wave    : 시간 기반 Sin 파도 (글자별 위상 오프셋)
 * + rainbow : 시간 기반 Hue 순환 색상 (글자별 위상 오프셋)
 *
 * 주의사항 ::
 * ForceMeshUpdate() 매 프레임 호출 — 효과 없을 때 조기 리턴으로 비용 회피.
 * maxVisibleCharacters 초과 글자(미공개)는 처리 스킵.
 * DialogueTextController와 동일 GameObject에 부착. TMP_Text 슬롯 별도 배선 필요.
 * =========================================================
 */
#endif

using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HDialogue {
    public sealed class TextEffectHandler : MonoBehaviour {
        #region 변수
        [SerializeField]
        TMP_Text tmpText;

        readonly List<TextEffectRange> effectRanges = new List<TextEffectRange>();
        bool hasEffects;

        const float SHAKE_AMPLITUDE    = 2f;
        const float WAVE_AMPLITUDE     = 3f;
        const float WAVE_SPEED         = 2f;
        const float WAVE_CHAR_PHASE    = 0.5f;
        const float RAINBOW_SPEED      = 0.5f;
        const float RAINBOW_CHAR_PHASE = 0.1f;
        #endregion

        #region Unity Life Cycle
        private void Update() {
            if (!hasEffects || tmpText == null) return;

            tmpText.ForceMeshUpdate();
            TMP_TextInfo textInfo   = tmpText.textInfo;
            int          maxVisible = tmpText.maxVisibleCharacters;

            bool vertexModified = false;
            bool colorModified  = false;

            for (int k = 0; k < effectRanges.Count; k++) {
                TextEffectRange range = effectRanges[k];

                for (int i = range.StartCharIndex; i < range.EndCharIndex && i < textInfo.characterCount; i++) {
                    if (i >= maxVisible) { break; }

                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                    if (!charInfo.isVisible) { continue; }

                    int meshIdx = charInfo.materialReferenceIndex;
                    int vertIdx = charInfo.vertexIndex;

                    switch (range.EffectName) {
                        case "shake":
                            _ApplyShake(textInfo, meshIdx, vertIdx);
                            vertexModified = true;
                            break;
                        case "wave":
                            _ApplyWave(textInfo, meshIdx, vertIdx, i);
                            vertexModified = true;
                            break;
                        case "rainbow":
                            _ApplyRainbow(textInfo, meshIdx, vertIdx, i);
                            colorModified = true;
                            break;
                    }
                }
            }

            if (!vertexModified && !colorModified) return;

            TMP_VertexDataUpdateFlags flags = TMP_VertexDataUpdateFlags.None;
            if (vertexModified) flags |= TMP_VertexDataUpdateFlags.Vertices;
            if (colorModified)  flags |= TMP_VertexDataUpdateFlags.Colors32;
            tmpText.UpdateVertexData(flags);
        }
        #endregion

        #region Public
        public void SetEffectRanges(IReadOnlyList<TextEffectRange> ranges) {
            effectRanges.Clear();
            for (int k = 0; k < ranges.Count; k++)
                effectRanges.Add(ranges[k]);
            hasEffects = effectRanges.Count > 0;
        }

        public void ClearEffects() {
            effectRanges.Clear();
            hasEffects = false;
        }
        #endregion

        #region Private — 효과 적용
        private static void _ApplyShake(TMP_TextInfo textInfo, int meshIdx, int vertIdx) {
            Vector3[] verts  = textInfo.meshInfo[meshIdx].vertices;
            var       offset = new Vector3(
                Random.Range(-SHAKE_AMPLITUDE, SHAKE_AMPLITUDE),
                Random.Range(-SHAKE_AMPLITUDE, SHAKE_AMPLITUDE),
                0f
            );
            verts[vertIdx]     += offset;
            verts[vertIdx + 1] += offset;
            verts[vertIdx + 2] += offset;
            verts[vertIdx + 3] += offset;
        }

        private static void _ApplyWave(TMP_TextInfo textInfo, int meshIdx, int vertIdx, int charIndex) {
            Vector3[] verts  = textInfo.meshInfo[meshIdx].vertices;
            var       offset = new Vector3(
                0f,
                Mathf.Sin(Time.time * WAVE_SPEED + charIndex * WAVE_CHAR_PHASE) * WAVE_AMPLITUDE,
                0f
            );
            verts[vertIdx]     += offset;
            verts[vertIdx + 1] += offset;
            verts[vertIdx + 2] += offset;
            verts[vertIdx + 3] += offset;
        }

        private static void _ApplyRainbow(TMP_TextInfo textInfo, int meshIdx, int vertIdx, int charIndex) {
            Color32[] colors = textInfo.meshInfo[meshIdx].colors32;
            Color32   c      = Color.HSVToRGB(
                Mathf.Repeat(Time.time * RAINBOW_SPEED + charIndex * RAINBOW_CHAR_PHASE, 1f),
                1f, 1f
            );
            colors[vertIdx]     = c;
            colors[vertIdx + 1] = c;
            colors[vertIdx + 2] = c;
            colors[vertIdx + 3] = c;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 HUI.TextUI → HDialogue 패키지 이관
 *
 * # 변경
 * - namespace HUI.TextUI → HDialogue
 * - HUI/Runtime/HUI/Text/Dialogue/ → HDialogue/Runtime/ 이관
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 TextEffectHandler Phase 3 구현
 *
 * # 목적
 * - DialogueTextController의 EffectPush/Pop 범위를 받아 TMP vertex를 직접 조작.
 * - shake / wave / rainbow 3종 효과 구현.
 *
 * # 설계 결정
 * - ForceMeshUpdate() + 버텍스 수정: TMP 공식 vertex 애니메이션 패턴.
 *   ForceMeshUpdate()가 클린 좌표를 재생성하므로 효과가 프레임간 누적되지 않음.
 * - maxVisibleCharacters 체크: 타이프라이터 진행 중 미공개 글자에 rainbow 색상이 적용되면
 *   alpha=0인 글자가 표시되므로 break로 차단.
 * - vertexModified / colorModified 플래그: shake+wave는 Vertices, rainbow는 Colors32.
 *
 * =============================================================================
 */
#endif
