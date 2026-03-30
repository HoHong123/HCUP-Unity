namespace HUtil.AssetHandler.Data {
    public enum AssetFetchMode : byte {
        // Runtime Cache, Editor Cache, etc.
        CacheFirst = 0,

        // Local Store, PlayerPrefs, File, etc.
        LocalStoreFirst = 1,
        LocalStoreOnly = 2,

        // Resources, Addressable, Remote, etc.
        SourceFirst = 3,
        SourceOnly = 4,
    }
}
