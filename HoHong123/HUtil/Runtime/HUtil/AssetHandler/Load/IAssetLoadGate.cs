using System;
using Cysharp.Threading.Tasks;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 동일 key 중복 로드를 합치는 게이트 계약 인터페이스 스크립트입니다.
 *
 * 주의사항 ::
 * 1. factory는 동일 key에 대해 재진입 가능성을 고려해야 합니다.
 * 2. 게이트는 로드 공유만 책임지고 cache 정책은 다루지 않습니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Load {
    public interface IAssetLoadGate<TKey, TAsset> {
        UniTask<TAsset> RunAsync(TKey key, Func<UniTask<TAsset>> factory);
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. key별 비동기 작업 공유 계약을 제공합니다.
 * 2. 중복 source 호출을 줄이는 경계를 제공합니다.
 *
 * 사용법 ::
 * 1. provider가 실제 source 로드를 감쌀 때 사용합니다.
 * 2. 같은 key 동시 요청이 많은 경로에 적용합니다.
 *
 * 이벤트 ::
 * 1. 직접 이벤트는 없습니다.
 * 2. 동일 key 요청이 하나의 task로 합쳐집니다.
 *
 * 기타 ::
 * 1. 로딩 자체의 정합성을 보조합니다.
 * 2. source 종류와는 무관한 공통 도구입니다.
 * =========================================================
 */
#endif
