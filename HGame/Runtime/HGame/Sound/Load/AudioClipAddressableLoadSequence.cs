using UnityEngine;
using Cysharp.Threading.Tasks;
using HUtil.Data.Load;
using HUtil.Data.Sequence;

namespace HGame.Sound.Load {
    public sealed class AudioClipAddressableLoadSequence :
        BaseLoadSequence<AudioClip>,
        IDataLoad<string, AudioClip> {
        public AudioClipAddressableLoadSequence() : base(DataLoadType.Addressable) { }


        protected override UniTask<AudioClip> _LoadByKeyAsync(string key) {
            // TODO: Addressables µµ¿‘ Ω√
            return UniTask.FromResult<AudioClip>(null);
        }


        // To Addressable Token Path
        protected override string _NormalizeKey(string tokenOrPath) {
            if (string.IsNullOrWhiteSpace(tokenOrPath)) return string.Empty;
            return _TrimExtension(tokenOrPath);
        }
    }
}
