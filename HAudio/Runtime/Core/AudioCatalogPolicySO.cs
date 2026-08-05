using System;
using System.Collections.Generic;
using UnityEngine;

namespace HAudio.Core {
    [CreateAssetMenu(
        menuName = "HCUP/Audio/Sound Catalog Policy",
        fileName = "SoundCatalogPolicy")]
    public sealed class AudioCatalogPolicySO : ScriptableObject {
        #region Nested
        [Serializable]
        public struct FolderMidMapping {
            public AudioMajorCategory Major;
            public string FolderName;
        }
        #endregion

        #region Unity IMGUI
        [SerializeField]
        List<FolderMidMapping> folderMidMappings = new();
        #endregion

        #region Properties
        public IReadOnlyList<FolderMidMapping> FolderMidMappings => folderMidMappings;
        #endregion

    }
}
