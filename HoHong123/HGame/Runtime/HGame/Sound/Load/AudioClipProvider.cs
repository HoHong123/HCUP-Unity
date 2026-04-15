using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using HDiagnosis.Logger;
using HDiagnosis.HDebug;
using HUtil.Data.Load;
using HUtil.Data.Provider;
using HGame.Sound.Core;

namespace HGame.Sound.Load {
    public sealed partial class AudioClipProvider : IAudioClipProvider {
        #region Fields
        readonly DataEndpoint<int, AudioClip> endpoint;
        readonly AudioClipCache cache;

        readonly Dictionary<int, string> tokenTable = new();
        readonly Dictionary<SoundCatalogSO, int> catalogs = new();

        readonly DataLoadType loadType;
        #endregion

        #region Properties
#if UNITY_EDITOR && ODIN_INSPECTOR
        public IReadOnlyDictionary<int, AudioClipCache.Item> Preview => cache.Preview;
#endif
        #endregion

        #region Public - Init
        public AudioClipProvider(DataLoadType load) {
            Assert.IsTrue(load == DataLoadType.Resources || load == DataLoadType.Addressable);

            loadType = load;
            cache = new AudioClipCache();

            IDataLoad<string, AudioClip> stringLoader = load switch {
                DataLoadType.Resources => new AudioClipResourceLoadSequence(),
                DataLoadType.Addressable => new AudioClipAddressableLoadSequence(),
                _ => null
            };

            Assert.IsNotNull(stringLoader);

            var uidLoader = new UidToStringConvertor<AudioClip>(stringLoader, _ResolveToken);
            var handler = new DataLoader<int, AudioClip>(uidLoader);

            endpoint = new DataEndpoint<int, AudioClip>(
                handler: handler,
                cacheStore: cache,
                loadGate: new SharedLoadGate<int, AudioClip>(),
                dataStore: null
            );
        }
        #endregion

        #region Public - Prewarm
        public async UniTask PrewarmIdAsync(int id) => await GetOrLoadAsync(id);
        public async UniTask PrewarmIdAsync(string id) => await PrewarmIdAsync(int.Parse(id));

        public async UniTask PrewarmCatalogAsync(SoundCatalogSO catalog) {
            Assert.IsNotNull(catalog);
            if (!catalog) return;

            if (catalogs.TryGetValue(catalog, out var count)) {
                catalogs[catalog] = count + 1;
                return;
            }

            catalogs.Add(catalog, 1);

            List<UniTask> tasks = new();
            foreach (var entry in catalog.Entries) {
                int uid = entry.Key.Id;
                if (uid <= 0) {
#if UNITY_EDITOR
                    HDebug.StackTraceError($"[AudioClipProvider] invalid UID :: {uid}", 10);
#endif
                    continue;
                }

                if (!tokenTable.ContainsKey(uid)) tokenTable.Add(uid, entry.Token);
                tasks.Add(PrewarmIdAsync(uid));
            }

            await UniTask.WhenAll(tasks);
        }
        #endregion

        #region Public - Get
        public bool TryGet(int id, out AudioClip clip) => cache.TryGet(id, out clip);
        public bool TryGet(string id, out AudioClip clip) => TryGet(int.Parse(id), out clip);

        public UniTask<AudioClip> GetOrLoadAsync(int id) => _GetOrLoadAsync(id, null, false, null);
        public UniTask<AudioClip> GetOrLoadAsync(int id, object owner) => _GetOrLoadAsync(id, owner, false, null);

        private async UniTask<AudioClip> _GetOrLoadAsync(
            int id,
            object owner,
            bool allowFallback = false,
            Func<string> fallbackTokenProvider = null) {

            var clip = await endpoint.GetAsync(
                key: id,
                loadType: loadType,
                useCache: true,
                forceRefresh: false,
                owner: owner);

            if (clip) return clip;

            HLogger.Error($"[AudioClipProvider] Missing token. uid={id}");

            if (!allowFallback || fallbackTokenProvider == null) {
#if UNITY_EDITOR
                HDebug.StackTraceError($"[AudioClipProvider] Fallback not allowed. allowFallback={allowFallback}", 10);
#endif
                return null;
            }

            var token = fallbackTokenProvider.Invoke();
            if (string.IsNullOrWhiteSpace(token)) return null;

            tokenTable[id] = token;

            return await endpoint.GetAsync(
                key: id,
                loadType: loadType,
                useCache: true,
                forceRefresh: true,
                owner: owner);
        }
        #endregion

        #region Public - Release
        public void ReleaseId(int id) => cache.Release(id);
        public void ReleaseId(int id, object owner) => cache.Release(id, owner);
        public void ReleaseId(string id) => ReleaseId(int.Parse(id));
        public int ReleaseOwner(object owner) => cache.ReleaseOwner(owner);

        public void ReleaseCatalog(SoundCatalogSO catalog) {
            Assert.IsNotNull(catalog);
            if (!catalog) return;
            if (!catalogs.TryGetValue(catalog, out var count)) return;

            if (--count > 0) {
                catalogs[catalog] = count;
                return;
            }

            catalogs.Remove(catalog);

            foreach (var entry in catalog.Entries) {
                int uid = entry.Key.Id;
                if (uid <= 0) continue;

                ReleaseId(uid);
                if (tokenTable.ContainsKey(uid) && !cache.TryGet(uid, out _))
                    tokenTable.Remove(uid);
            }
        }
        #endregion

        #region Public - Prune
        public void Prune() {
            cache.Prune();
        }
        #endregion

        #region Public - Clear
        public void Clear() {
            cache.Clear();
        }
        #endregion

        #region Private - Path Parsing
        private string _ResolveToken(int uid) {
            if (tokenTable.TryGetValue(uid, out var token) && !string.IsNullOrWhiteSpace(token))
                return token;
            return string.Empty;
        }
        #endregion
    }
}
