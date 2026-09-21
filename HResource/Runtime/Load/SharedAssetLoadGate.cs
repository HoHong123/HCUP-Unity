#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 동일 key 동시 로드를 공유하는 기본 게이트 구현. UniTaskCompletionSource 하나로 대기자를 모은다.
 *
 * 주요 기능 ::
 * loadingTable 로 진행 중 로드의 UniTaskCompletionSource 추적. 같은 key 요청은 그 .Task 에 합류해 source 호출 1 회.
 * 완료 시 대기자는 SynchronizationContext 를 거치지 않고 그 자리에서 동기로 재개된다. Task 할당이 없다.
 *
 * 사용법 ::
 * AssetProvider 가 _GetAsync 에서 fetch mode 전 구간을 본 게이트로 감쌈. 우회 경로 0건.
 *
 * 주의 :: 이 게이트는 성능 최적화가 아니라 정합성 장치다. 제거하면 영구 잔존이 발생한다.
 * AddressableAssetLoader 는 handleTable 조회가 await 앞, 등록이 await 뒤다. 게이트가 없으면
 * 동시 요청 2건이 모두 LoadAssetAsync 를 불러 Addressables refcount 가 2 로 오르고,
 * handleTable 은 뒤엣것으로 덮이며, Release 1회로는 0 에 도달하지 못한다.
 * HResource 가 refcount 를 1 로 고정하기로 한 선택을 성립시키는 것이 이 게이트다.
 *
 * Resources 축도 병합된다. ResourcesAssetLoader 가 Resources.LoadAsync 를 await 하므로 진행 중 구간이 있다.
 * Resources 는 핸들 refcount 가 없어 게이트가 없어도 잔존은 생기지 않는다. 합쳐지는 것은 중복 로드 요청이다.
 * factory 가 던져도 표에서 빼고 예외를 대기자 전원에게 전달한다. Preserve 로 바꾸지 말 것 - 진행 중 두 번째 합류자가 던진다. 게이트는
 * 결과 캐시가 아니라 진행 중 작업 공유만 담당 - 캐시 정책은 상위 provider 가 가져감.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using HDiagnosis.Logger;

namespace HResource.Load {
    public sealed class SharedAssetLoadGate<TKey, TAsset> : IAssetLoadGate<TKey, TAsset> {
        #region Private - Fields
        // UniTaskCompletionSource<T> 는 클래스 쪽이라 진행 중 동시 await 를 여럿 받는다.
        // 구조체 Core 나 Preserve 의 MemoizeSource 는 진행 중 continuation 슬롯이 하나라 두 번째 합류자가 던진다.
        readonly Dictionary<TKey, UniTaskCompletionSource<TAsset>> loadingTable = new();
        #endregion

        #region Public - Run
        public UniTask<TAsset> RunAsync(TKey key, Func<UniTask<TAsset>> factory) {
            if (factory == null) {
                HLogger.Throw(new ArgumentNullException(nameof(factory), "[SharedAssetLoadGate] factory is null."));
            }

            if (loadingTable.TryGetValue(key, out var runningSource)) {
                return runningSource.Task;
            }

            var newSource = new UniTaskCompletionSource<TAsset>();
            loadingTable[key] = newSource;
            _RunFactoryAsync(key, factory, newSource).Forget();
            return newSource.Task;
        }
        #endregion

        #region Private - Run
        private async UniTaskVoid _RunFactoryAsync(
            TKey key,
            Func<UniTask<TAsset>> factory,
            UniTaskCompletionSource<TAsset> source) {

            TAsset asset;
            try {
                asset = await factory.Invoke();
            }
            catch (Exception e) {
                // 삼키지 않는다. 합류한 대기자 전원에게 그대로 전달한다 (OperationCanceledException 은 취소로 변환된다).
                // 예외 타입을 좁히지 않는 이유 : factory 가 던지는 것은 무엇이든 대기자의 몫이고, 여기서 빠뜨린 타입은 대기자를 영원히 멈춘다.
                loadingTable.Remove(key);
                source.TrySetException(e);
                return;
            }

            // 순서 주의 : 표에서 먼저 뺀다. TrySetResult 는 대기자를 그 자리에서 동기로 재개하므로,
            // 재개된 대기자가 같은 key 를 다시 요청하면 끝난 source 가 아니라 새 로드를 타야 한다.
            loadingTable.Remove(key);
            source.TrySetResult(asset);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-21 (수정 2) :: Task 공유를 UniTaskCompletionSource 공유로 교체
 *
 * 변경 ::
 * loadingTable 의 값을 Task<TAsset> 에서 UniTaskCompletionSource<TAsset> 로 바꿨다.
 * RunAsync 는 async 가 아니다. 최초 호출자가 source 를 만들고 factory 는 _RunFactoryAsync(UniTaskVoid) 가 돌린다.
 * 결과는 TrySetResult, 예외는 TrySetException 으로 대기자 전원에게 넘긴다. 표에서 빼는 것이 완료 통지보다 먼저다.
 *
 * 이유 ::
 * AsTask() 는 로드마다 Task 를 할당하고, await Task 의 재개가 UnitySynchronizationContext 의 Post 를 거쳐
 * 대기자 전원이 컨텍스트 큐 처리 시점까지 밀렸다. 사용자 지시로 UniTask 단일로 전환한다.
 * Preserve 는 다시 쓰지 않았다. UniTask 2.5.11 의 MemoizeSource.OnCompleted 는 진행 중이면 continuation 을
 * 원본(단일 슬롯)에 그대로 넘겨 두 번째 합류자가 "Already continuation registered" 로 던진다.
 * 2026-05-01 의 되돌림(LOG-20260501-2) 원인과 같다. UniTaskCompletionSource<T> 클래스는
 * secondaryContinuationList 로 동시 대기자를 여럿 받는다.
 *
 * 결과 ::
 * 로드당 할당은 source 1 개(+ 대기자 2 명 이상이면 목록 1 개). 대기자는 완료 즉시 같은 호출 스택에서 재개된다.
 *
 * 주의 ::
 * 재개가 동기라 TrySetResult 안에서 대기자 코드가 연달아 실행된다. 대기자 하나가 던지면 UniTask 가
 * 미관측 예외로 보고하고 나머지 대기자는 계속 재개된다.
 * catch (Exception) 는 예외를 삼키는 것이 아니라 대기자에게 전달하는 경로다. 좁히면 빠진 타입에서 대기자가 멈춘다.
 * Unity 컴파일은 이 세션에서 확인하지 못했다.
 *
 * =========================================================
 * 2026-09-21 (수정) :: Resources 축 서술 정정
 *
 * 변경 ::
 * 헤더의 "Resources 축은 대상이 아니다" 를 "Resources 축도 병합된다" 로 교체.
 *
 * 이유 ::
 * 같은 날 ResourcesAssetLoader 가 Resources.Load 동기 호출에서 Resources.LoadAsync await 로 바뀌었다.
 * 진행 중 구간이 생겨 같은 key 동시 요청이 이 게이트에서 합쳐진다.
 *
 * 결과 ::
 * 코드 변경 없음. 주석이 실제 동작과 일치한다.
 *
 * 주의 ::
 * 아래 2026-09-08 항목의 "Resources 축은 대상이 아니다" 는 그 시점의 사실이다.
 *
 * =========================================================
 * 2026-09-08 (수정) :: 존재 이유를 성능에서 정합성으로 정정
 *
 * 변경 ::
 * 헤더의 "source 호출 비용 절감" 서술을 refcount 고정 유지 근거로 교체
 *
 * 이유 ::
 * Addressables 는 중복 로드를 막지만 참조 수를 올리며 막는다. HResource 는 그 수를 1 로
 * 고정하므로 진입 자체를 직렬화해야 한다. 성능으로 읽히면 제거 대상으로 오해된다
 *
 * 결과 ::
 * 게이트 제거가 왜 영구 잔존을 만드는지 헤더만 읽어도 드러난다
 *
 * 주의 ::
 * Resources 축은 대상이 아니다. LoadAsync 가 동기라 병합할 구간이 없다
 *
 * =========================================================
 * @Jason - PKH 2026.05.01 RunAsync 의 캐시 task 를 UniTask → Task 변환으로 정정 (Preserve 정정) [LOG-20260501-2]
 * - dedupe 게이트의 fan-out 의도와 multi-continuation 정합 위해 Task 로 전환.
 * =========================================================
 * @Jason - PKH 2026.05.01 RunAsync 의 캐시 task 를 Preserve 처리 [LOG-20260501-1]
 * - UniTask single-continuation 제약 회피 위해 Preserve 적용 (이후 LOG-20260501-2 로 정정됨).
 * =========================================================
 * 2026-04-26 (수정) :: 헤더 형틀 통합 + Dev Log 형식 도입 [LOG-20260426-1]
 * - 글로벌 §11 형틀 통일 + #if UNITY_EDITOR 가드 적용.
 * =========================================================
 * > 이전 엔트리는 docs/history/HUtil/Runtime/HUtil/AssetHandler/Load/SharedAssetLoadGate.md 참조 (총 4 엔트리)
 * =========================================================
 */
#endif
