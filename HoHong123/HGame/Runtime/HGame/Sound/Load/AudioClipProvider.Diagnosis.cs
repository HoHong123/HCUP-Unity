#if UNITY_EDITOR
using System.Collections.Generic;

namespace HGame.Sound.Load {
    public sealed partial class AudioClipProvider : IAudioClipDiagnostics {
        public AudioClipProviderSnapshot CreateSnapshot() {
            var snapshot = new AudioClipProviderSnapshot {
                ProviderName = GetType().Name,
                TokenCount = tokenTable.Count
            };

            snapshot.Catalogs.Capacity = catalogs.Count;
            Dictionary<int, List<string>> uidToCatalogs = new();

            foreach (var pair in catalogs) {
                var catalog = pair.Key;
                if (!catalog) continue;

                int refCount = pair.Value;
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

            foreach (var pair in tokenTable) {
                int id = pair.Key;
                uidToCatalogs.TryGetValue(id, out var catalogList);

                snapshot.Entries.Add(new AudioClipProviderSnapshot.Entry {
                    Id = id,
                    Token = pair.Value,
                    Dependency = cache.TryGetDependency(id),
                    OwnerCount = cache.TryGetOwnerCount(id),
                    IsLoaded = cache.TryGet(id, out var clip) && clip,
                    Clip = clip,
                    ClipName = clip ? clip.name : string.Empty,
                    ClipLength = clip ? clip.length : 0f,
                    CatalogNames = catalogList ?? new List<string>(0)
                });
            }

            return snapshot;
        }

        public int PruneUnusedTokens() {
            List<int> remove = null;

            foreach (var pair in tokenTable) {
                int id = pair.Key;
                bool loaded = cache.TryGet(id, out var clip) && clip;
                if (loaded) continue;

                int dependency = cache.TryGetDependency(id);
                if (dependency > 0) continue;

                (remove ??= new List<int>()).Add(id);
            }

            if (remove == null || remove.Count == 0) return 0;
            foreach (var id in remove)
                tokenTable.Remove(id);

            return remove.Count;
        }
    }
}
#endif
