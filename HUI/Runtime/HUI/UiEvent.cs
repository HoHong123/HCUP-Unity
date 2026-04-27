#if UNITY_EDITOR
/* =========================================================
 * UI 드래그 상태를 전역적으로 관리하기 위한 간단한 Drag Lock 시스템입니다.
 *
 * 목적 ::
 * UI 시스템에서 동시에 여러 Drag 이벤트가 발생하는 것을 방지하기 위함입니다.
 *
 * 동작 방식 ::
 * Drag Owner 객체를 기록하여 동일 Owner만 Unlock할 수 있도록 설계되었습니다.
 *
 * 주의사항 ::
 * Drag Unlock이 정상적으로 호출되지 않을 경우 ForcedUnlockDrag()로 상태를 초기화할 수 있습니다.
 * =========================================================
 */
#endif

using HDiagnosis.HDebug;

public static class UiEvent {
    public static bool IsDragging { get; private set; } = false;

    private static object dragOwner = null;


    public static bool LockDrag(object owner) {
        if (dragOwner != null) return false;
        dragOwner = owner;
        IsDragging = true;
        return true;
    }

    public static bool UnlockDrag(object owner) {
        if (dragOwner == null || dragOwner != owner) return false;
        dragOwner = null;
        IsDragging = false;
        return true;
    }

    public static void ForcedUnlockDrag() {
        HDebug.ErrorCaller("Force unlock the drag.");
        dragOwner = null;
        IsDragging = false;
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH 2026.03.10
 *
 * 주요 기능 ::
 * LockDrag
 *  + Drag 시작
 * UnlockDrag
 *  + Drag 종료
 * ForcedUnlockDrag
 *  + 강제 Drag 해제
 *
 * 사용법 ::
 * if (UiEvent.LockDrag(this))
 * {
 *     // drag start
 * }
 * UiEvent.UnlockDrag(this);
 *
 * 기타 ::
 * 전역 Drag 상태 관리용 Utility 클래스입니다.
 * =========================================================
 */
#endif