using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace HUtil.AssetHandler.Load {
    public interface IAddressableLabelLoader<TAsset> {
        UniTask<IList<TAsset>> LoadAllAsync(string label);
        UniTask<TAsset> LoadFirstAsync(string label);
        UniTask<TAsset> LoadSingleAsync(string label);
        UniTask<TAsset> LoadByIndexAsync(string label, int index);

        bool ReleaseAllByLabel(string label);
        bool ReleaseFirstByLabel(string label);
        bool ReleaseSingleByLabel(string label);
        bool ReleaseByLabelIndex(string label, int index);
        void ReleaseAll();
    }
}
