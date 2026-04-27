#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 동일 key 동시 로드를 공유하는 기본 게이트 구현. 17 줄짜리 dedupe + finally cleanup.
 *
 * 주요 기능 ::
 * loadingTable 로 진행 중 task 추적. 같은 key 요청은 한 UniTask 로 합쳐 source 호출 1 회.
 *
 * 사용법 ::
 * AssetProvider 가 _GetAsync 에서 source 로드를 본 게이트로 감쌈. 동일 key 연속 요청이
 * 발생하는 환경 (UI 다중 패널 같은 sprite 동시 요청 등) 에서 source 호출 비용 절감.
 *
 * 주의 ::
 * factory 는 예외 발생 시에도 정리 흐름 고려 (finally 에서 loadingTable.Remove). 게이트는
 * 결과 캐시가 아니라 진행 중 작업 공유만 담당 — 캐시 정책은 상위 provider 가 가져감.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;

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
 * Dev Log
 * =========================================================
 *
 * =========================================================
 * 2026-04-26 (수정) :: 헤더 형틀 통합 + Dev Log 형식 도입
 * =========================================================
 * 변경 ::
 * 기존 헤더 (상단 도입+주의사항 + 하단 주요기능/사용법/이벤트/기타) 를 한 곳에 통합하여
 * §11 형틀 통일. 하단 Dev Log 영역 추가. 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드.
 *
 * 이유 ::
 * 글로벌 CLAUDE.md §11 룰 일괄 적용.
 *
 * =========================================================
 * 2026-04-25 (최초 설계) :: SharedAssetLoadGate 초기 구현
 * =========================================================
 * 동일 key 동시 로드 dedupe 의 가장 단순한 구현 — Dictionary 한 개 + try/finally 한 개.
 * 같은 key 가 동시에 N 번 요청되면 첫 호출만 factory 실행, 나머지 N-1 호출은 진행 중
 * UniTask 를 await. finally 에서 loadingTable.Remove 로 task 완료 후 즉시 정리 — 다음
 * 호출은 다시 factory 실행 가능. 17 줄짜리 본문이 핵심 가치 (성능 + 방어적 cleanup 동시 달성).
 * =========================================================
 */
#endif
