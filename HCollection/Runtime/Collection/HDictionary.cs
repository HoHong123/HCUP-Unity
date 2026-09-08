#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Unity 기본 직렬화가 지원하지 않는 Dictionary 를 Inspector 에서 직렬화·편집할 수 있도록 만든 커스텀 래퍼.
 *
 * 사용 예 ::
 *   [SerializeField] HDictionary<string, int> stats = new();
 *
 * 특징 ::
 * Dictionary<TKey, TValue> 상속 + ISerializationCallbackReceiver. 런타임 조회 O(1) 보존.
 * entries List = 영속 source of truth (에디터), Dictionary = 런타임 조회 뷰.
 *
 * 동기화 경계 ::
 * 변경 API (Add / TryAdd / TryAddOrReplace / Remove x2 / Clear / indexer setter) 는
 * #if UNITY_EDITOR 로 통째 가드. 빌드에서는 `new` 키워드 hide 가 사라져 base 동명 API 가
 * 자동 노출 (사용자 코드 동작 동일). base 업캐스팅 후 호출은 entries 동기화 누락.
 * Odin reflection 같은 우회 경로는 IsEntriesOutOfSync + ForceSyncEntriesFromDictionary 로 보정.
 *
 * 빌드 메모리 최적화 ::
 * OnAfterDeserialize 말미에 entries = null. 그 시점부터 entries 책임 종료.
 * OnBeforeSerialize 본문도 #if UNITY_EDITOR 가드 (시그니처는 보존). 빌드 ToJson 결과는
 * { "entries": null } 로 의도된 빈 직렬화.
 *
 * 중복 키 정책 ::
 * 하드 에러 + first-wins. HDictionaryValidator 가 PlayMode / Build / Save 3 게이트를 차단.
 * 검증 우회 시 OnAfterDeserialize 가 첫 키 보존 + Debug.LogError.
 *
 * 주의사항 ::
 * - HDictionary 참조로만 변경 API 를 호출할 것. base 업캐스팅 후 호출은 entries 동기화 끊김.
 * - entries 필드는 빌드에서 null. 외부 직접 접근 금지.
 * - HasDuplicateKeys / DuplicateKeyCount 는 빌드에서 false / 0 을 반환.
 * - Add / Remove / indexer 의 entries 선형 탐색은 O(n). Inspector 편집 모델에서 수용 가능.
 * - OnAfterDeserialize 내부는 반드시 base.Clear / base.Add 를 호출. 오버라이드된 Clear / Add
 *   는 entries 를 건드리므로 역직렬화 도중 데이터 소실 또는 무한 루프를 유발한다.
 * =========================================================
 */
#endif

using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace HCollection {
    [Serializable]
    public class HDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver, IHDictionary {
        #region Nested Types
        [Serializable]
        private struct Entry {
            public TKey Key;
            public TValue Value;
        }
        #endregion

        #region Fields
        [SerializeField]
        List<Entry> entries = new();
        #endregion

#if UNITY_EDITOR
        #region Public - Indexer
        // 기존 키의 값 변경 또는 신규 키 추가 모두 entries 를 동기화한다.
        public new TValue this[TKey key] {
            get => base[key];
            set {
                bool existed = base.ContainsKey(key);
                base[key] = value;

                if (existed) {
                    _UpdateAllEntriesByKey(key, value);
                    return;
                }

                entries.Add(new Entry {
                    Key = key,
                    Value = value
                });
            }
        }
        #endregion
#endif

        #region Public - Serialization
        public void OnBeforeSerialize() {
#if UNITY_EDITOR
            // entries 는 에디터에서 항상 살아있으므로 lazy 재할당 불필요.
            // base 업캐스팅 경로로 삭제된 키는 Dictionary 에서만 사라지고 entries 에 고아로 남아,
            // 다음 역직렬화에서 그대로 부활했다. 고아 행을 정리한다.
            // 단, null 키 행은 "아직 키를 입력하지 않은 편집 중인 행" 이므로 지우지 않는다.
            for (int k = entries.Count - 1; k >= 0; k--) {
                TKey entryKey = entries[k].Key;
                if (entryKey is null) continue;
                if (ContainsKey(entryKey)) continue;
                Debug.LogWarning(
                    $"[HDictionary] Dropping orphan entry (key='{entryKey}') that no longer exists in the dictionary. "
                    + $"This happens when the dictionary is modified through a base Dictionary<K,V> reference.");
                entries.RemoveAt(k);
            }

            HashSet<TKey> existingKeys = new HashSet<TKey>(entries.Count, Comparer);
            for (int k = 0; k < entries.Count; k++) {
                existingKeys.Add(entries[k].Key);
            }

            foreach (var kv in this) {
                if (existingKeys.Contains(kv.Key)) continue;

                entries.Add(new Entry {
                    Key = kv.Key,
                    Value = kv.Value
                });

                existingKeys.Add(kv.Key);
            }
#endif
        }

        public void OnAfterDeserialize() {
            // 재-deserialize (빌드에서 Instantiate/프리팹 인스턴스화 시 재호출) - entries 가 이미
            // 해제된 상태라면 복원 소스가 없으므로 현재 Dictionary 상태를 그대로 유지한다.
            if (entries == null) return;

            // 로컬 재구축 후 성공 시에만 반영 - 콜백 도중 예외가 나가도 기존 데이터가 파괴되지 않는다.
            Dictionary<TKey, TValue> rebuilt = new Dictionary<TKey, TValue>(entries.Count, Comparer);
            for (int k = 0; k < entries.Count; k++) {
                Entry entry = entries[k];

                if (entry.Key is null) {
#if UNITY_EDITOR
                    Debug.LogError(
                        $"[HDictionary] Null key detected at index={k}. " +
                        $"Assign a key before entering play mode, building, or saving the scene.");
#endif
                    continue;
                }

#if UNITY_EDITOR
                // 값 타입 TKey 에서는 "비어 있음" 이 null 이 아니라 default 다 - `is null` 로는 잡히지 않아
                // 미배정 행 1개가 무경고로 정상 키가 됐다.
                if (typeof(TKey).IsValueType
                    && EqualityComparer<TKey>.Default.Equals(entry.Key, default)) {
                    Debug.LogWarning(
                        $"[HDictionary] Default-valued key at index={k}. " +
                        $"This is usually an unassigned inspector row.");
                }
#endif

                if (rebuilt.ContainsKey(entry.Key)) {
#if UNITY_EDITOR
                    Debug.LogError(
                        $"[HDictionary] Duplicate key detected. Key='{entry.Key}' at index={k}. " +
                        $"Fix duplicate keys before entering play mode, building, or saving the scene.");
#endif
                    continue;
                }

                rebuilt.Add(entry.Key, entry.Value);
            }

            base.Clear();
            foreach (var kv in rebuilt) {
                base.Add(kv.Key, kv.Value);
            }

#if !UNITY_EDITOR
            // 빌드 = 프록시 List 를 완전히 놓아 GC 대상으로 전환. 이후 entries 는 책임 종료.
            entries = null;
#endif
        }
        #endregion

        #region Public - IHDictionary Validation
        public bool HasDuplicateKeys() {
            if (entries == null || entries.Count < 2)
                return false;

            HashSet<TKey> seen = new HashSet<TKey>(entries.Count, Comparer);
            for (int k = 0; k < entries.Count; k++) {
                if (!seen.Add(entries[k].Key))
                    return true;
            }

            return false;
        }

        public int DuplicateKeyCount() {
            if (entries == null || entries.Count < 2)
                return 0;

            HashSet<TKey> seen = new HashSet<TKey>(entries.Count, Comparer);
            int duplicates = 0;
            for (int k = 0; k < entries.Count; k++) {
                if (!seen.Add(entries[k].Key))
                    duplicates++;
            }

            return duplicates;
        }
        #endregion

#if UNITY_EDITOR
        #region Public - Add
        public new void Add(TKey key, TValue value) {
            base.Add(key, value);
            entries.Add(new Entry {
                Key = key,
                Value = value
            });
        }

        public new bool TryAdd(TKey key, TValue value) {
            if (!base.TryAdd(key, value)) return false;
            entries.Add(new Entry {
                Key = key,
                Value = value
            });
            return true;
        }

        public bool TryAddOrReplace(TKey key, TValue value) {
            if (ContainsKey(key)) {
                this[key] = value;
                return false;
            }

            Add(key, value);
            return true;
        }
        #endregion

        #region Public - Remove
        public new bool Remove(TKey key) {
            if (!base.Remove(key)) return false;
            _RemoveAllEntriesByKey(key);
            return true;
        }

        public new bool Remove(TKey key, out TValue value) {
            if (!base.Remove(key, out value)) return false;
            _RemoveAllEntriesByKey(key);
            return true;
        }
        #endregion

        #region Public - Clear
        public new void Clear() {
            base.Clear();
            entries.Clear();
        }
        #endregion

        #region Public - Editor Sync Check
        public bool NeedsEntriesSync() {
            if (entries == null) return Count > 0;
            if (Count == 0) return false;

            HashSet<TKey> existingKeys = new HashSet<TKey>(entries.Count, Comparer);
            for (int k = 0; k < entries.Count; k++) {
                existingKeys.Add(entries[k].Key);
            }

            foreach (var kv in this) {
                if (!existingKeys.Contains(kv.Key)) return true;
            }

            return false;
        }

        public void ForceSyncEntriesFromDictionary() {
            // 중복 키 정책은 "하드 에러 + first-wins" 인데, 이 함수는 사용자 데이터를 조용히
            // 지워서 오류를 없앴다. 무엇이 사라지는지 반드시 알린다.
            if (entries != null && entries.Count > Count) {
                Debug.LogWarning(
                    $"[HDictionary] ForceSyncEntriesFromDictionary discards {entries.Count - Count} entry row(s) "
                    + $"(duplicates and/or orphans). Fix duplicate keys before syncing if they were intentional.");
            }

            if (entries == null) entries = new List<Entry>(Count);
            else entries.Clear();

            foreach (var kv in this) {
                entries.Add(new Entry {
                    Key = kv.Key,
                    Value = kv.Value
                });
            }
        }

        public string DescribeEntriesSyncState() {
            if (entries == null) {
                return $"entries=null (runtime mode assumed), dict.Count={Count}";
            }

            HashSet<TKey> dictKeys = new HashSet<TKey>(Keys, Comparer);
            HashSet<TKey> entriesKeys = new HashSet<TKey>(Comparer);
            int entriesDuplicateCount = 0;
            for (int k = 0; k < entries.Count; k++) {
                if (!entriesKeys.Add(entries[k].Key)) {
                    entriesDuplicateCount++;
                }
            }

            List<TKey> onlyInDict = new List<TKey>();
            List<TKey> onlyInEntries = new List<TKey>();
            foreach (TKey key in dictKeys) {
                if (!entriesKeys.Contains(key)) {
                    onlyInDict.Add(key);
                }
            }
            foreach (TKey key in entriesKeys) {
                if (!dictKeys.Contains(key)) {
                    onlyInEntries.Add(key);
                }
            }

            EqualityComparer<TValue> valueComparer = EqualityComparer<TValue>.Default;
            List<TKey> valueMismatch = new List<TKey>();
            HashSet<TKey> valueCheckedKeys = new HashSet<TKey>(Comparer);
            for (int k = 0; k < entries.Count; k++) {
                TKey key = entries[k].Key;
                if (!valueCheckedKeys.Add(key)) continue;
                if (!dictKeys.Contains(key)) continue;
                TValue dictValue = base[key];
                if (!valueComparer.Equals(dictValue, entries[k].Value)) {
                    valueMismatch.Add(key);
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"dict.Count={Count}, entries.Count={entries.Count}, entries.duplicates={entriesDuplicateCount}");
            sb.AppendLine($"only in dict ({onlyInDict.Count}): {string.Join(", ", onlyInDict)}");
            sb.AppendLine($"only in entries ({onlyInEntries.Count}) [orphan]: {string.Join(", ", onlyInEntries)}");
            sb.AppendLine($"value mismatch ({valueMismatch.Count}): {string.Join(", ", valueMismatch)}");
            return sb.ToString();
        }

        public bool IsEntriesOutOfSync() {
            if (entries == null) return Count > 0;
            if (entries.Count != Count) return true;

            HashSet<TKey> seen = new HashSet<TKey>(entries.Count, Comparer);
            EqualityComparer<TValue> valueComparer = EqualityComparer<TValue>.Default;

            for (int k = 0; k < entries.Count; k++) {
                TKey key = entries[k].Key;
                // Dictionary.TryGetValue 는 null 키에 ArgumentNullException 을 던진다 - 먼저 걸러낸다.
                if (key is null) return true;
                if (!seen.Add(key)) return true;
                if (!TryGetValue(key, out TValue dictValue)) return true;
                if (!valueComparer.Equals(dictValue, entries[k].Value)) return true;
            }

            return false;
        }
        #endregion

        #region Public - Debug
        public IReadOnlyList<(TKey Key, TValue Value)> DebugSnapshot() {
            List<(TKey, TValue)> snapshot = new List<(TKey, TValue)>(Count);
            foreach (var kvp in this) snapshot.Add((kvp.Key, kvp.Value));
            return snapshot;
        }
        #endregion
        
        #region Private - Entry Sync
        // 종전에는 첫 행만 갱신해, 중복 키 상태에서 TryAddOrReplace/indexer 로 값을 "정상적으로"
        // 고쳐도 stale 둘째 행이 entries 에 영구히 남아 재직렬화마다 dup-key 오류가 재현됐다
        // (케이스 리포트 09 COR-6). 값만 모든 매칭 행에 맞춰서는 "행 개수 중복" 자체가 해소되지
        // 않아 오류가 그대로 재현된다 - 매칭 행 중 하나만 남기고 나머지는 정리해야 한다.
        // 중복 키 정책(하드 에러 + first-wins)과 맞춰, 남기는 한 행에 새 값을 반영한다.
        private void _UpdateAllEntriesByKey(TKey key, TValue value) {
            IEqualityComparer<TKey> comparer = Comparer;
            bool valueApplied = false;
            int purged = 0;
            for (int k = entries.Count - 1; k >= 0; k--) {
                if (!comparer.Equals(entries[k].Key, key)) continue;

                if (valueApplied) {
                    entries.RemoveAt(k);
                    purged++;
                    continue;
                }

                entries[k] = new Entry {
                    Key = key,
                    Value = value
                };
                valueApplied = true;
            }

            if (purged > 0) {
                Debug.LogWarning(
                    $"[HDictionary] Update collapsed {purged} duplicate row(s) for key='{key}' into one while syncing the new value. "
                    + $"Fix duplicate keys in the inspector to avoid relying on this cleanup.");
            }
        }

        // 종전에는 첫 행만 제거해, 중복 키 상태에서 Remove 하면 둘째 행이 승격되어
        // "삭제했는데 값이 바뀐 채 살아있는" 결과가 나왔다. 키가 사라지면 그 키의 모든 행이 사라져야 한다.
        private void _RemoveAllEntriesByKey(TKey key) {
            IEqualityComparer<TKey> comparer = Comparer;
            for (int k = entries.Count - 1; k >= 0; k--) {
                if (comparer.Equals(entries[k].Key, key)) entries.RemoveAt(k);
            }
        }
        #endregion
#endif
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-08-08 (수정) :: _UpdateFirstEntryValue → _UpdateAllEntriesByKey (COR-6)
 *
 * 변경 ::
 * 1. `_UpdateFirstEntryValue(TKey, TValue)` 를 `_UpdateAllEntriesByKey(TKey, TValue)` 로
 *    개명. 매칭 행 중 하나(역방향 순회상 첫 발견)에 새 값을 반영하고, 나머지 매칭 행은
 *    `RemoveAt` 으로 정리 + 정리 건수가 있으면 `Debug.LogWarning`.
 * 2. 인덱서 setter 의 호출부(existed 분기)를 새 이름으로 갱신.
 *
 * 이유 ::
 * 케이스 리포트 09 COR-6. 중복 키 상태에서 공개 API(`TryAddOrReplace`/indexer)로 값을
 * 갱신해도 stale 둘째 행이 `entries` 에 영구히 남아, 재직렬화마다 `OnAfterDeserialize` 의
 * dup-key `LogError` 가 계속 재현됐다.
 *
 * **1차 시도(값만 모든 매칭 행에 동기화)는 부족했다** - Unity MCP `execute_code` 로 실측한
 * 결과, 두 행 모두 새 값으로는 갱신되지만 "행이 2개" 라는 사실 자체는 그대로라
 * `OnAfterDeserialize` 의 dup-key 오류가 여전히 재현됐다(리포트가 지적한 증상이 안 없어짐).
 * `_RemoveAllEntriesByKey` 와 진짜 대칭이 되려면 "값 동기화" 가 아니라 "중복 행 자체의 정리"
 * 가 필요해, 매칭 행을 하나로 수렴시키는 형태로 재설계했다. 클래스 헤더의 기존 중복 키
 * 정책("하드 에러 + first-wins")과 `ForceSyncEntriesFromDictionary` 의 파기 경고 관행을
 * 그대로 따른다.
 *
 * 결과 ::
 * Unity MCP `execute_code` 로 리플렉션 기반 재현(`entries=[(K,10),(K,99)]` 조성 후
 * `TryAddOrReplace(K,777)` → 재직렬화 왕복) - 수정 전: stale 둘째 행 잔존 + dup-key 오류
 * 재발 확인. 1차 시도(값만 동기화): 두 행 다 777 이지만 여전히 2행 → dup-key 오류 재발
 * 확인. 최종본: 재직렬화 후 `entries.Count == 1`, dup-key 오류 미재현, `dict[K] == 777`
 * 유지 확인.
 *
 * 주의 ::
 * O(n) 선형 탐색은 동일. 정리된 행이 있으면 경고를 남기므로, 편집자는 Inspector 에서
 * 근본 원인(중복 키 입력)을 고치라는 신호를 계속 받는다 - 이 정리는 증상 완화이지
 * 중복 키 입력 자체를 막지는 않는다(그건 `HDictionaryValidator` 3게이트의 역할).
 *
 * =========================================================
 * 2026-04-26 (수정 3) :: 헤더 형틀 복원 + 헤더/Dev Log #if UNITY_EDITOR 가드 적용
 *
 * 변경 ::
 * 1. 헤더 주석을 "도입 + 사용 예 / 특징 / 동기화 경계 / 빌드 메모리 최적화 / 중복 키 정책 /
 *    주의사항" 7 섹션 형틀로 복원. 각 섹션 내용은 1~3 줄로 압축.
 * 2. 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드로 감쌈 (이전 "수정 1" 에서 제거했던 가드 복원).
 *
 * 이유 ::
 * 직전 "수정 1" 이 헤더를 1~3 줄로 통째 압축해 형틀 (섹션 라벨) 자체를 손상시켰다.
 * reader 가 "이 클래스가 어떤 축으로 설명되는가" 를 섹션 라벨만으로 한눈에 파악할 수
 * 있도록 형틀을 보존하면서 각 섹션 내용만 압축하는 방향이 맞다. #if UNITY_EDITOR 가드는
 * IL 영향은 없지만 IDE (VS / Rider / VS Code C# 확장) 가 회색조로 표시해 "이 영역은 빌드에
 * 안 들어간다" 를 reader 의 시야에 미리 인식시킨다. 글로벌 CLAUDE.md §11 의 헤더 컨벤션으로
 * 모든 미래 시스템에 동일 적용.
 *
 * =========================================================
 * 2026-04-26 (수정 2) :: GetValueOrDefault 제거
 *
 * 변경 ::
 * 1. Public - Get region 전체 제거 (GetValueOrDefault 단일 메서드).
 * 2. 인접한 #endif + #if UNITY_EDITOR 페어를 통합하여 Add/Remove/Clear region 을
 *    한 #if UNITY_EDITOR 블록 안으로 정리.
 * 3. 본 Dev Log 의 "2026-04-25 (최초 설계) > Public API 목록" 에서
 *    GetValueOrDefault 줄 제거.
 *
 * 이유 ::
 * GetValueOrDefault 는 entries 와 무관한 읽기 API 였고 본문이 단순히 TryGetValue 호출 후
 * ternary 반환만 했다. .NET Standard 2.1 의 System.Collections.Generic.CollectionExtensions
 * 가 동일 시그니처 (1-arg / 2-arg, defaultValue 인자명까지 일치) 의 extension method 를
 * 이미 제공하므로 HDictionary 에서 별도 정의할 이유가 없었다.
 *
 * "entries 와의 관계가 없으면 HDictionary 가 정의하지 않는다" 라는 단일 결정 기준을 끝까지
 * 적용한 결과 - HDictionary 가 책임질 이유가 없는 메서드를 책임 범위에서 제거.
 *
 * 결과 ::
 * 1. 사용자 코드 변화 0 - `dict.GetValueOrDefault(key, fallback)` 호출이 .NET Standard 2.1
 *    extension method 로 자동 바인딩 (Dictionary<K,V> 가 IReadOnlyDictionary<K,V> 구현).
 *    `using System.Collections.Generic;` 만 있으면 자동 노출.
 * 2. HDictionary 의 책임 경계 명료화 - entries 동기화 + 직렬화 콜백 + IHDictionary 구현 +
 *    Editor 진단 도구만 유지.
 * 3. LOC -7 (region 헤더 + 본문 + 인접 #endif + #if UNITY_EDITOR + 빈 줄 정리).
 *
 * 호출처 검증 (2026-04-26 시점 grep) ::
 * - VFoldersLibs / UniTask Sum.cs 의 Nullable<T>.GetValueOrDefault() 호출만 존재.
 * - HDictionary 인스턴스의 명시 호출 0건.
 * - portfolio bundle sample (HDictionaryUsageSample.cs) 의 4 곳은 extension method 로 자동
 *   바인딩되어 동작 변화 없음.
 *
 * =========================================================
 * 2026-04-26 (수정) :: 변경 API + OnBeforeSerialize 본문 #if UNITY_EDITOR 가드 적용
 *
 * 변경 ::
 * 1. OnBeforeSerialize 본문 전체를 #if UNITY_EDITOR 로 감쌌다 (시그니처는 보존).
 *    - lazy 재할당 (`if (entries == null) entries = new List<Entry>(Count);`) 제거.
 *    - 빌드에서는 본문이 빈 메서드로 통과한다.
 * 2. 모든 변경 API (Add / TryAdd / TryAddOrReplace / Remove x2 / Clear / indexer setter)
 *    + private helper (_UpdateFirstEntryValue, _RemoveFirstEntryByKey) 를
 *    통째로 #if UNITY_EDITOR 로 감쌌다.
 *    - 빌드에서는 `new` 키워드 hide 가 사라져 base Dictionary<K,V> 의 동명 API 가
 *      자동 노출된다 (사용자 코드 동작 동일).
 *    - entries 동기화 분기와 Entry struct 생성 IL 이 빌드 바이너리에서 제거된다.
 * 3. 변경 API 본문에서 `entries?.Add(...)` / `entries?.Clear()` / null 가드를 제거하고
 *    `entries.Add(...)` / `entries.Clear()` 직접 호출로 단순화.
 *    - #if UNITY_EDITOR 가드 안에서는 entries 가 항상 살아있음이 보장되므로
 *      null check 자체가 dead code.
 * 4. 헤더 주석을 1~3줄로 간략화하고 기존 긴 자료를 본 Dev Log 의 "2026-04-25 (최초 설계)"
 *    엔트리로 이관.
 *
 * 이유 ::
 * 기존 설계는 변경 API 와 OnBeforeSerialize 의 entries 동기화 분기를 빌드에 그대로
 * 두고 null-safe (?. 연산자) 로 무력화했다. 이 방식은 다음 두 가지 over-engineering 을 동반.
 *   (a) 빌드 바이너리에 dead code 가 잔존 (변경 API 본문 + Entry struct 생성 + null check IL).
 *   (b) OnBeforeSerialize 의 lazy 재할당이 "혹시 빌드에서 ToJson 호출되면" 시나리오만을
 *       위해 깔린 안전망인데, 이 시나리오는 HDictionary 의 1차 의도 ("Inspector 에서
 *       직렬화·편집할 수 있는 Dictionary") 에 포함되지 않는다.
 *
 * 사용자 정신 모델 정의:
 *   - 인스펙터: entries 직렬 관련 기능 모두 필요.
 *   - 에디터 PlayMode: entries 살아있어야 인스펙터에서 데이터 확인 가능. Public API 가
 *     entries 동기화하므로 OnBeforeSerialize 의 append-only safety net 은 불필요.
 *   - 빌드: OnAfterDeserialize 가 entries → Dictionary 복원 후 entries = null,
 *     그 시점부터 entries 책임 종료. 변경 API 의 entries 동기화는 dead code.
 * 위 정의를 받아들이면 본 리팩토링이 1차 의도와 정합하면서 빌드 바이너리도 슬림화한다.
 *
 * 결과 ::
 * 1. 빌드 바이너리에서 entries 동기화 관련 IL 제거 (Entry struct 생성, null check, 분기 모두).
 * 2. lazy 재할당 제거로 "빌드에서 ToJson 한 번 호출 시 entries 가 영구 살아남던" 미묘한
 *    메모리 누수 가능성도 제거.
 * 3. 빌드에서 ToJson 호출 시 entries 는 null 인 채로 직렬화된다 ({"entries": null}).
 *    이는 의도된 동작 - 빌드 환경에서 HDictionary 를 ToJson 해야 한다면 도메인 코드가
 *    별도 직렬화 도구로 Dictionary 자체를 처리한다.
 * 4. ISerializationCallbackReceiver 인터페이스 일관성은 시그니처 보존으로 유지
 *    (OnBeforeSerialize 시그니처는 빌드에 노출, 본문만 #if 가드).
 * 5. 변경 API 통째 가드 후에도 사용자 코드는 동작 변화 없음 - `dict.Add(...)` 가
 *    빌드에서 base.Dictionary.Add 로 자동 결정됨 (`new` 키워드 hide 의 자연스런 부작용).
 *
 * 주의 ::
 * 1. 빌드에서 `dict.TryAddOrReplace(...)` 를 호출하면 컴파일 에러 (base 에 없는 신규 API).
 *    리팩토링 시점 기준 코드베이스 호출처 0건 (헤더 주석 외). 향후 빌드 호출 추가 시 본
 *    메서드만 본문 가드 형태로 별도 보존할 것.
 * 2. PlayMode 도중 인스펙터 데이터 동기화는 Public API 의 entries 동기화에 100% 의존.
 *    Public API 를 우회하는 경로 (예: Odin reflection 직접 편집) 는 IsEntriesOutOfSync
 *    + ForceSyncEntriesFromDictionary 콤보로 별도 처리 (Editor - Sync Check 영역).
 *
 * =========================================================
 * 2026-04-25 (최초 설계) :: HDictionary 초기 구현
 *
 * 설계 모델 ::
 * 1. entries List 가 영속 source of truth, Dictionary 는 런타임 조회 뷰.
 *    - OnAfterDeserialize: entries -> Dictionary 재구축 (중복 키 first-wins, entries 불변)
 *    - OnBeforeSerialize: entries 는 절대 wipe 하지 않고 Dictionary 에만 존재하는 신규 키 append
 *    - 변경 API 오버라이드: 두 컬렉션 동기 갱신
 * 2. 중복 키 정책 = 하드 에러 + first-wins.
 *    - HDictionaryValidator 가 PlayMode/Build/Save 3 게이트로 차단.
 *    - 검증 우회 시 OnAfterDeserialize 가 첫 키 보존 + Debug.LogError.
 * 3. 빌드 메모리 최적화:
 *    - OnAfterDeserialize 말미에서 entries = null (빌드 한정).
 *    - Dictionary 본체만 잔존.
 * 4. base 업캐스팅 후 호출은 `new` 키워드 hide 한계로 entries 동기화 누락.
 *    - OnBeforeSerialize append-only safety net 이 신규 키만 수습.
 *    - 기존 키의 Value 변경/삭제는 직렬화 시 누락 가능 (Odin DictionaryDrawer 한정 함정).
 *
 * 메모리 모델 ::
 * - 에디터: Dictionary<K,V> + List<Entry> (Inspector 표시용 유지)
 * - 빌드:   Dictionary<K,V> 만 잔존 (entries 는 OnAfterDeserialize 직후 GC 대상)
 *
 * 성능 요약 ::
 * - 런타임 조회: O(1) (Dictionary 상속)
 * - Add / TryAdd: O(1) (단순 append)
 * - Remove / indexer-set (기존 키): O(n) (entries 선형 탐색)
 * - 저장/로드: O(n) 1회 변환
 *
 * 사용법 ::
 * 1. [SerializeField] HDictionary<K,V> field = new();
 * 2. 일반 Dictionary 처럼 접근: field[key], field.Add(...), field.ContainsKey(...)
 * 3. HDictionary 참조로만 변경 API 를 호출할 것.
 *    Dictionary<K,V> 로 업캐스팅 후 호출하면 `new` 은닉 한계로 entries 가 동기화되지 않는다.
 *
 * Public API 목록 ::
 * - 변경 API (entries 동기화 동반): Add / TryAdd / TryAddOrReplace / Remove x2 / Clear / indexer setter
 * - IHDictionary 구현: HasDuplicateKeys / DuplicateKeyCount (빌드 노출, entries == null 시 false/0)
 * - Editor 진단: NeedsEntriesSync / ForceSyncEntriesFromDictionary / DescribeEntriesSyncState /
 *               IsEntriesOutOfSync / DebugSnapshot (#if UNITY_EDITOR 가드)
 *
 * Odin DictionaryDrawer 자동 동기화 전략 ::
 * Odin DictionaryDrawer 는 reflection 으로 base Dictionary<K,V> 를 직접 조작하여 HDictionary 의
 * `new` shadowed Add/Remove/indexer 오버라이드를 우회한다. 결과적으로 Odin UI 로 편집한
 * 추가/수정/삭제가 [SerializeField] entries 에 반영되지 않아 YAML 저장 시점에 변경사항이 누락된다.
 * (추가만 OnBeforeSerialize 의 append-only safety net 이 우연히 커버, 수정/삭제는 완전히 누락)
 *
 * 이 문제는 HDictionary 내부에서 해결 불가 - 컨테이너 Object 참조가 부재하고 serialization
 * 콜백 내 Unity API 호출은 비권장이기 때문. 컨테이너 Object 레이어에서 Odin [OnInspectorGUI]
 * 훅으로 IsEntriesOutOfSync + ForceSyncEntriesFromDictionary + EditorUtility.SetDirty 콤보를 적용.
 *
 * 구현 예 (컨테이너 Object) ::
 *   [Sirenix.OdinInspector.OnInspectorGUI]
 *   private void _AutoSync() {
 *       if (field != null && field.IsEntriesOutOfSync()) {
 *           field.ForceSyncEntriesFromDictionary();
 *           UnityEditor.EditorUtility.SetDirty(this);
 *       }
 *   }
 *
 * 안정 상태에서는 IsEntriesOutOfSync 가 short-circuit return 으로 비용 미미.
 * =========================================================
 */
#endif
