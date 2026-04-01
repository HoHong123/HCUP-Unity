#if UNITY_EDITOR
/* =========================================================
 * Recycle ScrollView 시스템에서 사용하는 Cell View의 베이스 클래스입니다.
 * 재사용 가능한 UI 셀(View) 구조를 정의하기 위한 추상 베이스 클래스입니다.
 *
 * 특징 ::
 * 1. Generic 기반 CellData 바인딩 구조
 * 2. RecycleView 패턴에 맞춘 Bind / Dispose 인터페이스 제공
 * 3. Odin Inspector 사용 여부에 따라 SerializedMonoBehaviour 지원
 *
 * 주의사항 ::
 * Cell은 재사용되므로 Bind 시 모든 UI 상태를 반드시 초기화해야 합니다.
 * =========================================================
 */
#endif

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using UnityEngine;
#endif

namespace HUI.ScrollView {
    public abstract class BaseRecycleCellView<TCellData> :
#if ODIN_INSPECTOR
        SerializedMonoBehaviour
#else
        MonoBehaviour
#endif
        where TCellData : class {
        public abstract void Bind(TCellData data);
        public abstract void Dispose();
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH 2026.03.10
 *
 * 주요 기능 ::
 * Bind(TCellData data)
 *  + 셀 데이터 바인딩
 * Dispose()
 *  + 셀 반환 시 정리 로직
 *
 * 사용 구조 ::
 * BaseRecycleCellView<TCellData>
 *     └ ItemCellView
 *     └ RankCellView
 *     └ ShopCellView
 *
 * 사용법 ::
 * class RankCellView : BaseRecycleCellView<RankData> {
 *     public override void Bind(RankData data) {
 *         rankTxt.text = data.rank.ToString();
 *     }
 *
 *     public override void Dispose() {
 *         // 리사이클 시 정리 로직
 *     }
 * }
 *
 * 구조 ::
 * TCellData
 *  + Cell UI에 전달되는 데이터 모델
 *
 * 기타 ::
 * ScrollViewCell 재사용 패턴을 위한 베이스 View 클래스입니다.
 * =========================================================
 */
#endif