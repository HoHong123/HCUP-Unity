#if UNITY_EDITOR
namespace HAudio.Load {
    public interface IAudioClipDiagnostics {
        AudioClipProviderSnapshot CreateSnapshot();
        int PruneUnusedTokens();
    }
}
#endif