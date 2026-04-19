#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Unity 기본 직렬화가 지원하지 않는 Dictionary를 Inspector에서 직렬화·편집할 수 있도록 만든 prefix 래퍼 클래스입니다.
 *
 * 사용 예 ::
 * [SerializeField] HDictionary<string, int> stats = new();
 *
 * 특징 ::
 * Dictionary<TKey, TValue>를 상속하므로 런타임 조회는 O(1) 해시 경로를 그대로 유지합니다.
 * 데이터 모델은 "entries List가 source of truth, Dictionary는 runtime 조회 뷰"입니다.
 *   OnAfterDeserialize : entries를 순회해 Dictionary를 재구축 (중복 키는 first-wins, entries는 불변)
 *   OnBeforeSerialize  : entries를 절대 wipe하지 않고, Dictionary에만 존재하는 신규 키를 append
 * 이 구조는 Inspector에서 편집한 중복 엔트리(에러 상태)가 직렬화 라운드트립으로 인해
 * 소실되지 않도록 보장합니다. 중복은 상위 Validator가 workflow를 차단해 해소를 강제합니다.
 *
 * 빌드 메모리 최적화 ::
 * 배포 빌드(!UNITY_EDITOR)에서는 OnAfterDeserialize 말미에 entries 프록시 List를
 * null로 대체해 백엔드 배열과 List 헤더를 모두 GC 회수 대상으로 만듭니다.
 * 즉 빌드 런타임에서 HDictionary의 실제 메모리는 Dictionary 본체만 남습니다.
 * JsonUtility.ToJson 같은 런타임 직렬화 요청이 들어오면 OnBeforeSerialize에서
 * entries를 lazy 재할당해 일시적으로만 복원합니다.
 * 에디터(UNITY_EDITOR)에서는 Inspector 표시·편집·반복 저장을 위해 entries를 유지합니다.
 *
 * 중복 키 정책 (하드 에러) ::
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
 * =========================================================
 */
#endif

using System;
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

        #region Public - Serialization
        public void OnBeforeSerialize() {
            // 하드 에러 정책: entries 자체가 source of truth이다.
            // 중복 키가 포함된 entries는 사용자가 수정해야 할 오류 상태이지만
            // 그 상태를 직렬화 주기가 파괴해서는 안 된다.
            //
            // 따라서 이 메서드는 기존 entries를 절대 덮어쓰거나 Clear하지 않는다.
            // 대신 코드에서 Dictionary.Add 경유로 추가된 "entries에 아직 없는 키"만
            // entries 말미에 append하여 직렬화 대상에 포함시킨다.
            //
            // 트레이드오프: Dictionary API로 기존 키의 Value를 변경한 뒤 저장해도
            // entries의 Value는 동기화되지 않는다. Inspector가 주 편집 경로인 설계
            // 전제에서 이는 수용 가능한 한계이다.
            if (entries == null) entries = new List<Entry>(Count);

            HashSet<TKey> existingKeys = new HashSet<TKey>(entries.Count);
            for (int i = 0; i < entries.Count; i++) {
                existingKeys.Add(entries[i].Key);
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
            Clear();

            if (entries != null && entries.Count > 0) {
                for (int k = 0; k < entries.Count; k++) {
                    Entry entry = entries[k];

                    if (ContainsKey(entry.Key)) {
                        // 중복 키는 하드 에러. first-wins 정책 — 먼저 등록된 값을 보존하고
                        // 이후의 중복 엔트리는 무시한다. 상위 검증 레이어(HDictionaryValidator)가
                        // Play/Build/Save를 차단하므로 정상 워크플로우에서는 이 경로가 실행되지
                        // 않으며, 만약 실행된다면 검증 우회 상태이므로 에러 로그를 남긴다.
#if UNITY_EDITOR
                        Debug.LogError(
                            $"[HDictionary] Duplicate key detected. Key='{entry.Key}' at index={k}. " +
                            $"Fix duplicate keys before entering play mode, building, or saving the scene.");
#endif
                        continue;
                    }

                    Add(entry.Key, entry.Value);
                }
            }

#if !UNITY_EDITOR
            // 배포 빌드: 프록시 List를 완전히 놓아 GC 대상으로 전환.
            // 이후 JsonUtility.ToJson 등으로 직렬화 요청이 오면
            // OnBeforeSerialize에서 lazy 재할당으로 일시 복원.
            entries = null;
#endif
        }
        #endregion

        #region Public - Add
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

        #region IHDictionary - Validation
        public bool HasDuplicateKeys() {
            if (entries == null || entries.Count < 2) return false;

            HashSet<TKey> seen = new HashSet<TKey>(entries.Count);
            for (int i = 0; i < entries.Count; i++) {
                if (!seen.Add(entries[i].Key)) return true;
            }
            return false;
        }

        public int DuplicateKeyCount() {
            if (entries == null || entries.Count < 2) return 0;

            HashSet<TKey> seen = new HashSet<TKey>(entries.Count);
            int duplicates = 0;
            for (int i = 0; i < entries.Count; i++) {
                if (!seen.Add(entries[i].Key)) duplicates++;
            }
            return duplicates;
        }
        #endregion

#if UNITY_EDITOR
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
 *    + Dictionary 데이터를 Entry List로 변환 (저장 직전 1회, O(n))
 *    + entries가 null이면 즉석 재할당 (lazy 복원)
 * 2. OnAfterDeserialize
 *    + Entry List 데이터를 Dictionary로 복원 (로드 직후 1회, O(n))
 *    + 배포 빌드에서는 복원 후 entries를 null 처리하여 메모리 해제
 * 3. TryAddOrReplace
 *    + Key 존재 시 값 교체 / 없으면 신규 추가
 * 4. GetValueOrDefault
 *    + Key 미존재 시 기본값 반환
 *
 * 메모리 모델 ::
 * 에디터 - Dictionary<K,V> + List<Entry> (Inspector 표시용 유지)
 * 배포 빌드 - Dictionary<K,V>만 잔존 (entries는 초기화 후 GC 대상)
 *
 * 성능 요약 ::
 * 런타임 조회 - O(1) (Dictionary 상속)
 * 저장/로드 - O(n) 1회 변환
 *
 * 사용법 ::
 * 1. [SerializeField] HDictionary<K,V> field = new();
 * 2. 일반 Dictionary처럼 접근: field[key], field.Add(...), field.ContainsKey(...)
 *
 * 기타 ::
 * 1. DebugSnapshot()은 에디터 전용 디버그 스냅샷 반환 (읽기 전용 투영)
 * 2. 중복 Key는 Inspector 편집 중에만 발생 가능하며 last-wins 정책 적용
 * =========================================================
 */
#endif
