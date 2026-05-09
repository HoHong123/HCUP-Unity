#if UNITY_EDITOR
// =============================================================================
// NodeSnapSettings
// =============================================================================
// NodeWindow 시스템의 Snap + Grid 설정 보관 ScriptableSingleton.
// Phase 1-E (2026-05-08) 도입.
//
// 특징 ::
// ScriptableSingleton 단일성 강제 (NodeUIDRegistry 와 동일 패턴).
// ProjectSettings/HCUP/NodeSnapSettings.asset 에 직렬 (프로젝트 일부, 머신 간 공유).
// Project Settings > HCUP > Node Window 페이지 + HGraphWindow Toolbar 사이드패널 양쪽
// 같은 instance 의 SerializedObject 를 공유 — 한쪽 변경이 다른쪽에 자동 반영.
//
// 필드 (P1E-β/γ/δ) ::
// gridUnit  / int [1, 100] / 기본 20  / Shift 스냅 단위 (px, GridBackground minor 정합)
// showGrid  / bool          / 기본 true / GridBackground 표시 토글
// mode      / SnapMode       / 기본 OnShiftHold / Shift 스냅 동작 mode
//
// 사용 예 ::
// int unit = NodeSnapSettings.instance.GridUnit;
// SnapMode mode = NodeSnapSettings.instance.Mode;
//
// 주의사항 ::
// 외부 노출은 getter 만 (직접 set 금지). 변경은 SerializedObject 경유.
// =============================================================================
using UnityEditor;
using UnityEngine;

namespace HWindows.Editor.NodeWindow.Settings {
    [FilePath("ProjectSettings/HCUP/NodeSnapSettings.asset",
              FilePathAttribute.Location.ProjectFolder)]
    public sealed class NodeSnapSettings : ScriptableSingleton<NodeSnapSettings> {
        #region Serialized Fields
        [SerializeField, Range(1, 100)]
        int gridUnit = 20;

        [SerializeField]
        bool showGrid = true;

        [SerializeField]
        SnapMode mode = SnapMode.OnShiftHold;
        #endregion

        #region Public API
        public int GridUnit => gridUnit;
        public bool ShowGrid => showGrid;
        public SnapMode Mode => mode;
        #endregion

        #region Internal - SettingsProvider 측에서만 호출
        internal void Save() => base.Save(true);
        #endregion
    }
}
#endif

// =============================================================================
// Dev Log
// =============================================================================
// 2026-05-08 (최초 설계) :: Phase 1-E P1E-α/β/γ/δ 채택
//
//   변경 / ScriptableSingleton<NodeSnapSettings> 신규. 3 필드 (gridUnit/showGrid/mode).
//   이유 / Phase 1-E 의 "Shift 스냅 + Grid 가시 + Snap mode" 가 NodeWindow 시스템 단일
//          설정 보관 자리. NodeUIDRegistry 와 동일 ScriptableSingleton 패턴 채택 —
//          새 인프라 학습 비용 0.
//   결과 / Project Settings > HCUP > Node Window 페이지 + HGraphWindow Toolbar 사이드패널
//          양쪽 같은 instance 공유. 후속 phase 가 settings 항목 추가할 확장점.
//   주의 / 외부 노출은 getter 만. 사후 필드 추가 시 직렬 default 0 리셋 리스크
//          (CLAUDE.md 전역 규칙 10번 ScriptableObject assets-modify 함정과 같은 분류) —
//          본 phase 에 3 필드 일괄 도입.
// =============================================================================
