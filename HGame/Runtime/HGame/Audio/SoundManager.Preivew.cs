#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using HGame.Sound.Core;

/* =========================================================
 * @Jason - PKH
 * Audio.SoundManager의 에디터 preview partial 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 에디터에서만 사용되는 디버그 보조 코드입니다.
 * 2. 실제 로딩 결과를 가공해 보여주지만 로딩 정책 자체를 바꾸지는 않습니다.
 * =========================================================
 */

namespace HGame.Audio {
    public sealed partial class SoundManager {
        readonly Dictionary<SoundCatalogSO, int> previewCatalogRefs = new Dictionary<SoundCatalogSO, int>();
        readonly HashSet<string> previewTokens = new HashSet<string>(StringComparer.Ordinal);

        #region Public - Preview
        public AudioClipManagerSnapshot CreateSnapshot() {
            var snapshot = new AudioClipManagerSnapshot {
                ManagerName = GetType().Name,
                TokenCount = previewTokens.Count,
                EntryCount = previewTokens.Count,
            };

            Dictionary<string, List<string>> tokenToCatalogs = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var pair in previewCatalogRefs) {
                SoundCatalogSO catalog = pair.Key;
                if (!catalog) continue;

                int loadedCount = 0;
                int entryCount = catalog.Entries?.Count ?? 0;
                if (catalog.Entries != null) {
                    foreach (var entry in catalog.Entries) {
                        if (entry == null) continue;

                        string token = _NormalizeToken(entry.Token);
                        if (string.IsNullOrWhiteSpace(token)) continue;

                        _TrackCatalogName(tokenToCatalogs, token, catalog.name);

                        AudioClip loadedClip = null;
                        if (clipRepository != null && clipRepository.TryGet(token, out loadedClip) && loadedClip) {
                            loadedCount++;
                        }
                    }
                }

                snapshot.Catalogs.Add(new AudioClipManagerSnapshot.CatalogGroup {
                    Name = catalog.name,
                    RefCount = pair.Value,
                    EntryCount = entryCount,
                    LoadedCount = loadedCount,
                });
            }

            List<string> orderedTokens = new List<string>(previewTokens);
            orderedTokens.Sort(StringComparer.Ordinal);

            foreach (var token in orderedTokens) {
                AudioClip clip = null;
                bool isLoaded = clipRepository != null && clipRepository.TryGet(token, out clip) && clip;
                if (isLoaded) snapshot.LoadedCount++;

                tokenToCatalogs.TryGetValue(token, out var catalogNames);
                snapshot.Entries.Add(new AudioClipManagerSnapshot.Entry {
                    Token = token,
                    IsLoaded = isLoaded,
                    Clip = clip,
                    ClipName = clip ? clip.name : string.Empty,
                    ClipLength = clip ? clip.length : 0f,
                    CatalogNames = catalogNames ?? new List<string>(),
                });
            }

            return snapshot;
        }
        #endregion

        #region Private - Preview
        private void _TrackPreviewToken(string token) {
            if (string.IsNullOrWhiteSpace(token)) return;
            previewTokens.Add(token);
        }

        private void _TrackPreviewCatalog(SoundCatalogSO catalog) {
            if (!catalog) return;

            if (previewCatalogRefs.TryGetValue(catalog, out int refCount)) {
                previewCatalogRefs[catalog] = refCount + 1;
            }
            else {
                previewCatalogRefs.Add(catalog, 1);
            }

            if (catalog.Entries == null) return;
            foreach (var entry in catalog.Entries) {
                if (entry == null) continue;
                _TrackPreviewToken(_NormalizeToken(entry.Token));
            }
        }

        private void _ReleasePreviewCatalog(SoundCatalogSO catalog) {
            if (!catalog) return;
            if (!previewCatalogRefs.TryGetValue(catalog, out int refCount)) return;

            if (refCount > 1) {
                previewCatalogRefs[catalog] = refCount - 1;
                return;
            }

            previewCatalogRefs.Remove(catalog);
        }

        private void _TrackCatalogName(
            Dictionary<string, List<string>> tokenToCatalogs,
            string token,
            string catalogName) {

            if (!tokenToCatalogs.TryGetValue(token, out var catalogNames)) {
                catalogNames = new List<string>();
                tokenToCatalogs.Add(token, catalogNames);
            }

            if (!catalogNames.Contains(catalogName)) {
                catalogNames.Add(catalogName);
            }
        }
        #endregion
    }
}

/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. preview 대상 catalog와 token을 추적합니다.
 * 2. 현재 상태를 AudioClipManagerSnapshot으로 변환합니다.
 *
 * 사용법 ::
 * 1. SoundManager 인스펙터 preview 확인 용도로 사용합니다.
 * 2. CreateSnapshot 호출 시 현재 추적 상태를 정리합니다.
 *
 * 이벤트 ::
 * 1. preview 추적 목록이 바뀌면 다음 스냅샷 내용이 갱신됩니다.
 * 2. 별도의 런타임 이벤트는 발생시키지 않습니다.
 *
 * 기타 ::
 * 1. 디버그용 HashSet과 Dictionary를 내부에서 유지합니다.
 * 2. 재생 로직과 해제 로직은 본체 SoundManager에 있습니다.
 * =========================================================
 */
#endif
