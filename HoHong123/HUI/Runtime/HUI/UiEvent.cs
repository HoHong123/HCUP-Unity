using HUtil.Diagnosis;

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