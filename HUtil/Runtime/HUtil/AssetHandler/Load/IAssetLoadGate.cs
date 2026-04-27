#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 동일 key 동시 로드를 합치는 게이트 계약 인터페이스.
 *
 * 주요 기능 ::
 * RunAsync(key, factory) — key 별 비동기 작업을 공유. 같은 key 동시 요청은 한 UniTask 로 dedupe.
 *
 * 사용법 ::
 * AssetProvider 가 _GetAsync 에서 source 로드 호출을 본 게이트로 감쌈. 같은 key 가 동시에
 * N 번 요청되어도 source factory 는 1 번만 실행, 나머지 N-1 호출은 진행 중 task 를 await.
 *
 * 주의 ::
 * factory 는 동일 key 에 대해 재진입 가능성 고려. 게이트는 진행 중 작업 공유만 책임지고
 * cache 정책 (저장/만료) 은 다루지 않음 — 책임 분리.
 * =========================================================
 */
#endif

using System;
using Cysharp.Threading.Tasks;

namespace HUtil.AssetHandler.Load {
    public interface IAssetLoadGate<TKey, TAsset> {
        UniTask<TAsset> RunAsync(TKey key, Func<UniTask<TAsset>> factory);
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
 * 기존 헤더 (도입 + 주의사항) 에 "주요 기능 / 사용법" 섹션 추가하여 §11 형틀 통일.
 * 하단 Dev Log 영역 추가. 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드.
 *
 * 이유 ::
 * 글로벌 CLAUDE.md §11 룰 일괄 적용.
 *
 * =========================================================
 * 2026-04-25 (최초 설계) :: IAssetLoadGate 초기 구현
 * =========================================================
 * 동일 key 동시 로드 dedupe 라는 단일 책임만 노출. 결과 캐시 (cache 정책) 와 명확히 분리.
 * 기본 구현체 SharedAssetLoadGate 가 17 줄짜리 finally cleanup 패턴으로 진행 중 task 공유.
 * 게이트 자체는 source 종류와 무관한 공통 도구 — Resources/Addressable 어느 loader 에도 적용.
 * =========================================================
 */
#endif
