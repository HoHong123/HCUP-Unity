#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대화 태그 구조 검증기 (Phase 5, 에디터 전용).
 *
 * 특징 / 지원기능 ::
 * + Validate(rawText) : ValidationIssue 목록 반환 — Debug.Log 없음
 * + 검증 항목 : 짝 없는 태그(Error) / 필수 인자 누락(Warning) /
 *               잘못된 float 인자(Warning) / 알 수 없는 태그(Warning)
 *
 * 주의사항 ::
 * Runtime DialogueTagParser와 검증 로직이 일치해야 함 — 태그 추가 시 양쪽 동시 갱신.
 * 순수 정적 클래스. 상태 없음.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using System.Globalization;

namespace HDialogue.Editor {
    public static class DialogueTextValidator {
        #region Type
        public enum IssueSeverity { Error, Warning }

        public readonly struct ValidationIssue {
            public readonly IssueSeverity Severity;
            public readonly string        Message;

            public ValidationIssue(IssueSeverity severity, string message) {
                Severity = severity;
                Message  = message;
            }
        }
        #endregion

        #region 전역
        static readonly HashSet<string> pairTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "shake", "wave", "rainbow", "silent"
        };

        static readonly HashSet<string> requiredArgTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "sfx", "event", "voice"
        };

        static readonly HashSet<string> floatArgTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "pause", "speed"
        };

        static readonly HashSet<string> allCustomTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "pause", "speed", "speed_end", "shake", "wave", "rainbow",
            "sfx", "event", "silent", "voice"
        };

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

        #region Public
        public static IReadOnlyList<ValidationIssue> Validate(string rawText) {
            var issues    = new List<ValidationIssue>();
            var openPairs = new Stack<string>();

            if (string.IsNullOrEmpty(rawText)) return issues;

            int i = 0;
            while (i < rawText.Length) {
                if (rawText[i] != '<') { i++; continue; }

                int closeIdx = rawText.IndexOf('>', i + 1);
                if (closeIdx < 0) { i++; continue; }

                string tagContent = rawText.Substring(i + 1, closeIdx - i - 1);
                _CheckTag(tagContent, issues, openPairs);
                i = closeIdx + 1;
            }

            while (openPairs.Count > 0) {
                string name = openPairs.Pop();
                issues.Add(new ValidationIssue(IssueSeverity.Error,
                    $"<{name}>: 닫기 태그 누락 — 라인 끝에서 자동 닫힘."));
            }

            return issues;
        }
        #endregion

        #region Private
        private static void _CheckTag(string tagContent, List<ValidationIssue> issues, Stack<string> openPairs) {
            if (string.IsNullOrEmpty(tagContent)) return;

            char first = tagContent[0];
            // Hex 컬러 단축 태그 → 검증 불필요
            if (first == '#' || (first == '/' && tagContent.Length > 1 && tagContent[1] == '#')) return;

            bool   isClosing = first == '/';
            string body      = isClosing ? tagContent.Substring(1) : tagContent;

            int    eqIdx = body.IndexOf('=');
            string name  = (eqIdx >= 0 ? body.Substring(0, eqIdx) : body).Trim().ToLowerInvariant();
            string arg   = eqIdx >= 0 ? body.Substring(eqIdx + 1).Trim() : null;

            if (isClosing) {
                // </speed_end> 는 유효한 SpeedReset 닫기 형식
                if (name == "speed_end") return;

                if (pairTags.Contains(name)) {
                    if (openPairs.Count == 0 || !string.Equals(openPairs.Peek(), name, StringComparison.OrdinalIgnoreCase))
                        issues.Add(new ValidationIssue(IssueSeverity.Error, $"</{name}>: 대응하는 열기 태그 없음."));
                    else
                        openPairs.Pop();
                } else if (!allCustomTags.Contains(name) && !tmpTags.Contains(name)) {
                    issues.Add(new ValidationIssue(IssueSeverity.Warning, $"</{name}>: 알 수 없는 닫기 태그 → TMP PassThrough."));
                }
                return;
            }

            // 열기 태그 — 필수 인자 체크
            if (requiredArgTags.Contains(name)) {
                if (string.IsNullOrEmpty(arg))
                    issues.Add(new ValidationIssue(IssueSeverity.Warning, $"<{name}>: 필수 인자 없음 → 토큰 스킵됩니다."));
                return;
            }

            // float 인자 체크
            if (floatArgTags.Contains(name)) {
                if (!string.IsNullOrEmpty(arg) &&
                    !float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                    issues.Add(new ValidationIssue(IssueSeverity.Warning, $"<{name}={arg}>: 잘못된 float 인자 → 기본값 사용."));
                return;
            }

            // 쌍 태그 — 열기 추적
            if (pairTags.Contains(name)) {
                openPairs.Push(name);
                return;
            }

            // 기타 알려진 커스텀·TMP 태그 → 정상
            if (allCustomTags.Contains(name) || tmpTags.Contains(name)) return;

            // 알 수 없는 태그
            issues.Add(new ValidationIssue(IssueSeverity.Warning, $"<{tagContent}>: 알 수 없는 태그 → TMP PassThrough."));
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 HUI.Editor.TextUI → HDialogue.Editor 패키지 이관
 *
 * # 변경
 * - namespace HUI.Editor.TextUI → HDialogue.Editor
 * - HUI/Editor/HUI/Text/Dialogue/ → HDialogue/Editor/ 이관
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueTextValidator Phase 5 베이스 코드 생성
 *
 * # 목적
 * - DialogueTagParser의 런타임 경고 로직을 에디터에서 미리 검증하는 정적 도구.
 * - Debug.Log 없이 ValidationIssue 목록만 반환 → 에디터 윈도우에서 표시.
 *
 * # 설계 결정
 * - Parse() 재사용 대신 별도 검증 로직: Parse()는 Debug.Log를 뱉으므로 에디터 로그 오염.
 * - ValidationIssue / IssueSeverity를 public 중첩 타입으로 선언 (컨벤션 예외: 3줄 데이터 타입).
 * - tmpTags / allCustomTags 등 DialogueTagParser의 private statics 복제 필요.
 *   태그 추가 시 두 파일 동시 갱신이 필수 — 헤더 주의사항에 명시.
 *
 * =============================================================================
 */
#endif
