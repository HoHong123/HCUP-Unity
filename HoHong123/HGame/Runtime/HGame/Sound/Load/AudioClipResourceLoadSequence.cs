using Cysharp.Threading.Tasks;
using HUtil.Data.Load;
using HUtil.Data.Sequence;
using System;
using UnityEngine;

namespace HGame.Sound.Load {
    public sealed class AudioClipResourceLoadSequence :
        ResourcesLoadSequence<AudioClip>, 
        IDataLoad<string, AudioClip> {
        #region Const
        const string ASSETS_ROOT = "Assets/";
        const string RESOURCE_ROOT = "Assets/Resources/";
        #endregion

        public AudioClipResourceLoadSequence() : base(string.Empty) { }


        protected override UniTask<AudioClip> _LoadByKeyAsync(string key) =>
            UniTask.FromResult(Resources.Load<AudioClip>(key));

        // To Resource Path
        protected override string _NormalizeKey(string tokenOrPath) {
            if (string.IsNullOrWhiteSpace(tokenOrPath))
                return string.Empty;

            if (!tokenOrPath.StartsWith(ASSETS_ROOT, StringComparison.OrdinalIgnoreCase))
                return _TrimExtension(tokenOrPath);

            if (tokenOrPath.StartsWith(RESOURCE_ROOT, StringComparison.OrdinalIgnoreCase))
                return _TrimExtension(tokenOrPath.Substring(RESOURCE_ROOT.Length));

            return string.Empty;
        }
    }
}
