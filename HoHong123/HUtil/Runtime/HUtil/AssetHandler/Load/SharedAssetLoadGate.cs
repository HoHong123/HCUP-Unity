using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 동일 key 동시 로드를 공유하는 기본 게이트 구현 스크립트입니다.
 *
 * 주의사항 ::
 * 1. factory는 예외 발생 시에도 정리 흐름을 고려해야 합니다.
 * 2. 게이트는 결과 캐시가 아니라 진행 중 작업 공유만 담당합니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Load {
    public sealed class SharedAssetLoadGate<TKey, TAsset> : IAssetLoadGate<TKey, TAsset> {
        #region Private - Fields
        readonly Dictionary<TKey, UniTask<TAsset>> loadingTable = new();
        #endregion

        #region Public - Run
        public async UniTask<TAsset> RunAsync(TKey key, Func<UniTask<TAsset>> factory) {
            Assert.IsNotNull(factory, "[SharedAssetLoadGate] factory is null.");

            if (loadingTable.TryGetValue(key, out var runningTask)) {
                return await runningTask;
            }

            var newTask = factory.Invoke();
            loadingTable[key] = newTask;

            try {
                return await newTask;
            }
            finally {
                loadingTable.Remove(key);
            }
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. loadingTable로 실행 중 작업을 추적합니다.
 * 2. 같은 key 요청을 하나의 UniTask로 공유합니다.
 *
 * 사용법 ::
 * 1. provider 기본 조합에서 중복 source 호출을 줄이기 위해 사용합니다.
 * 2. 동일 key 연속 요청이 발생하는 환경에 적합합니다.
 *
 * 이벤트 ::
 * 1. 직접 이벤트는 없습니다.
 * 2. 실행 중 task가 끝나면 게이트 테이블에서 제거됩니다.
 *
 * 기타 ::
 * 1. source 종류와 무관한 공통 유틸리티입니다.
 * 2. 로딩 dedupe를 담당하는 얇은 구현입니다.
 * =========================================================
 */
#endif
