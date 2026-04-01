using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using HUtil.Data.Cache;
using HUtil.Data.Load;

namespace HUtil.Sample.OwnerTracking {
    [DisallowMultipleComponent]
    public sealed class AddressableOwnerTrackingSample : MonoBehaviour {
        #region SerializedField / Public
        [Serializable]
        public sealed class AssetEntry {
            [SerializeField]
            string key;
            [SerializeField]
            Sprite asset;

            public string Key => key;
            public Sprite Asset => asset;
        }

        [Header("Addressable Mock Source")]
        [SerializeField]
        List<AssetEntry> entries = new();
        [SerializeField]
        string sampleKey = "Sample/Icon";

        [Header("Owners")]
        [SerializeField]
        UnityEngine.Object ownerA;
        [SerializeField]
        UnityEngine.Object ownerB;

        [Header("Automation")]
        [SerializeField]
        bool runScenarioOnStart;
        [SerializeField]
        bool releaseOwnersOnDisable = true;
        [SerializeField]
        bool verboseLogging = true;
        #endregion

        #region Fields
        Dictionary<string, Sprite> assetTable;
        AssetProvider<Sprite> provider;
        BaseDataCache<string, Sprite> cache;
        #endregion

        #region Properties
        public string SampleKey => sampleKey;
        #endregion

        #region Initialization
        private void Awake() {
            _Initialize();
        }
        #endregion

        #region Unity Life Cycle
        private void Start() {
            if (!runScenarioOnStart) return;
            _ = RunScenarioAsync();
        }

        private void OnDisable() {
            if (!releaseOwnersOnDisable) return;
            if (provider == null) return;

            _ReleaseAllOwners();
        }
        #endregion

        #region Public - Sample Control
        public async UniTask<Sprite> LoadOwnerAAsync() =>
            await _LoadOwnerAsync(_ResolveOwner(ownerA), nameof(ownerA));

        public async UniTask<Sprite> LoadOwnerBAsync() =>
            await _LoadOwnerAsync(_ResolveOwner(ownerB), nameof(ownerB));

        public async UniTask<Sprite> LoadAnonymousAsync() {
            var asset = await provider.GetOrLoadAsync(sampleKey);
            _LogState("Anonymous Load");
            return asset;
        }

        public void ReleaseAnonymous() {
            provider.ReleaseId(sampleKey);
            _LogState("Anonymous ReleaseId");
        }

        public int ReleaseOwnerA() => _ReleaseOwner(_ResolveOwner(ownerA), nameof(ownerA));

        public int ReleaseOwnerB() => _ReleaseOwner(_ResolveOwner(ownerB), nameof(ownerB));

        public async UniTask RunScenarioAsync() {
            await LoadOwnerAAsync();
            await LoadOwnerAAsync();
            await LoadOwnerBAsync();
            await LoadAnonymousAsync();

            ReleaseOwnerA();
            ReleaseAnonymous();
            ReleaseOwnerB();
        }
        #endregion

        #region Private - Initialization
        private void _Initialize() {
            assetTable = _BuildAssetTable(entries);
            cache = new BaseDataCache<string, Sprite>();
            provider = new AssetProvider<Sprite>(
                DataLoadType.Addressable,
                new SampleAddressableSpriteLoader(assetTable),
                cache);
        }

        private static Dictionary<string, Sprite> _BuildAssetTable(List<AssetEntry> source) {
            var table = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            if (source == null) return table;

            foreach (var entry in source) {
                if (entry == null) continue;
                if (string.IsNullOrWhiteSpace(entry.Key)) continue;
                if (entry.Asset == null) continue;

                table[entry.Key] = entry.Asset;
            }

            return table;
        }
        #endregion

        #region Private - Load / Release
        private async UniTask<Sprite> _LoadOwnerAsync(object owner, string ownerLabel) {
            var asset = await provider.GetOrLoadAsync(sampleKey, owner);
            _LogState($"{ownerLabel} Load");
            return asset;
        }

        private int _ReleaseOwner(object owner, string ownerLabel) {
            int releasedCount = provider.ReleaseOwner(owner);
            _LogState($"{ownerLabel} ReleaseOwner ({releasedCount})");
            return releasedCount;
        }

        private void _ReleaseAllOwners() {
            provider.ReleaseOwner(_ResolveOwner(ownerA));
            provider.ReleaseOwner(_ResolveOwner(ownerB));
            provider.ReleaseId(sampleKey);
        }
        #endregion

        #region Private - Helper
        private object _ResolveOwner(UnityEngine.Object owner) {
            if (owner != null) return owner;
            return this;
        }

        private void _LogState(string label) {
            if (!verboseLogging) return;

#if UNITY_EDITOR
            int dependency = cache.TryGetDependency(sampleKey);
            int ownerCount = cache.TryGetOwnerCount(sampleKey);
            bool hasAsset = cache.TryGet(sampleKey, out var asset);
            string assetName = hasAsset && asset != null ? asset.name : "null";

            Debug.Log(
                $"[{nameof(AddressableOwnerTrackingSample)}] {label} | Key={sampleKey} | " +
                $"Dependency={dependency} | OwnerCount={ownerCount} | Asset={assetName}",
                this);
#else
            Debug.Log($"[{nameof(AddressableOwnerTrackingSample)}] {label} | Key={sampleKey}", this);
#endif
        }
        #endregion

        #region Private - Nested Class
        private sealed class SampleAddressableSpriteLoader : IDataLoad<string, Sprite> {
            readonly IReadOnlyDictionary<string, Sprite> assetTable;

            public DataLoadType Type => DataLoadType.Addressable;

            public SampleAddressableSpriteLoader(IReadOnlyDictionary<string, Sprite> assetTable) {
                this.assetTable = assetTable ?? throw new ArgumentNullException(nameof(assetTable));
            }

            public UniTask<Sprite> LoadAsync(string key) {
                if (string.IsNullOrWhiteSpace(key))
                    return UniTask.FromResult<Sprite>(null);
                if (!assetTable.TryGetValue(key, out var asset))
                    return UniTask.FromResult<Sprite>(null);
                return UniTask.FromResult(asset);
            }
        }
        #endregion

#if UNITY_EDITOR
        #region Public - Debug
        [ContextMenu("Run Owner Tracking Scenario")]
        public void DebugRunScenario() {
            _ = RunScenarioAsync();
        }

        [ContextMenu("Load Owner A")]
        public void DebugLoadOwnerA() {
            _ = LoadOwnerAAsync();
        }

        [ContextMenu("Load Owner B")]
        public void DebugLoadOwnerB() {
            _ = LoadOwnerBAsync();
        }

        [ContextMenu("Load Anonymous")]
        public void DebugLoadAnonymous() {
            _ = LoadAnonymousAsync();
        }

        [ContextMenu("Release Owner A")]
        public void DebugReleaseOwnerA() {
            ReleaseOwnerA();
        }

        [ContextMenu("Release Owner B")]
        public void DebugReleaseOwnerB() {
            ReleaseOwnerB();
        }

        [ContextMenu("Release Anonymous")]
        public void DebugReleaseAnonymous() {
            ReleaseAnonymous();
        }
        #endregion
#endif
    }
}
