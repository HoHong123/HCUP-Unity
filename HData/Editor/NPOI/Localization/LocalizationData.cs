#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 로컬리제이션 엑셀 1행을 파싱한 임시 데이터 전송 객체(DTO).
 *
 * 특징 ::
 * - HcupLocalizationTableLoader / HUnityLocalizationTableLoader 양 로더의 Import 파이프라인에서 사용 (LocalizationExcelParser 경유)
 * - 언어 필드는 Excel 컬럼명과 1:1 대응 (UID / Korean / English / Japanese / Chinese / Russian)
 *
 * 주의사항 ::
 * - Editor Assembly 전용 (HCUP.HData.NPOI.Editor). 런타임 참조 불가.
 * - 장기 보관 용도 아님 — Import 직후 LocalizationSO 에 기록 후 버려진다.
 * =========================================================
 */
#endif

using System;
using HcupLocalization;

namespace HData.NPOI.Localization {
    [Serializable]
    public class LocalizationData {
        public string uid;
        public string korean;
        public string english;
        public string japanese;
        public string chinese;
        public string russian;

        /// <summary> 지정 언어의 번역 문자열 반환. 미매칭 언어는 SwitchExpressionException 발생. </summary>
        // 기본 arm 없음 — 언어 추가 시 미매칭이 예외로 즉시 드러나게 (조용한 "" 기록 금지)
        public string GetText(LocalizationLanguage language) => language switch {
            LocalizationLanguage.Korean   => korean,
            LocalizationLanguage.English  => english,
            LocalizationLanguage.Japanese => japanese,
            LocalizationLanguage.Chinese  => chinese,
            LocalizationLanguage.Russian  => russian,
        };
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.07.04 GetText 기본 arm 제거 — 미매칭 언어를 예외로 노출
 *
 * # 수정
 * - switch 식 `_ => ""` 기본 arm 제거. 미매칭 LocalizationLanguage 는 SwitchExpressionException 즉시 발생
 * - 조용한 빈 문자열 기록을 막아 언어 추가 시 매핑 누락을 즉시 드러냄 ("에러를 조용히 무시하지 말 것" 규칙 정합)
 *
 * =============================================================================
 * @Jason - PKH 2026.07.04 헤더 서술 현행화
 *
 * # 수정
 * - 특징 섹션의 로더 서술을 HcupLocalizationTableLoader / HUnityLocalizationTableLoader 양 로더 기준으로 갱신
 *
 * =============================================================================
 * @Jason - PKH 2026.07.03 GetText(LocalizationLanguage) 추가
 *
 * # 추가
 * - 언어 → 필드 매핑을 DTO 단일 소스로 통합 (양 로더 공용)
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 최초 작성
 *
 * # 목적
 * - HCUP-2.1.0 Localization Phase 2 — NPOI Loader 내부 파싱 DTO
 * - GoodsData 와 달리 Inspector 표시 목적이 아니므로 HTitle / HSpritePreview 미사용
 *
 * =============================================================================
 */
#endif
