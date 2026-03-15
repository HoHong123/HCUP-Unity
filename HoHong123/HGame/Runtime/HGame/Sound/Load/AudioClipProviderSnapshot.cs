#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HGame.Sound.Load {
    [Serializable]
    public sealed class AudioClipProviderSnapshot {
        [Serializable]
        public struct Entry {
            public int Id;
            public int Dependency;
            public string Token;

            public bool IsLoaded;

            public AudioClip Clip;

            public string ClipName;
            public float ClipLength;

            public List<string> CatalogNames;
        }

        [Serializable]
        public struct CatalogGroup {
            public string Name;
            public int RefCount;
            public int EntryCount;
            public int LoadedCount;
        }

        public string ProviderName;

        public int TokenCount;
        public int LoadedCount;
        public int EntryCount;

        public List<CatalogGroup> Catalogs = new();
        public List<Entry> Entries = new();
    }
}
#endif