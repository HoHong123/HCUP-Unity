#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 유니티에서 발생하는 드래그 이벤트의 데드락 현상을 방지하기 위해 구현된 전역 스크립트입니다.
 * =========================================================
 */
#endif

using Util.Diagnosis;

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