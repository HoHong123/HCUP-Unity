#if UNITY_EDITOR
// =============================================================================
// SnapMode
// =============================================================================
// HGraphNode 의 Shift 스냅 동작 mode.
// NodeSnapSettings.Mode 의 값으로 사용 — 사용자가 Project Settings 에서 선택.
//
// 의미 ::
// Off          / 절대 스냅 안 함. Shift 눌러도 무시.
// OnShiftHold  / Shift 눌린 동안만 실시간 스냅 (milestone §1-2-3 기본 동작).
// Always       / Shift 무관 항상 실시간 스냅 (사용자 옵션).
//
// 적용 ::
// HGraphNode.SetPosition override 의 _ApplySnap 에서 분기 처리.
// =============================================================================

namespace HWindows.Editor.NodeWindow.Settings {
    public enum SnapMode {
        Off,
        OnShiftHold,
        Always
    }
}
#endif

// =============================================================================
// Dev Log
// =============================================================================
// 2026-05-08 (최초 설계) :: Phase 1-E P1E-β 채택
//
//   변경 / SnapMode 3 값 enum 도입.
//   이유 / milestone §1-2-3 의 "Shift 누른 동안만 스냅" 기본 동작 + 사용자 옵션
//          (Always / Off) 을 단일 enum 으로 표현. SnapTiming 같은 별도 enum 분리 회피.
//   결과 / NodeSnapSettings 의 Mode 필드 타입.
//   주의 / 기본값 OnShiftHold 는 NodeSnapSettings 측에서 지정.
// =============================================================================
