using System;
using System.Collections.Generic;
using UnityEngine;

namespace HGame.Sound.Core {
    [CreateAssetMenu(
        menuName = "HCUP/Audio/Sound Catalog Policy",
        fileName = "SoundCatalogPolicy")]
    public sealed class SoundCatalogPolicySO : ScriptableObject {
        #region Nested
        [Serializable]
        public struct UidRange {
            public SoundMajorCategory Major;
            public int MinInclusive;
            public int MaxExclusive;

            public bool Contains(int uid) => uid >= MinInclusive && uid < MaxExclusive;
        }

        [Serializable]
        public struct FolderMidMapping {
            public SoundMajorCategory Major;
            public string FolderName;
        }
        #endregion

        #region Unity IMGUI
        [SerializeField]
        List<UidRange> uidRanges = new();
        [SerializeField]
        List<FolderMidMapping> folderMidMappings = new();
        #endregion

        #region Properties
        public IReadOnlyList<UidRange> UidRanges => uidRanges;
        public IReadOnlyList<FolderMidMapping> FolderMidMappings => folderMidMappings;
        #endregion

        #region Public API
        public bool TryGetUidRange(SoundMajorCategory major, out UidRange range) {
            for (int k = 0; k < uidRanges.Count; k++) {
                if (uidRanges[k].Major != major) continue;
                range = uidRanges[k];
                return true;
            }

            range = default;
            return false;
        }
        #endregion

        #region Validation
        private void OnValidate() {
            for (int k = 0; k < uidRanges.Count; k++) {
                var range = uidRanges[k];
                if (range.MaxExclusive < range.MinInclusive)
                    range.MaxExclusive = range.MinInclusive;
            }
        }
        #endregion
    }
}
