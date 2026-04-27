#if UNITY_EDITOR
namespace HGame.Sound.Load {
    public interface IAudioClipDiagnostics {
        AudioClipProviderSnapshot CreateSnapshot();
        int PruneUnusedTokens();
    }
}
#endif