using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using HGame.Sound.Core;

namespace HGame.Sound.AddOn {
    [Serializable]
    public sealed class SfxView {
        #region Fields
        [Title("Catalogs")]
        [SerializeField][ListDrawerSettings]
        [OnValueChanged("_EditorRebuildPreview", includeChildren: true)]
        List<SoundCatalogSO> catalogs = new();

        public IReadOnlyList<SoundCatalogSO> Catalogs => catalogs;
        #endregion

        #region ======== Editor Only ========
#if UNITY_EDITOR
        [Title("Preview (Editor Only)")]
        [SerializeField]
        [ListDrawerSettings(IsReadOnly = true)]
        List<EntryPreview> previews = new();

        [Serializable]
        public sealed class EntryPreview {
            [HideLabel][ReadOnly]
            [HorizontalGroup("Row", Width = 0.3f), LabelWidth(25)]
            public int Id;

            [HideLabel][ReadOnly]
            [HorizontalGroup("Row", Width = 0.7f), LabelWidth(75)]
            public AudioClips Clip;
        }

        private void _EditorRebuildPreview() {
            previews ??= new List<EntryPreview>();
            previews.Clear();

            if (catalogs == null || catalogs.Count < 1) return;

            HashSet<int> used = new();

            for (int k = 0; k < catalogs.Count; k++) {
                var catalog = catalogs[k];
                if (!catalog) continue;

                var entries = catalog.Entries;
                if (entries == null) continue;

                for (int j = 0; j < entries.Count; j++) {
                    var entry = entries[j];
                    if (entry == null) continue;

                    int uid = entry.Key.Id;
                    if (uid <= 0) continue;
                    if (!used.Add(uid)) continue;

                    AudioClips clipEnum = default;
                    if (Enum.IsDefined(typeof(AudioClips), uid)) {
                        clipEnum = (AudioClips)Enum.ToObject(typeof(AudioClips), uid);
                    }

                    previews.Add(new EntryPreview {
                        Id = uid,
                        Clip = clipEnum
                    });
                }
            }

            previews.Sort((a, b) => a.Id.CompareTo(b.Id));
        }
#endif
        #endregion
    }
}
