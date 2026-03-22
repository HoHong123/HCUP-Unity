#if UNITY_EDITOR
/* =========================================================
 * 런타임 데이터 캐시 저장소 베이스 클래스입니다.
 * Key → Data 구조로 캐시 데이터를 관리합니다.
 *
 * 주의사항 ::
 * 1. Dependency 값이 0이 되면 캐시가 제거됩니다.
 * 2. Save 호출 시 Dependency는 1로 시작합니다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;

namespace HUtil.Data.Cache {
    public class BaseDataCache<TKey, TData> : IDataCache<TKey, TData> where TData : class {
        #region Nested Class
#if UNITY_EDITOR && ODIN_INSPECTOR
        public class Item {
#else
        protected class Item {
#endif
            public HashSet<object> Owners;
            public int Dependency;
            public int AnonymousDependency;
            public TData Data;
        }
        #endregion

        #region Fields
        protected readonly Dictionary<TKey, Item> table = new();
        #endregion

        #region Events
        public event Action<TKey, TData> OnDataRemoved;
        #endregion

        #region Properties
#if UNITY_EDITOR && ODIN_INSPECTOR
        public IReadOnlyDictionary<TKey, Item> Preview => table;
#endif
        #endregion

        #region Public - Load
        /// <summary> 데이터 반환 / 익명 의존성 증가 </summary>
        public bool TryLoad(TKey key, out TData data) {
            data = null;
            if (!_TryGetItem(key, out var item, out data)) return false;

            item.AnonymousDependency++;
            item.Dependency++;
            return true;
        }

        /// <summary> 데이터 반환 / Owner 의존성 등록 </summary>
        public bool TryLoad(TKey key, object owner, out TData data) {
            data = null;
            if (owner == null) return TryLoad(key, out data);
            if (!_TryGetItem(key, out var item, out data)) return false;
            if (!item.Owners.Add(owner)) return true;

            item.Dependency++;
            return true;
        }
        #endregion

        #region Public - Get
        /// <summary> 데이터 반환 </summary>
        public virtual bool TryGet(TKey key, out TData data) {
            data = null;
            return _TryGetItem(key, out _, out data);
        }
        #endregion

        #region Public - Save
        public bool Save(TKey key, TData data) {
            if (data == null) return false;
            if (table.TryGetValue(key, out var item) && item.Data != null) {
                item.AnonymousDependency++;
                item.Dependency++;
                return true;
            }

            table[key] = new Item {
                Owners = new(),
                Dependency = 1,
                AnonymousDependency = 1,
                Data = data
            };

            return true;
        }

        public bool Save(TKey key, TData data, object owner) {
            if (owner == null) return Save(key, data);
            if (data == null) return false;

            if (table.TryGetValue(key, out var item) && item.Data != null) {
                if (!item.Owners.Add(owner)) return true;

                item.Dependency++;
                return true;
            }

            table[key] = new Item {
                Owners = new() { owner },
                Dependency = 1,
                AnonymousDependency = 0,
                Data = data
            };

            return true;
        }
        #endregion

        #region Public - Prune
        public void Prune() {
            List<TKey> removeKeys = null;

            foreach (var pair in table) {
                if (pair.Value.Dependency > 0 && pair.Value.Data != null) continue;
                (removeKeys ??= new()).Add(pair.Key);
            }

            if (removeKeys == null)
                return;
            foreach (var key in removeKeys)
                _RemoveItem(key);
        }
        #endregion

        #region Public - Clear
        public void Clear() {
            if (table.Count < 1) return;

            List<TKey> removeKeys = new(table.Keys);
            foreach (var key in removeKeys) _RemoveItem(key);
        }
        #endregion

        #region Public - Remove
        public void ForceRemove(TKey key) {
            _RemoveItem(key);
        }

        public bool Release(TKey key) {
            if (!table.TryGetValue(key, out var item) || item.Data == null) return false;
            if (item.AnonymousDependency < 1) return false;

            item.AnonymousDependency--;
            item.Dependency--;

            if (item.Dependency > 0) return false;
            return _RemoveItem(key);
        }

        public bool Release(TKey key, object owner) {
            if (owner == null) return Release(key);
            if (!table.TryGetValue(key, out var item) || item.Data == null) return false;
            if (!item.Owners.Remove(owner)) return false;

            item.Dependency--;
            if (item.Dependency > 0) return false;
            return _RemoveItem(key);
        }

        public int ReleaseOwner(object owner) {
            if (owner == null) return 0;

            List<TKey> removeKeys = null;
            int releasedCount = 0;

            foreach (var pair in table) {
                var item = pair.Value;
                if (item.Data == null) continue;
                if (!item.Owners.Remove(owner)) continue;

                item.Dependency--;
                releasedCount++;

                if (item.Dependency > 0) continue;
                (removeKeys ??= new()).Add(pair.Key);
            }

            if (removeKeys == null) return releasedCount;
            foreach (var key in removeKeys) {
                _RemoveItem(key);
            }

            return releasedCount;
        }


        private bool _RemoveItem(TKey key) {
            if (!table.TryGetValue(key, out var item)) return false;
            if (!table.Remove(key)) return false;

            _NotifyRemoved(key, item.Data);
            return true;
        }

        private void _NotifyRemoved(TKey key, TData data) {
            if (data == null) return;
            OnDataRemoved?.Invoke(key, data);
        }
        #endregion

        #region Private - Helper
        private bool _TryGetItem(TKey key, out Item item, out TData data) {
            data = null;
            item = null;
            if (!table.TryGetValue(key, out item) || item.Data == null) return false;

            data = item.Data;
            return true;
        }
        #endregion

#if UNITY_EDITOR
        #region Public - Debug
        public int TryGetDependency(TKey key) {
            return table.TryGetValue(key, out var item) && item.Data != null ? item.Dependency : 0;
        }

        public int TryGetOwnerCount(TKey key) {
            return table.TryGetValue(key, out var item) && item.Data != null ? item.Owners.Count : 0;
        }
        #endregion
#endif
    }
}

#if UNITY_EDITOR
/* Dev Log
 * ==============================================================
 * @Jason - PKH
 * Load와 Get의 차이점이 명확하게 보이지 않겠지만,
 * 
 * Load = 호출 시 호출한 오브젝트가 해당 데이터에 의존성을 가진다는 의미
 * Get = 호출 시 호출한 오브젝트가 해당 데이터에 의존성을 가지지 않는다는 의미
 * 
 * 즉슨, 책임있는 사용과 책임없는 사용의 차이다.
 *
 * 현재 구현은 익명 의존성과 Owner 기반 의존성을 함께 지원한다.
 * 1. Load시 Owner가 있으면 HashSet 기준으로 1회만 등록된다.
 * 2. 동일 Owner의 중복 Load는 Dependency를 중복 증가시키지 않는다.
 * 3. ReleaseOwner 호출 시 Owner가 점유한 모든 Key가 일괄 정산된다.
 * =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. TryLoad
 *    + 캐시 데이터 반환 + 의존성 증가
 * 2. TryGet
 *    + 캐시 데이터 반환 (의존성 증가 없음)
 * 3. Save
 *    + 캐시 데이터 저장
 * 4. Release
 *    + 의존성 감소
 * 5. Prune
 *    + Dependency 0 데이터 제거
 *
 * 사용법 ::
 * 1. Save로 데이터 캐시 등록
 * 2. TryLoad로 의존성 기반 사용
 *
 * 기타 ::
 * 1. Dependency 기반 캐시 관리 시스템입니다.
 * 2. 제거 이벤트를 통해 외부 자원 Release와 연동할 수 있습니다.
 * ==============================================================
 */
#endif
