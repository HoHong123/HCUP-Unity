#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Recycle ScrollView 의 재사용 Cell View 베이스 추상 클래스. Generic CellData 바인딩 + Bind/Dispose 계약.
 *
 * 주요 기능 ::
 * Bind(TCellData data) — 풀에서 꺼낼 때 데이터 바인딩.
 * Dispose () — 풀로 반환 시 정리 로직.
 *
 * 사용법 ::
 *   class RankCellView : BaseRecycleCellView<RankData> {
 *       public override void Bind(RankData data) { rankTxt.text = data.rank.ToString(); }
 *       public override void Dispose() { "리사이클 시 정리" }
 *   }
 *
 * 주의 ::
 * Cell 은 풀에서 재사용되므로 Bind 시 모든 UI 상태를 반드시 초기화. 직전 셀의 잔존 상태가
 * 시각적으로 새 데이터에 섞이는 함정 회피.
 *
 * 구조 ::
 * Odin Inspector 사용 여부에 따라 SerializedMonoBehaviour / MonoBehaviour 분기.
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
 * Dev Log
 * =========================================================
 *
 * =========================================================
 * 2026-04-26 (수정) :: 헤더 형틀 통합 + Dev Log 형식 도입
 * =========================================================
 * 변경 ::
 * 기존 헤더 (상단 도입+특징+주의사항 + 하단 주요기능/사용 구조/사용법/구조/기타) 를 한 곳에
 * 통합하여 §11 형틀 통일. 하단 Dev Log 영역 추가. 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드.
 *
 * 이유 ::
 * 글로벌 CLAUDE.md §11 룰 일괄 적용.
 *
 * =========================================================
 * 2026-04-25 (최초 설계, @Jason - PKH 2026.03.10) :: BaseRecycleCellView 초기 구현
 * =========================================================
 * Generic 기반 CellData 바인딩 구조 + RecycleView 패턴에 맞춘 Bind/Dispose 인터페이스.
 * Odin Inspector 사용 여부에 따라 SerializedMonoBehaviour 지원 (조건부 컴파일).
 *
 * 사용 구조 ::
 * BaseRecycleCellView<TCellData>
 *     └ ItemCellView
 *     └ RankCellView
 *     └ ShopCellView
 *
 * Cell 이 풀에서 재사용되는 점이 핵심 — 사용자 도메인 코드는 Bind 안에서 반드시 모든 UI
 * 상태를 새 데이터로 초기화해야 함. 잔존 상태 (직전 데이터의 텍스트 / 색상 / 활성화 등) 가
 * 새 데이터에 섞이면 시각적 버그 발생.
 * =========================================================
 */
#endif
