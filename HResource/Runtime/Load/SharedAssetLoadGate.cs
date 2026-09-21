#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 동일 key 동시 로드를 공유하는 기본 게이트 구현. 합류자가 올 때만 완료 소스를 만든다.
 *
 * 주요 기능 ::
 * loadingTable 로 진행 중 key 추적. 같은 key 요청은 factory 1 회로 합쳐지고 합류자는 완료 소스를 await.
 * 완료 시 합류자는 같은 호출 스택에서 동기로 재개된다 (프레임을 넘기지 않음).
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
 * 성공 · 실패 모두 합류자를 깨우기 전에 loadingTable 에서 뺀다. Preserve 로 바꾸지 말 것 - 진행 중 두 번째 합류자가 던진다. 게이트는
 * 결과 캐시가 아니라 진행 중 작업 공유만 담당 - 캐시 정책은 상위 provider 가 가져감.
 * factory 안에서 같은 key 로 RunAsync 를 부르면 자기 자신에게 합류해 영원히 끝나지 않는다 (교착).
 * 등록을 factory 뒤로 옮기면 교착 대신 이중 로드 = refcount 잔존이 되므로, 교착을 택하고 계약으로 금지한다.
 * 동기 재개는 "합류자가 늦게 깨는" 틈을 줄일 뿐 닫지 않는다. 합류자 쪽 호출자가 받자마자 반납하면
 * 뒤이어 재개되는 최초 호출자가 반납된 에셋을 받는다 (획득 직후 동기 반납 경합, 별도 과제).
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
        // 진행 중인 key -> 합류자용 완료 소스. null 은 "진행 중이지만 합류자가 아직 없음" 이다.
        // UniTaskCompletionSource<T> 는 클래스 쪽이라 진행 중 동시 await 를 여럿 받는다.
        // 구조체 Core 나 Preserve 의 MemoizeSource 는 진행 중 continuation 슬롯이 하나라 두 번째 합류자가 던진다.
        readonly Dictionary<TKey, UniTaskCompletionSource<TAsset>> loadingTable = new();
        #endregion

        #region Public - Run
        public async UniTask<TAsset> RunAsync(TKey key, Func<UniTask<TAsset>> factory) {
            if (factory == null) {
                HLogger.Throw(new ArgumentNullException(nameof(factory), "[SharedAssetLoadGate] factory is null."));
            }

            if (loadingTable.TryGetValue(key, out var joined)) {
                // 완료 소스는 첫 합류자가 만든다. 합류가 없으면 할당도, 아무도 읽지 않는 예외도 생기지 않는다.
                if (joined == null) {
                    joined = new UniTaskCompletionSource<TAsset>();
                    loadingTable[key] = joined;
                }

                return await joined.Task;
            }

            loadingTable.Add(key, null);

            TAsset result;
            try {
                result = await factory.Invoke();
            }
            catch (Exception exception) {
                // 삼키지 않는다. 합류자에게 같은 예외를 넘긴 뒤 최초 호출자에게 그대로 다시 던진다.
                loadingTable.Remove(key, out var failed);
                failed?.TrySetException(exception);
                throw;
            }

            // 먼저 뺀다. 재개된 합류자가 같은 key 를 다시 요청하면 새 로드로 가야 한다.
            loadingTable.Remove(key, out var source);
            // 합류자들이 이 호출 스택 안에서 차례로 재개된다. 최초 호출자는 그 뒤에 반환한다.
            source?.TrySetResult(result);
            return result;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-21 (병합) :: master 의 게이트 구현과 dev/hong/fix/hresource-load-gate 의 구현을 병합
 *
 * 변경 ::
 * 두 줄기가 같은 날 각자 Task 공유를 UniTaskCompletionSource 공유로 바꿨다. 코드는 브랜치 쪽을 채택했다.
 * master 쪽의 "Preserve 로 바꾸지 말 것" 경고와 필드 주석 두 줄은 헤더와 필드에 옮겨 살렸다.
 *
 * 이유 ::
 * 브랜치 구현은 첫 합류자가 올 때만 완료 소스를 만든다. master 구현은 로드마다 항상 만들어,
 * 합류자 없는 실패에서 아무도 읽지 않은 예외가 미관측으로 다시 보고되고 TaskTracker 항목이 남았다.
 * 브랜치의 EditMode 테스트(HResource/Tests/Editor)도 브랜치 구현의 동작을 고정한다.
 *
 * 결과 ::
 * RunAsync 는 async 이고 최초 호출자는 factory 결과를 직접 받는다. 합류가 없으면 할당이 없다.
 * 아래 두 2026-09-21 항목은 각 줄기의 기록이며 고치지 않는다. "(수정 2)" 항목의 코드는 이 병합으로 대체됐다.
 *
 * 주의 ::
 * Unity 에디터 컴파일과 EditMode 테스트 실행은 병합 시점에 확인하지 못했다.
 *
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
 * 2026-09-21 (수정) :: 진행 중 공유를 Task 에서 지연 생성 UniTaskCompletionSource 로 교체
 *
 * 변경 ::
 * loadingTable 값을 Task<TAsset> 에서 UniTaskCompletionSource<TAsset> 로 바꿨다. 값은 첫 합류자가
 * 올 때 만들고 그 전에는 null 이다. finally 정리를 성공 · 실패 두 경로의 명시적 Remove 로 바꿨다.
 *
 * 이유 ::
 * Task 합류자의 await 는 continuation 을 UnitySynchronizationContext 에 넘겨 최초 호출자보다 늦게
 * 재개될 수 있다. UniTaskCompletionSource 는 TrySetResult 안에서 합류자를 동기로 재개한다.
 * 완료 소스를 항상 만들면 결함이 둘 생긴다 (UniTask 소스로 확인). 합류자 없는 실패는 아무도 읽지 않은
 * ExceptionHolder 가 소멸자에서 PublishUnobservedTaskException 으로 한 번 더 보고되고, 생성자가
 * TaskTracker 에 등록한 항목이 MarkHandled 없이 남는다. 지연 생성은 둘 다 없애고 합류 없는 흔한
 * 경우의 할당을 0 으로 만든다.
 *
 * 결과 ::
 * 합류가 없으면 AsTask() 할당도 완료 소스 할당도 없다. 취소는 TrySetException 이 TrySetCanceled 로
 * 넘겨 합류자에게 취소로 전달된다. 동작 검증은 HResource/Tests/Editor 의 EditMode 테스트.
 *
 * 주의 ::
 * 등록이 factory 호출 앞이라 같은 key 재진입은 교착한다 (헤더 참조). 이전 구현은 factory 를 먼저
 * 불러 재진입 시 이중 로드였다. 획득 직후 동기 반납 경합은 이 변경으로 닫히지 않는다.
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
