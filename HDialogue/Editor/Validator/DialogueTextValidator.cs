#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대화 태그 구조 검증기 (Phase 5, 에디터 전용).
 *
 * 특징 / 지원기능 ::
 * + Validate(rawText) : ValidationIssue 목록 반환 — Debug.Log 없음
 * + 검증 항목 : 짝 없는 태그(Error) / 필수 인자 누락(Warning) /
 *               잘못된 float 인자(Warning) / 알 수 없는 태그(Warning) /
 *               sfx 미구현 경고(Warning)
 *
 * 주의사항 ::
 * 모든 태그 집합 정의는 DialogueTagRegistry 단일 소스 참조.
 * 새 커스텀 태그 추가 시 DialogueTagRegistry.cs 만 수정하면 됨.
 * 순수 정적 클래스. 상태 없음.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using System.Globalization;
using HDialogue;

namespace HDialogue.Editor {
    public static class DialogueTextValidator {
        #region Type
        public enum IssueSeverity { Error, Warning }

        public readonly struct ValidationIssue {
            public readonly IssueSeverity Severity;
            public readonly string Message;

            public ValidationIssue(IssueSeverity severity, string message) {
                Severity = severity;
                Message = message;
            }
        }
        #endregion

        #region Public
        public static IReadOnlyList<ValidationIssue> Validate(string rawText) {
            var issues = new List<ValidationIssue>();
            var openPairs = new Stack<string>();

            if (string.IsNullOrEmpty(rawText)) return issues;

            int k = 0;
            while (k < rawText.Length) {
                if (rawText[k] != '<') { k++; continue; }

                int closeIdx = rawText.IndexOf('>', k + 1);
                if (closeIdx < 0) { k++; continue; }

                string tagContent = rawText.Substring(k + 1, closeIdx - k - 1);
                _CheckTag(tagContent, issues, openPairs);
                k = closeIdx + 1;
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

            bool isClosing = first == '/';
            string body = isClosing ? tagContent.Substring(1) : tagContent;

            int eqIdx = body.IndexOf('=');
            string name = (eqIdx >= 0 ? body.Substring(0, eqIdx) : body).Trim().ToLowerInvariant();
            string arg = eqIdx >= 0 ? body.Substring(eqIdx + 1).Trim() : null;

            if (isClosing) {
                // </speed_end> 는 유효한 SpeedReset 닫기 형식
                if (name == "speed_end") return;

                if (DialogueTagRegistry.PairTags.Contains(name)) {
                    if (openPairs.Count == 0 || !string.Equals(openPairs.Peek(), name, StringComparison.OrdinalIgnoreCase))
                        issues.Add(new ValidationIssue(IssueSeverity.Error, $"</{name}>: 대응하는 열기 태그 없음."));
                    else
                        openPairs.Pop();
                } else if (!DialogueTagRegistry.AllCustomTags.Contains(name) && !DialogueTagRegistry.TmpTags.Contains(name)) {
                    issues.Add(new ValidationIssue(IssueSeverity.Warning, $"</{name}>: 알 수 없는 닫기 태그 → TMP PassThrough."));
                }
                return;
            }

            // 열기 태그 — 필수 인자 체크
            if (DialogueTagRegistry.RequiredArgTags.Contains(name)) {
                if (string.IsNullOrEmpty(arg))
                    issues.Add(new ValidationIssue(IssueSeverity.Warning, $"<{name}>: 필수 인자 없음 → 토큰 스킵됩니다."));
                if (name == "sfx")
                    issues.Add(new ValidationIssue(IssueSeverity.Warning, $"<sfx>: 미구현 태그 — 런타임 no-op. Phase 5+ 구현 예정."));
                return;
            }

            // float 인자 체크
            if (DialogueTagRegistry.FloatArgTags.Contains(name)) {
                if (!string.IsNullOrEmpty(arg) &&
                    !float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                    issues.Add(new ValidationIssue(IssueSeverity.Warning, $"<{name}={arg}>: 잘못된 float 인자 → 기본값 사용."));
                return;
            }

            // 쌍 태그 — 열기 추적
            if (DialogueTagRegistry.PairTags.Contains(name)) {
                openPairs.Push(name);
                return;
            }

            // 기타 알려진 커스텀·TMP 태그 → 정상
            if (DialogueTagRegistry.AllCustomTags.Contains(name) || DialogueTagRegistry.TmpTags.Contains(name)) return;

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
 * @Jason - PKH 2026.05.17 (수정) :: sfx 태그 미구현 Warning 추가

 * # 변경
 * - _CheckTag() RequiredArgTags 분기 내에 sfx 전용 Warning 추가.
 *   <sfx=...> 사용 시 인자 유무 무관하게 "미구현 — no-op" Warning 발행.
 *
 * # 이유
 * - AgentReview Warning #9 (2026-05-17 19:13:03).
 * - sfx는 DialogueTagRegistry에 정의된 태그이나 런타임에서 처리되지 않음.
 *   에디터에서 사전 경고 없으면 기획자가 태그를 작성해도 효과가 없는 이유를 알 수 없음.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: DialogueTagRegistry 도입 — 로컬 태그 집합 전량 이관

 * # 변경
 * - pairTags / requiredArgTags / floatArgTags / allCustomTags 로컬 필드 전량 제거.
 * - 모든 참조를 DialogueTagRegistry.PairTags / RequiredArgTags / FloatArgTags /
 *   AllCustomTags / TmpTags 로 교체.
 * - DialogueTagParser.TmpTags 참조 → DialogueTagRegistry.TmpTags 로 교체.
 * - #region 전역 제거 (로컬 필드 없어짐).
 *
 * # 이유
 * - 새 태그 추가 시 DialogueTagRegistry.cs 한 파일만 수정하면 됨.
 *   "태그 추가 시 양쪽 동시 갱신" 주석 조건 해소.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: tmpTags → DialogueTagParser.TmpTags 참조로 교체
 *
 * # 변경
 * - 로컬 `static readonly HashSet<string> tmpTags` 필드 제거.
 * - `tmpTags.Contains(name)` → `DialogueTagParser.TmpTags.Contains(name)` (2곳).
 * - `using HDialogue;` 추가.
 *
 * # 이유
 * - TMP 태그 집합을 두 파일에 중복 정의 → DialogueTagParser가 단일 소스.
 *   태그 추가 시 한 곳만 수정하면 됨.
 *
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
 * - Parse() 재사용 대신 별도 검증 로직: Parse()는 HLogger를 뱉으므로 에디터 로그 오염.
 * - ValidationIssue / IssueSeverity를 public 중첩 타입으로 선언 (컨벤션 예외: 3줄 데이터 타입).
 *
 * =============================================================================
 */
#endif
