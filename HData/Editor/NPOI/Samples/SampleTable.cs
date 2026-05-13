#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetDatabaseInstance<T> 상속 샘플 — ImportData 결과 보관 SO
 *
 * 사용법 ::
 * SampleTableLoader.ImportData() 호출 후 Rows로 접근
 *
 * 주의사항 ::
 * UNITY_EDITOR 전용. _rows는 에디터 직렬화 전용 필드
 * =========================================================
 */
#endif

using System.Collections.Generic;
using UnityEngine;
using HData.NPOI.Core;

namespace HData.NPOI.Samples {
    // AssetDatabaseInstance<T> 상속 예시 — Import 결과 데이터 보관 SO
    [CreateAssetMenu(fileName = "SampleTable", menuName = "HData/Sample/Table")]
    public class SampleTable : AssetDatabaseInstance<SampleTable> {
#if UNITY_EDITOR
        [SerializeField]
        List<SampleData> rows = new();

        public IReadOnlyList<SampleData> Rows => rows;

        public void SetRows(List<SampleData> data) {
            rows = data;
        }
#endif
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.13 컨벤션 정리
 *
 * # 변경
 * - _rows → rows (언더바 접두 적용 오류 수정 → camelCase 복원)
 * - 헤더 주석 추가
 *
 * =============================================================================
 */
#endif
