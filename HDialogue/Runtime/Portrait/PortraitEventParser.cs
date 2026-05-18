#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- portrait.* 인라인 이벤트 키 파서. 순수 정적 클래스.
 *
 * 특징 / 지원기능 ::
 * + TryParse(eventKey, out instruction) — 파싱 성공 시 true
 * + 포맷: "portrait.<verb>[@<characterKey>]:<arg1>[,<arg2>...]"
 * + portrait. prefix 없으면 false 반환 (다른 시스템 이벤트 필터)
 *
 * 주의사항 ::
 * 알 수 없는 동사는 false 반환 + HLogger.Warning.
 * 인자 파싱 실패(split 부족)는 CharacterStageDirector._Apply() 에서 처리.
 * =========================================================
 */
#endif

using System;
using HDiagnosis.Logger;

namespace HDialogue {
    public static class PortraitEventParser {
        const string PREFIX = "portrait.";

        public static bool TryParse(string eventKey, out PortraitEventInstruction instruction) {
            instruction = default;
            if (string.IsNullOrEmpty(eventKey) || !eventKey.StartsWith(PREFIX, StringComparison.Ordinal)) {
                return false;
            }

            string body = eventKey.Substring(PREFIX.Length);

            string verbPart;
            string characterKey = string.Empty;
            string argsPart = string.Empty;

            int colonIdx = body.IndexOf(':');
            string head = colonIdx >= 0 ? body.Substring(0, colonIdx) : body;
            if (colonIdx >= 0) argsPart = body.Substring(colonIdx + 1);

            int atIdx = head.IndexOf('@');
            if (atIdx >= 0) {
                verbPart = head.Substring(0, atIdx);
                characterKey = head.Substring(atIdx + 1);
            } else {
                verbPart = head;
            }

            if (!_TryParseVerb(verbPart, out PortraitVerb verb)) {
                HLogger.Warning($"[PortraitEventParser] 알 수 없는 portrait 동사: '{verbPart}' (eventKey='{eventKey}')");
                return false;
            }

            string[] args = string.IsNullOrEmpty(argsPart)
                ? Array.Empty<string>()
                : argsPart.Split(',');

            instruction = new PortraitEventInstruction(verb, characterKey, args);
            return true;
        }

        static bool _TryParseVerb(string raw, out PortraitVerb verb) {
            return Enum.TryParse(raw, ignoreCase: true, out verb);
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 PortraitEventParser 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 4-E — portrait.* 이벤트 키 파싱 단일 책임 클래스
 *
 * # 포맷 예시
 * - "portrait.pose@alice:happy"       → Pose, "alice", ["happy"]
 * - "portrait.show@bob:left,center"   → Show, "bob", ["left","center"]
 * - "portrait.hide@alice:"            → Hide, "alice", []
 * - "portrait.shake:"                 → Shake, "", []
 * - "bgm.play"                        → false (portrait. prefix 없음)
 *
 * # 설계 결정
 * - Enum.TryParse(ignoreCase: true): 대소문자 무관 동사 허용 ("Pose" == "pose")
 * - Array.Empty<string>(): 인자 없는 경우 GC 할당 없는 빈 배열 반환
 *
 * =============================================================================
 */
#endif
