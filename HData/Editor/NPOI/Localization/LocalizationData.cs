#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 로컬리제이션 엑셀 1행을 파싱한 임시 데이터 전송 객체(DTO).
 *
 * 특징 ::
 * - LocalizationTableLoader.ImportData() 내부에서만 사용
 * - 언어 필드는 Excel 컬럼명과 1:1 대응 (UID / Korean / English / Japanese / Chinese / Russian)
 *
 * 주의사항 ::
 * - Editor Assembly 전용 (HCUP.HData.NPOI.Editor). 런타임 참조 불가.
 * - 장기 보관 용도 아님 — Import 직후 LocalizationSO 에 기록 후 버려진다.
 * =========================================================
 */
#endif

using System;

namespace HData.NPOI.Localization {
    [Serializable]
    public class LocalizationData {
        public string uid;
        public string korean;
        public string english;
        public string japanese;
        public string chinese;
        public string russian;
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
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
