#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetDatabaseInstance<T> 상속 — GoodsData Import 결과 보관 SO
 *
 * 사용법 :: GoodsTableLoader.ImportData() 호출 후 Rows로 접근
 * 주의사항 :: UNITY_EDITOR 전용. rows는 에디터 직렬화 전용 필드
 * =========================================================
 */
#endif

using System.Collections.Generic;
using UnityEngine;
using HData.NPOI.Core;

namespace HData.NPOI.Samples {
    public class GoodsTable : AssetDatabaseInstance<GoodsTable> {
#if UNITY_EDITOR
        [SerializeField]
        List<GoodsData> rows = new();

        public IReadOnlyList<GoodsData> Rows => rows;

        public void SetRows(List<GoodsData> data) {
            rows = data;
        }
#endif
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.13 최초 작성
 *
 * # 목적
 * - 900000_Goods.xlsx Goods 시트 Import 결과 보관 ScriptableObject
 * - SampleTable 패턴 동일 — GoodsData 타입 특화
 *
 * =============================================================================
 */
#endif
