#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Unity 기본 직렬화가 지원하지 않는 Dictionary를 Inspector에서 직렬화·편집할 수 있도록 만든 커스텀 래퍼 클래스입니다.
 *
 * 사용 예 ::
 * [SerializeField] HDictionary<string, int> stats = new();
 *
 * 특징 ::
 * Dictionary<TKey, TValue>를 상속하므로 런타임 조회는 O(1) 해시 경로를 그대로 유지합니다.
 * 데이터 모델은 "entries List가 영속 source of truth, Dictionary는 런타임 조회 뷰"입니다.
 *   OnAfterDeserialize : entries를 순회해 Dictionary를 재구축 (중복 키는 first-wins, entries는 불변)
 *   OnBeforeSerialize  : entries를 절대 wipe하지 않고, Dictionary에만 존재하는 신규 키를 append
 *   Add/Remove/TryAdd/indexer 오버라이드 : 런타임 변경을 즉시 entries에 반영하여
 *                                          HDictionary 참조 경로에서는 두 컬렉션이 항상 동기 상태를 유지
 * 이 구조는 Inspector에서 편집한 중복 엔트리(에러 상태)가 직렬화 라운드트립으로 인해
 * 소실되지 않도록 보장합니다. 중복은 상위 Validator가 workflow를 차단해 해소를 강제합니다.
 *
 * 동기화 경계 ::
 * HDictionary 참조로 호출하는 모든 변경 API(Add, TryAdd, Remove, TryAddOrReplace, this[key] = ...,
 * Clear)는 entries와 Dictionary를 함께 갱신합니다.
 * 단, 베이스 Dictionary<TKey, TValue>로 업캐스팅 후 호출하는 경우는 `new` 키워드 은닉 한계로
 * entries가 동기화되지 않습니다. 이 경로로 "새 키"가 추가된 경우 OnBeforeSerialize의 append 경로가
 * 수습하지만, 기존 키의 Value 변경·삭제는 직렬화 시 반영되지 않을 수 있습니다.
 *
 * 빌드 메모리 최적화 ::
 * 배포 빌드(!UNITY_EDITOR)에서는 OnAfterDeserialize 말미에 entries 프록시 List를
 * null로 대체해 백엔드 배열과 List 헤더를 모두 GC 회수 대상으로 만듭니다.
 * 즉 빌드 런타임에서 HDictionary의 실제 메모리는 Dictionary 본체만 남습니다.
 * JsonUtility.ToJson 같은 런타임 직렬화 요청이 들어오면 OnBeforeSerialize에서
 * entries를 lazy 재할당해 일시적으로만 복원합니다.
 * 에디터(UNITY_EDITOR)에서는 Inspector 표시·편집·반복 저장을 위해 entries를 유지합니다.
 *
 * 중복 키 정책 (하드 에러, first-wins) ::
 * 중복 Key는 오류입니다. Editor 측 HDictionaryValidator가 Play Mode 진입, Build,
 * Scene/Asset Save 경로를 차단하므로 정상 워크플로우에서는 중복이 있는 상태로
 * 런타임에 진입할 수 없습니다. 혹시 검증을 우회해 런타임에 도달하면 첫 번째 키의
 * 값을 보존(first-wins)하고 Debug.LogError를 남깁니다.
 *
 * 주의사항 ::
 * entries 필드는 빌드 런타임에 null일 수 있으므로 외부에서 직접 접근하지 말 것.
 * OnBeforeSerialize / OnAfterDeserialize 경로 외의 접근은 비보장입니다.
 * HasDuplicateKeys / DuplicateKeyCount는 entries가 있는 Editor 맥락에서만 유의미한
 * 결과를 반환합니다. 배포 빌드에서는 엔트리 해제 이후 false/0을 반환할 수 있습니다.
 * Add/Remove/indexer 오버라이드의 entries 선형 탐색 비용은 O(n)입니다.
 * Inspector 주도 편집 모델에서는 수용 가능하지만, 고빈도 런타임 변경에는 부적합할 수 있습니다.
 * OnAfterDeserialize 내부는 반드시 base.Clear / base.Add를 호출해야 합니다.
 * 오버라이드된 Clear/Add는 entries를 건드리므로 역직렬화 도중 데이터 소실 또는 무한 루프를 유발합니다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using System.Text;
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

        #region Public - Indexer
        // 기존 키의 값 변경 또는 신규 키 추가 모두 entries를 동기화한다.
        public new TValue this[TKey key] {
            get => base[key];
            set {
                bool existed = base.ContainsKey(key);
                base[key] = value;

                if (entries == null) return;

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

        #region Public - Serialization
        public void OnBeforeSerialize() {
            if (entries == null) entries = new List<Entry>(Count);

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
        }

        public void OnAfterDeserialize() {
            // 반드시 base.Clear()를 호출한다.
            // 오버라이드된 Clear()는 entries까지 비우므로 바로 다음 라인의 복원 루프가 읽어야 할 entries 데이터가 사라진다.
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
            // 배포 빌드 = 프록시 List를 완전히 놓아 GC 대상으로 전환.
            // 이후 JsonUtility.ToJson 등으로 직렬화 요청이 오면 OnBeforeSerialize에서 lazy 재할당으로 일시 복원.
            entries = null;
#endif
        }
        #endregion

        #region Public - Add
        public new void Add(TKey key, TValue value) {
            base.Add(key, value);
            entries?.Add(new Entry {
                Key = key,
                Value = value
            });
        }

        public new bool TryAdd(TKey key, TValue value) {
            if (!base.TryAdd(key, value)) return false;
            entries?.Add(new Entry {
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

        #region Public - Get
        public TValue GetValueOrDefault(TKey key, TValue defaultValue = default) {
            return TryGetValue(key, out TValue value) ? value : defaultValue;
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
            entries?.Clear();
        }
        #endregion

        #region IHDictionary - Validation
        public bool HasDuplicateKeys() {
            if (entries == null || entries.Count < 2) return false;

            HashSet<TKey> seen = new HashSet<TKey>(entries.Count, Comparer);
            for (int k = 0; k < entries.Count; k++) {
                if (!seen.Add(entries[k].Key)) return true;
            }

            return false;
        }

        public int DuplicateKeyCount() {
            if (entries == null || entries.Count < 2) return 0;

            HashSet<TKey> seen = new HashSet<TKey>(entries.Count, Comparer);
            int duplicates = 0;
            for (int k = 0; k < entries.Count; k++) {
                if (!seen.Add(entries[k].Key)) duplicates++;
            }

            return duplicates;
        }
        #endregion

        #region Private - Entry Sync
        private void _UpdateFirstEntryValue(TKey key, TValue value) {
            Debug.Log("[HDictionary] Updating value of existing key in entries. Key='" + key + "'.");
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
            if (entries == null) return;

            IEqualityComparer<TKey> comparer = Comparer;
            for (int k = 0; k < entries.Count; k++) {
                if (!comparer.Equals(entries[k].Key, key)) continue;
                entries.RemoveAt(k);
                return;
            }
        }
        #endregion

#if UNITY_EDITOR
        #region Editor - Sync Check
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

        #region Debug
        public IReadOnlyList<(TKey Key, TValue Value)> DebugSnapshot() {
            List<(TKey, TValue)> snapshot = new List<(TKey, TValue)>(Count);
            foreach (var kvp in this) snapshot.Add((kvp.Key, kvp.Value));
            return snapshot;
        }
        #endregion
#endif
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. OnBeforeSerialize
 *    + Dictionary에만 존재하는 신규 키를 entries에 append (append-only safety net)
 *    + entries가 null이면 즉석 재할당 (lazy 복원)
 *    + 기존 entries는 절대 wipe하지 않음 (중복 에러 상태 보존)
 *    + Dictionary.Comparer를 HashSet에도 전달하여 커스텀 비교자 일관성 확보
 * 2. OnAfterDeserialize
 *    + base.Clear로 Dictionary만 비움 (entries는 복원 소스이므로 건드리지 않음)
 *    + Entry List를 base.Add로 Dictionary에 복원 (O(n), 1회)
 *    + 중복 키는 first-wins, 초과분은 Debug.LogError
 *    + 배포 빌드에서는 복원 후 entries를 null 처리하여 메모리 해제
 * 3. Add / TryAdd
 *    + base API 호출 후 entries에 append
 *    + Add는 중복 시 ArgumentException 그대로 전파, entries는 미변경
 *    + TryAdd는 실패 시 no-op
 * 4. Remove(TKey) / Remove(TKey, out TValue)
 *    + base 제거 성공 시 entries에서 첫 번째 일치 항목 제거 (first-wins 일관성)
 * 5. this[TKey] setter
 *    + 기존 키면 entries 값 교체, 신규 키면 entries에 append
 * 6. TryAddOrReplace
 *    + 존재 시 값 교체(false 반환) / 없으면 신규 추가(true 반환)
 *    + 내부 indexer·Add 오버라이드를 경유하므로 entries가 함께 동기화됨
 * 7. Clear
 *    + Dictionary와 entries를 동시에 비움
 * 8. GetValueOrDefault
 *    + Key 미존재 시 기본값 반환 (읽기 전용)
 * 9. NeedsEntriesSync (Editor 전용)
 *    + Dictionary에 있지만 entries에 없는 키가 하나라도 있으면 true
 *    + Odin DictionaryDrawer 등 reflection 편집 경로가 base Dictionary만 수정해
 *      entries 동기화가 끊긴 상태를 감지
 *    + 컨테이너 Object가 OnValidate에서 이 값을 보고 EditorUtility.SetDirty(this)를
 *      호출하면 Ctrl+S 저장 파이프라인이 복구됨
 *    + append-only 정책 일관성을 위해 entries에만 있는 키(고아 엔트리)는 검사 제외
 * 10. ForceSyncEntriesFromDictionary (Editor 전용)
 *    + entries 전체를 Clear한 뒤 현재 Dictionary 내용으로 재구축 (O(n))
 *    + Odin DictionaryDrawer가 편집한 Dictionary 본체를 수동으로 entries에 플러시하는 용도
 *    + 중복 키·고아 엔트리 등 이전 오류 상태도 함께 리셋됨
 *    + 호출 후 컨테이너 Object에서 EditorUtility.SetDirty(this)를 호출해야 저장 경로로 반영
 * 11. DescribeEntriesSyncState (Editor 전용)
 *    + Dictionary vs entries의 불일치 현황을 리포트 문자열로 반환
 *    + only-in-dict / only-in-entries / value-mismatch / entries-duplicates 4개 축으로 분리
 *    + Odin Button에서 호출해 콘솔에 찍어보는 디버그 용도, 저장 실패·동기화 끊김 추적에 사용
 * 12. IsEntriesOutOfSync (Editor 전용)
 *    + entries가 Dictionary와 완전히 일치하는지 단일 boolean으로 반환 (O(n) 단일 순회)
 *    + 감지 대상 : Count 불일치 / entries 중복 / 고아 엔트리 / 공통 키의 Value 변경
 *    + NeedsEntriesSync는 append-only 관점의 부분 체크(신규 키만)인 반면
 *      IsEntriesOutOfSync는 Odin 편집의 추가/수정/삭제 3경로를 모두 커버
 *    + 컨테이너 Object의 Odin [OnInspectorGUI] 훅에서 매 Inspector repaint마다 호출하여
 *      자동 동기화 트리거 조건으로 사용
 *
 * 자동 동기화 전략 (Odin DictionaryDrawer 환경) ::
 * Odin DictionaryDrawer는 reflection으로 base Dictionary<K,V>를 직접 조작하여
 * HDictionary의 `new` shadowed Add/Remove/indexer 오버라이드를 우회한다. 결과적으로
 * Odin UI로 편집한 추가/수정/삭제가 [SerializeField] entries에 반영되지 않아 YAML
 * 저장 시점에 변경사항이 누락된다. (추가만 OnBeforeSerialize의 append-only safety net이
 * 우연히 커버하고, 수정/삭제는 완전히 누락됨)
 *
 * 이 문제는 HDictionary 내부에서 해결 불가 — 컨테이너 Object 참조가 부재하고
 * serialization 콜백 내 Unity API 호출은 비권장이기 때문. 따라서 컨테이너 Object
 * 레이어에서 Odin [OnInspectorGUI] 훅을 사용해 매 Inspector repaint마다
 * IsEntriesOutOfSync를 확인하고 불일치 시 ForceSyncEntriesFromDictionary +
 * EditorUtility.SetDirty 콤보로 복구하는 패턴이 권장된다. OnValidate / [OnValueChanged]
 * 등 Unity·Odin 이벤트 기반 훅은 Odin reflection 편집 경로를 완전히 커버하지 못해
 * 부분적 실패를 유발했으므로, 이벤트 중심이 아닌 "매 repaint 상태 검사" 방식을 택한다.
 *
 * 구현 예 (컨테이너 Object) ::
 *   [Sirenix.OdinInspector.OnInspectorGUI]
 *   private void _AutoSync() {
 *       bool changed = false;
 *       if (field != null && field.IsEntriesOutOfSync()) {
 *           field.ForceSyncEntriesFromDictionary();
 *           changed = true;
 *       }
 *       if (changed) UnityEditor.EditorUtility.SetDirty(this);
 *   }
 *
 * 성능 ::
 * IsEntriesOutOfSync는 O(n) 단일 순회이고 매 Inspector repaint마다 실행되지만
 * 안정 상태(동기화 완료)에서는 short-circuit return으로 비용이 미미하다. n이 수십~수백
 * 수준인 일반적 에셋 스케일에서는 무시 가능.
 *
 * 메모리 모델 ::
 * 에디터 - Dictionary<K,V> + List<Entry> (Inspector 표시용 유지)
 * 배포 빌드 - Dictionary<K,V>만 잔존 (entries는 초기화 후 GC 대상)
 *
 * 성능 요약 ::
 * 런타임 조회 - O(1) (Dictionary 상속)
 * Add / TryAdd - O(1) (단순 append)
 * Remove / indexer-set (기존 키) - O(n) (entries 선형 탐색)
 * 저장/로드 - O(n) 1회 변환
 *
 * 사용법 ::
 * 1. [SerializeField] HDictionary<K,V> field = new();
 * 2. 일반 Dictionary처럼 접근: field[key], field.Add(...), field.ContainsKey(...)
 * 3. HDictionary 참조로만 변경 API를 호출할 것.
 *    Dictionary<K,V>로 업캐스팅 후 호출하면 `new` 은닉 한계로 entries가 동기화되지 않는다.
 *
 * 기타 ::
 * 1. DebugSnapshot()은 에디터 전용 디버그 스냅샷 반환 (읽기 전용 투영)
 * 2. 중복 Key는 Inspector 편집 중에만 정상적으로 발생 가능하며 first-wins 정책 적용
 * 3. ContainsKey / TryGetValue / ContainsValue 등 읽기 전용 API는 오버라이드하지 않음
 *    (entries 동기화 필요가 없으므로 베이스 O(1) 경로를 그대로 노출)
 * =========================================================
 */
#endif
