using Cysharp.Threading.Tasks;
using UnityEngine;
using HUtil.Data.Provider;
using HAudio.Core;

namespace HAudio.Load {
    public interface IAudioClipProvider : IDataProvider<int, AudioClip> {
        UniTask PrewarmCatalogAsync(SoundCatalogSO catalog);

        void ReleaseCatalog(SoundCatalogSO catalog);
    }
}
