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
                    _UpdateFirstEntryValue(key, value);
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
            // Dictionary 에만 존재하는 신규 키만 append (Public API 우회 경로의 safety net).
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
            base.Clear();

            if (entries != null && entries.Count > 0) {
                for (int k = 0; k < entries.Count; k++) {
                    Entry entry = entries[k];

                    if (ContainsKey(entry.Key)) {
#if UNITY_EDITOR
                        Debug.LogError(
                            $"[HDictionary] Duplicate key detected. Key='{entry.Key}' at index={k}. " +
                            $"Fix duplicate keys before entering play mode, building, or saving the scene.");
#endif
                        continue;
                    }

                    base.Add(entry.Key, entry.Value);
                }
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
            _RemoveFirstEntryByKey(key);
            return true;
        }

        public new bool Remove(TKey key, out TValue value) {
            if (!base.Remove(key, out value)) return false;
            _RemoveFirstEntryByKey(key);
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
        private void _UpdateFirstEntryValue(TKey key, TValue value) {
            Debug.Log($"[HDictionary] Updating value of existing key in entries. Key='{key}'.");
            IEqualityComparer<TKey> comparer = Comparer;
            for (int k = 0; k < entries.Count; k++) {
                if (!comparer.Equals(entries[k].Key, key)) continue;
                entries[k] = new Entry {
                    Key = key,
                    Value = value
                };
                return;
            }
        }

        private void _RemoveFirstEntryByKey(TKey key) {
            IEqualityComparer<TKey> comparer = Comparer;
            for (int k = 0; k < entries.Count; k++) {
                if (!comparer.Equals(entries[k].Key, key)) continue;
                entries.RemoveAt(k);
                return;
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
 *
 * =========================================================
 * 2026-04-26 (수정 3) :: 헤더 형틀 복원 + 헤더/Dev Log #if UNITY_EDITOR 가드 적용
 * =========================================================
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
 * =========================================================
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
 * =========================================================
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
 * =========================================================
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
