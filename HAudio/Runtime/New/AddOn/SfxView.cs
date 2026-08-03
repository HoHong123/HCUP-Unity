using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR && ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using HAudio.Core;

namespace HAudio.AddOn {
    [Serializable]
    public sealed class SfxView {
        #region IMGUI Fields
#if UNITY_EDITOR && ODIN_INSPECTOR
        [Title("Catalogs")]
        [ListDrawerSettings]
#endif
        [SerializeField]
        List<SoundCatalogSO> catalogs = new();

        public IReadOnlyList<SoundCatalogSO> Catalogs => catalogs;
        #endregion

        #region ======== Editor Only ========
#if UNITY_EDITOR
#if ODIN_INSPECTOR
        [Title("Preview (Editor Only)")]
        [ReadOnly]
        [ListDrawerSettings(IsReadOnly = true)]
#endif
        [SerializeField]
        List<string> previews = new();

        public void EditorRebuildPreview() {
            previews ??= new List<string>();
            previews.Clear();

            if (catalogs == null || catalogs.Count < 1) return;
            HashSet<string> usedTokens = new(System.StringComparer.Ordinal);

            for (int k = 0; k < catalogs.Count; k++) {
                var catalog = catalogs[k];
                if (!catalog)
                    continue;

                var entries = catalog.Entries;
                if (entries == null)
                    continue;

                for (int j = 0; j < entries.Count; j++) {
                    var entry = entries[j];
                    if (entry == null)
                        continue;

                    string token = entry.Token;
                    if (string.IsNullOrWhiteSpace(token)) continue;
                    if (!usedTokens.Add(token)) continue;

                    previews.Add(token);
                }
            }

            previews.Sort(System.StringComparer.Ordinal);
        }
#endif
#endregion
    }
}
