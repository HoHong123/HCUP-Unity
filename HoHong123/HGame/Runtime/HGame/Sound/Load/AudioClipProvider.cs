using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using HUtil.Logger;
using HUtil.Diagnosis;
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

        #region ======== Init ========
        public AudioClipProvider(DataLoadType load) {
            Assert.IsTrue(load == DataLoadType.Resources || load == DataLoadType.Addressable);

            loadType = load;
            cache = new AudioClipCache();

            // string 기반 로더
            IDataLoad<string, AudioClip> stringLoader = load switch {
                DataLoadType.Resources => new AudioClipResourceLoadSequence(),
                DataLoadType.Addressable => new AudioClipAddressableLoadSequence(),
                _ => null
            };

            Assert.IsNotNull(stringLoader);

            // int 기반 로더
            var uidLoader = new UidToStringConvertor<AudioClip>(stringLoader, _ResolveToken);
            var handler = new DataLoader<int, AudioClip>(uidLoader);

            endpoint = new DataEndpoint<int, AudioClip>(
                handler: handler,
                cacheStore: cache,
                loadGate: new SharedLoadGate<int, AudioClip>(),
                dataStore: null // AudioClip은 저장 불필요
            );
        }
        #endregion

        #region ======== Prewarm ========
        public async UniTask PrewarmIdAsync(int id) => await GetOrLoadAsync(id);
        public async UniTask PrewarmIdAsync(string id) => await PrewarmIdAsync(int.Parse(id));
        public async UniTask PrewarmCatalogAsync(SoundCatalogSO catalog) {
            Assert.IsNotNull(catalog);
            if (!catalog) return;

            if (catalogs.TryGetValue(catalog, out var cnt)) {
                catalogs[catalog] = cnt + 1;
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

        #region ======== Get ========
        public bool TryGet(int id, out AudioClip clip) => cache.TryGet(id, out clip);
        public bool TryGet(string id, out AudioClip clip) => TryGet(int.Parse(id), out clip);

        public UniTask<AudioClip> GetOrLoadAsync(int id) => _GetOrLoadAsync(id, false, null);
        private async UniTask<AudioClip> _GetOrLoadAsync(
            int id,
            bool allowFallback = false,
            Func<string> fallbackTokenProvider = null) {

            // 캐시 우선(Dependency 증가 포함)
            var clip = await endpoint.GetAsync(
                key: id,
                loadType: loadType,
                useCache: true,
                forceRefresh: false);

            if (clip) return clip;

            // token이 없으면 fallback
            HLogger.Error($"[AudioClipProvider] Missing token. uid={id}");

            if (!allowFallback || fallbackTokenProvider == null) {
#if UNITY_EDITOR
                HDebug.StackTraceError($"[AudioClipProvider] Fallback not allowed. allowFallback={allowFallback}", 10);
#endif
                return null;
            }

            var token = fallbackTokenProvider.Invoke();
            if (string.IsNullOrWhiteSpace(token)) return null;

            // fallback은 “임시 토큰 주입”으로 처리 = UID 토큰 테이블에 넣고 로드
            tokenTable[id] = token;

            // 재시도 = 캐시 사용
            return await endpoint.GetAsync(
                key: id,
                loadType: loadType,
                useCache: true,
                forceRefresh: true);
        }
        #endregion

        #region ======== Release ========
        public void ReleaseId(int id) => cache.Release(id);
        public void ReleaseId(string id) => ReleaseId(int.Parse(id));
        public void ReleaseCatalog(SoundCatalogSO catalog) {
            Assert.IsNotNull(catalog);
            if (!catalog) return;
            if (!catalogs.TryGetValue(catalog, out var cnt)) return;

            if (--cnt > 0) {
                catalogs[catalog] = cnt;
                return;
            }

            catalogs.Remove(catalog);

            foreach (var entry in catalog.Entries) {
                int uid = entry.Key.Id;
                if (uid <= 0) continue;
                ReleaseId(uid);
                // 더 이상 캐시에 없으면 token도 제거
                if (tokenTable.ContainsKey(uid) && !cache.TryGet(uid, out _))
                    tokenTable.Remove(uid);
            }
        }
        #endregion

        #region ======== Prune ========
        public void Prune() {
            cache.Prune();
            // TODO :: Save에 Prune이 있다면 적용
        }
        #endregion

        #region ======== Clear ========
        public void Clear() {
            cache.Clear();
            // 필요에 따라 Save의 Clear 구현
        }
        #endregion

        #region ======== Path Parsing ========
        private string _ResolveToken(int uid) {
            if (tokenTable.TryGetValue(uid, out var token) && 
                !string.IsNullOrWhiteSpace(token))
                return token;
            return string.Empty;
        }
        #endregion
    }
}
