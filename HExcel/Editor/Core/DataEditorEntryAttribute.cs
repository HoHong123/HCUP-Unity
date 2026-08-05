#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * DataEditorWindow 사이드바에 자동 노출할 Loader 클래스 마커.
 *
 * 특징 ::
 * - Label 은 사이드바 표기 겸 Ordinal 정렬 키 (예: "00. HcupLocalization")
 * - TypeCache 스캔 — 다른 어셈블리의 Loader 도 참조 없이 발견됨 (순환 참조 회피)
 *
 * 주의사항 ::
 * - 부착 클래스는 AssetDatabaseInstance<T> 계열이어야 한다 (static Instance 필요)
 * =========================================================
 */
#endif

using System;

namespace HExcel.Core {
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class DataEditorEntryAttribute : Attribute {
        public string Label { get; }

        public DataEditorEntryAttribute(string label) {
            Label = label;
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.07.03 최초 작성
 *
 * # 목적
 * - HUnityLocalization.Editor → HExcel.Editor 참조 방향에서 DataEditorWindow 가
 *   신규 로더를 하드코딩할 수 없음 (순환 참조) → TypeCache 자동 발견으로 전환
 *
 * =============================================================================
 */
#endif
