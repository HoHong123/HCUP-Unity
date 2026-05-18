#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- HDialogue 커스텀 태그 정의 단일 소스.
 *
 * 특징 / 지원기능 ::
 * + EffectTags  : EffectPush/Pop 대상 (shake / wave / rainbow)
 * + PairTags    : 열기/닫기 쌍이 필요한 태그 (effect + silent)
 * + RequiredArgTags : 필수 인자 보유 태그 (sfx / event / voice)
 * + FloatArgTags    : float 인자 태그 (pause / speed)
 * + AllCustomTags   : 전체 커스텀 태그 목록
 * + TmpTags         : TMP 표준 태그 (PassThrough 대상)
 *
 * 주의사항 ::
 * 새 커스텀 태그 추가 시 이 파일만 수정하면 됨.
 * DialogueTagParser / DialogueTextValidator 는 여기에서 읽는다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;

namespace HDialogue {
    public static class DialogueTagRegistry {
        #region TMP 표준 태그
        public static readonly HashSet<string> TmpTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
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

        #region 커스텀 태그 집합
        public static readonly HashSet<string> EffectTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "shake", "wave", "rainbow"
        };

        public static readonly HashSet<string> PairTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "shake", "wave", "rainbow", "silent"
        };

        public static readonly HashSet<string> RequiredArgTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "sfx", "event", "voice"
        };

        public static readonly HashSet<string> FloatArgTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "pause", "speed"
        };

        public static readonly HashSet<string> AllCustomTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "pause", "speed", "speed_end", "shake", "wave", "rainbow",
            "sfx", "event", "silent", "voice"
        };
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.17 (최초 설계) :: DialogueTagRegistry 도입 — 태그 정의 단일 소스

 * # 목적
 * - DialogueTagParser(Runtime)와 DialogueTextValidator(Editor)에 분산된
 *   커스텀 태그 집합 정의를 한 곳으로 집결.
 * - 새 태그 추가 시 이 파일만 수정하면 parser/validator 양쪽에 자동 반영.
 *
 * # 설계 결정
 * - Runtime 레이어 배치: Editor → Runtime 참조는 허용, 역방향 금지.
 *   DialogueTextValidator(Editor)가 이 클래스를 직접 참조 가능.
 * - TmpTags도 이관: DialogueTagParser.TmpTags는 backward-compat 포워딩 프로퍼티로 유지.
 * - EffectTags(parser 내부 private) / PairTags(validator 로컬) 의미 분리 보존:
 *   effect는 EffectPush/Pop 대상, pair는 열기/닫기 짝 추적 대상(silent 포함).
 *
 * =============================================================================
 */
#endif
