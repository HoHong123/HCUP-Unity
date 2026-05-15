#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- HCUP-2.2.0 대화 텍스트 태그 파서 (Phase 1).
 *
 * 특징 / 지원기능 ::
 * + Parse(rawText) : IReadOnlyList<DialogueToken> — 라인당 1회 호출
 * + 커스텀 태그 인식 : pause / speed / speed_end / shake / wave /
 *                      rainbow / sfx / event / silent / voice
 * + TMP 표준 태그 → PassThrough 토큰 (TMP 처리 위임)
 * + 오류 처리 : 잘못된 float 인자 → 기본값 + 경고 / 짝 없는 태그 → 자동 닫기 + 경고
 *
 * 주의사항 ::
 * 상태 없는 순수 정적 클래스. Parse()는 스레드 안전하지 않음 (Stack 지역 변수).
 * Char 토큰은 한 글자당 1개 — 긴 라인은 토큰 수가 많음.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using HDiagnosis.Logger;

namespace HDialogue {
    public static class DialogueTagParser {
        #region 전역 — TMP 표준 태그 집합
        static readonly HashSet<string> tmpTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "b", "i", "u", "s", "sup", "sub",
            "size", "cspace", "mspace", "width",
            "color", "alpha", "gradient", "mark",
            "font", "style", "sprite", "link", "material",
            "indent", "line-height", "line-indent", "margin",
            "pos", "voffset", "rotate", "space",
            "lowercase", "uppercase", "smallcaps",
            "align", "nobr", "noparse", "page", "char"
        };
        #endregion

        #region 전역 — EffectPush/Pop 대상 커스텀 태그
        static readonly HashSet<string> effectTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "shake", "wave", "rainbow"
        };
        #endregion

        #region Public
        public static IReadOnlyList<DialogueToken> Parse(string rawText) {
            if (string.IsNullOrEmpty(rawText))
                return System.Array.Empty<DialogueToken>();

            var tokens = new List<DialogueToken>();
            var openEffects = new Stack<string>();

            int k = 0;
            while (k < rawText.Length) {
                if (rawText[k] != '<') {
                    tokens.Add(DialogueToken.Char(rawText[k]));
                    k++;
                    continue;
                }

                int closeIdx = rawText.IndexOf('>', k + 1);
                if (closeIdx < 0) {
                    tokens.Add(DialogueToken.Char(rawText[k]));
                    k++;
                    continue;
                }

                string tagContent = rawText.Substring(k + 1, closeIdx - k - 1);
                _ParseTag(tagContent, tokens, openEffects);
                k = closeIdx + 1;
            }

            while (openEffects.Count > 0) {
                string unclosed = openEffects.Pop();
                HLogger.Warning($"[DialogueTagParser] Unclosed <{unclosed}> → auto EffectPop at line end.");
                tokens.Add(DialogueToken.EffectPop());
            }

            return tokens;
        }
        #endregion

        #region Private — 태그 파싱
        private static void _ParseTag(string tagContent, List<DialogueToken> tokens, Stack<string> openEffects) {
            if (string.IsNullOrEmpty(tagContent)) return;

            // Hex 컬러 단축 태그 (<#FF0000>) → PassThrough
            char first = tagContent[0];
            if (first == '#' || (first == '/' && tagContent.Length > 1 && tagContent[1] == '#')) {
                tokens.Add(DialogueToken.PassThrough($"<{tagContent}>"));
                return;
            }

            bool isClosing = first == '/';
            string body = isClosing ? tagContent.Substring(1) : tagContent;

            int eqIdx = body.IndexOf('=');
            string name = (eqIdx >= 0 ? body.Substring(0, eqIdx) : body).Trim().ToLowerInvariant();
            string arg = eqIdx >= 0 ? body.Substring(eqIdx + 1).Trim() : null;

            // ── 커스텀 태그 (열기) ──
            if (!isClosing) {
                switch (name) {
                    case "pause":
                        tokens.Add(DialogueToken.Pause(_ParseFloat(arg, 0f, "pause")));
                        return;
                    case "speed":
                        tokens.Add(DialogueToken.SpeedSet(_ParseFloat(arg, 1f, "speed")));
                        return;
                    case "speed_end":
                        tokens.Add(DialogueToken.SpeedReset());
                        return;
                    case "sfx":
                        if (!_RequireArg(arg, "sfx")) return;
                        tokens.Add(DialogueToken.Sfx(arg));
                        return;
                    case "event":
                        if (!_RequireArg(arg, "event")) return;
                        tokens.Add(DialogueToken.Event(arg));
                        return;
                    case "voice":
                        if (!_RequireArg(arg, "voice")) return;
                        tokens.Add(DialogueToken.VoiceSet(arg));
                        return;
                    case "silent":
                        tokens.Add(DialogueToken.SilentPush());
                        return;
                }

                if (effectTags.Contains(name)) {
                    openEffects.Push(name);
                    tokens.Add(DialogueToken.EffectPush(name));
                    return;
                }
            }

            // ── 커스텀 태그 (닫기) ──
            if (isClosing) {
                switch (name) {
                    case "speed_end":
                        tokens.Add(DialogueToken.SpeedReset());
                        return;
                    case "silent":
                        tokens.Add(DialogueToken.SilentPop());
                        return;
                }

                if (effectTags.Contains(name)) {
                    if (openEffects.Count > 0) openEffects.Pop();
                    tokens.Add(DialogueToken.EffectPop());
                    return;
                }
            }

            // ── TMP 표준 태그 → PassThrough ──
            if (tmpTags.Contains(name)) {
                tokens.Add(DialogueToken.PassThrough($"<{tagContent}>"));
                return;
            }

            // ── 알 수 없는 태그 → 경고 + PassThrough ──
            HLogger.Warning($"[DialogueTagParser] Unknown tag <{tagContent}> → PassThrough to TMP.");
            tokens.Add(DialogueToken.PassThrough($"<{tagContent}>"));
        }

        private static float _ParseFloat(string str, float defaultVal, string tagName) {
            if (!string.IsNullOrEmpty(str) &&
                float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                return result;
            HLogger.Warning($"[DialogueTagParser] <{tagName}> invalid float arg '{str}' → using default {defaultVal}.");
            return defaultVal;
        }

        private static bool _RequireArg(string arg, string tagName) {
            if (!string.IsNullOrEmpty(arg)) return true;
            HLogger.Warning($"[DialogueTagParser] <{tagName}> missing required arg → token skipped.");
            return false;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 Debug.LogWarning → HLogger.Warning 교체
 *
 * # 변경
 * - using HDiagnosis.Logger 추가
 * - Parse(), _ParseTag(), _ParseFloat(), _RequireArg() 내 Debug.LogWarning → HLogger.Warning (4곳)
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 HUI.TextUI → HDialogue 패키지 이관
 *
 * # 변경
 * - namespace HUI.TextUI → HDialogue
 * - HUI/Runtime/HUI/Text/Dialogue/ → HDialogue/Runtime/ 이관
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueTagParser Phase 1 구현
 *
 * # 목적
 * - DialogueLine.RawText → IReadOnlyList<DialogueToken> 변환. Phase 2 타이프라이터의 입력.
 *
 * # 설계 결정
 * - 순수 정적 클래스 : 상태 없음. 라인당 1회 Parse() 호출, 결과를 Controller가 캐시.
 * - Stack<string> openEffects : EffectPush/Pop 짝 추적. 라인 끝에서 자동 닫기.
 * - TMP 표준 태그 PassThrough : 파서가 모르는 TMP 태그는 원본 그대로 전달 — TMP가 최종 결정.
 * - float.TryParse InvariantCulture : 로케일 무관 소수점 파싱 ('.' 구분자 강제).
 * - speed_end 이중 인식 : <speed_end> 와 </speed_end> 모두 SpeedReset — 테스트 데이터 호환.
 *
 * =============================================================================
 */
#endif
