#if UNITY_EDITOR
using System.Collections.Generic;

namespace HGame.Sound.Load {
    public sealed partial class AudioClipProvider : IAudioClipDiagnostics {
        public AudioClipProviderSnapshot CreateSnapshot() {
            var snapshot = new AudioClipProviderSnapshot {
                ProviderName = GetType().Name,
                TokenCount = tokenTable.Count
            };

            // catalog group 정보
            snapshot.Catalogs.Capacity = catalogs.Count;

            // catalogName 매핑
            Dictionary<int, List<string>> uidToCatalogs = new();

            foreach (var kv in catalogs) {
                var catalog = kv.Key;
                if (!catalog) continue;

                int refCount = kv.Value;
                int entryCount = catalog.Entries?.Count ?? 0;
                int loadedCount = 0;
                if (catalog.Entries != null) {
                    foreach (var entry in catalog.Entries) {
                        int uid = entry.Key.Id;
                        if (uid <= 0) continue;
                        if (cache.TryGet(uid, out var clip) && clip) loadedCount++;
                    }
                }

                snapshot.Catalogs.Add(new AudioClipProviderSnapshot.CatalogGroup {
                    Name = catalog.name,
                    RefCount = refCount,
                    EntryCount = entryCount,
                    LoadedCount = loadedCount,
                });

                // 기존 uidToCatalogs 누적 로직
                foreach (var entry in catalog.Entries) {
                    int uid = entry.Key.Id;
                    if (uid <= 0) continue;

                    if (!uidToCatalogs.TryGetValue(uid, out var list)) {
                        list = new List<string>(2);
                        uidToCatalogs.Add(uid, list);
                    }

                    if (!list.Contains(catalog.name)) list.Add(catalog.name);
                }
            }

            foreach (var kv in tokenTable) {
                int id = kv.Key;
                uidToCatalogs.TryGetValue(id, out var catalogList);

                snapshot.Entries.Add(new AudioClipProviderSnapshot.Entry {
                    Id = id,
                    Token = kv.Value,
                    Dependency = cache.TryGetDependency(id),

                    IsLoaded = cache.TryGet(id, out var clip) && clip,
                    Clip = clip,
                    ClipName = clip ? clip.name : string.Empty,
                    ClipLength = clip ? clip.length : 0f,

                    CatalogNames = catalogList ?? new List<string>(0)
                });
            }

            return snapshot;
        }

        /// <summary>
        /// 더 이상 어떤 카탈로그에서도 참조되지 않는 사운드 클립 토큰을 내부 캐시에서 제거하는 정리(clean-up) 기능
        /// </summary>
        public int PruneUnusedTokens() {
            // Dep<=0 && not loaded -> remove token
            // tokenTable을 순회하며 삭제하므로 키 리스트를 따로 만든다.
            List<int> remove = null;

            foreach (var kv in tokenTable) {
                int id = kv.Key;

                // 캐시에 없고 dep<=0이면 제거 대상
                bool loaded = cache.TryGet(id, out var clip) && clip;
                if (loaded) continue;

                int dep = cache.TryGetDependency(id);
                if (dep > 0) continue;

                (remove ??= new List<int>()).Add(id);
            }

            if (remove == null || remove.Count == 0) return 0;

            foreach (var id in remove) tokenTable.Remove(id);

            return remove.Count;
        }
    }
}
#endif