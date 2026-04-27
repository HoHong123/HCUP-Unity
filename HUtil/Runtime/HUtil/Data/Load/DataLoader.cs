#if UNITY_EDITOR
/* =========================================================
 * DataLoadType에 따라 IDataLoad 구현체를 선택하는 Loader Resolver 클래스입니다.
 *
 * 구조 ::
 * DataLoadType → IDataLoad 매핑
 *
 * 주의사항 ::
 * Loader가 등록되지 않은 Type 요청 시 Assert가 발생합니다.
 * =========================================================
 */
#endif

using System.Collections.Generic;
using UnityEngine.Assertions;

namespace HUtil.Data.Load {
    public sealed class DataLoader<TKey, TData> {
        #region Fields
        readonly Dictionary<DataLoadType, IDataLoad<TKey, TData>> table = new();
        #endregion

        #region Public - Constructors
        public DataLoader(IDataLoad<TKey, TData> loader) {
            Assert.IsNotNull(loader);
            table[loader.Type] = loader;
        }
        public DataLoader(IEnumerable<IDataLoad<TKey, TData>> loaders) {
            Assert.IsNotNull(loaders);
            foreach (var loader in loaders) {
                Assert.IsNotNull(loader);
                table[loader.Type] = loader;
            }
        }
        #endregion

        #region Public - Resolve
        public IDataLoad<TKey, TData> Resolve(DataLoadType type) {
            if (table.TryGetValue(type, out var loader)) return loader;
            Assert.IsTrue(false, $"[DataLoader] Loader not registered. type={type}");
            return null;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. IDataLoad 등록
 * 2. DataLoadType 기반 Loader 조회
 *
 * 사용법 ::
 * 1. DataLoader 생성 시 Loader들을 등록합니다.
 * 2. Resolve(type)으로 Loader를 반환받습니다.
 *
 * 기타 ::
 * 1. 여러 Loader를 통합 관리하는 Resolver 역할을 합니다.
 * =========================================================
 */
#endif