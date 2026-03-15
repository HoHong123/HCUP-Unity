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

using System.Collections.Generic;
using HUtil.Collection;

namespace HUtil.Data.Cache {
    public class BaseDataCache<TKey, TData> : IDataCache<TKey, TData> where TData : class {
        #region Nested Class
#if UNITY_EDITOR && ODIN_INSPECTOR
        // 디버깅용 클래스
        public class Item {
#else
        protected class Item {
#endif
            public HashSet<object> Owners;
            public int Dependency;
            public TData Data;
        }
        #endregion

        #region Properties
        protected readonly Dictionary<TKey, Item> table = new();
#if UNITY_EDITOR && ODIN_INSPECTOR
        public IReadOnlyDictionary<TKey, Item> Preview => table;
#endif
        #endregion

        #region Public - Load
        /// <summary> 데이터 반환 / 의존성 체크 </summary>
        public bool TryLoad(TKey key, out TData data) {
            data = null;
            if (TryGet(key, out var item)) {
                table[key].Dependency++;
                return true;
            }
            return false;
        }
        #endregion

        #region Public - Get
        /// <summary> 데이터 반환 </summary>
        public virtual bool TryGet(TKey key, out TData data) {
            data = null;
            if (table.TryGetValue(key, out var item) && item.Data != null) {
                data = item.Data;
                return true;
            }
            return false;
        }
        #endregion

        #region Public - Save
        public bool Save(TKey key, TData data) {
            if (data == null) return false;

            if (table.TryGetValue(key, out var item) && item.Data != null) {
                item.Dependency++;
                return true;
            }

            table[key] = new Item {
                Owners = new(),
                Dependency = 1,
                Data = data
            };

            return true;
        }
        #endregion

        #region Public - Prune
        public void Prune() {
            table.RemoveIf(item => item.Dependency < 1);
        }
        #endregion

        #region Public - Clear
        public void Clear() {
            table.Clear();
        }
        #endregion

        #region Public - Remove
        public void ForceRemove(TKey key) {
            table.Remove(key);
        }

        public bool Release(TKey key) {
            if (!table.TryGetValue(key, out var item) || item.Data == null) return false;
            if (--item.Dependency > 0) return false;
            return table.Remove(key);
        }
        #endregion

#if UNITY_EDITOR
        #region Public - Debug
        public int TryGetDependency(TKey key) {
            return (table.TryGetValue(key, out var item) && item.Data == null) ? item.Dependency : 0;
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
 * TODO :: 추후 Dependency가 아닌 Set<object>을 통해 의존성을 체크하고
 * 1. Load시 소유자 리스트에 등록
 * 2. 소유자가 Load 호출시, 의존성 체크 후 의존성을 늘리지 않고 데이터 제공
 * 3. 소유자가 Get 호출시, 데이터 제공
 * 4. 소유자가 아닌 대상이 Get 호출시, 데이터 미제공
 * 이와 같은 흐름으로 제공될 예정.
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
 * ==============================================================
 */
#endif