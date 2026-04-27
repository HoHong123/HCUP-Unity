using Cysharp.Threading.Tasks;
using UnityEngine;
using HUtil.Data.Provider;
using HGame.Sound.Core;

namespace HGame.Sound.Load {
    public interface IAudioClipProvider : IDataProvider<int, AudioClip> {
        UniTask PrewarmCatalogAsync(SoundCatalogSO catalog);

        void ReleaseCatalog(SoundCatalogSO catalog);
    }
}
