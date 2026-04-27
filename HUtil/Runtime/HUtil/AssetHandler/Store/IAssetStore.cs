#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * cache 바깥 영속 저장소 (디스크/PlayerPrefs/SQLite 등) 계약 인터페이스. 선택 계층.
 *
 * 주요 기능 ::
 * HasAsync / LoadAsync / SaveAsync / DeleteAsync / ClearAsync — 비동기 CRUD.
 *
 * 사용법 ::
 * AssetProvider 의 fetch mode 가 LocalStoreFirst / LocalStoreOnly / SourceFirst 일 때만 호출.
 * 도메인 측에서 구현체 주입 (디스크 캐시 / PlayerPrefs / SQLite 등). 미주입 시 store 관련
 * fetch mode 는 InvalidOperationException 으로 차단.
 *
 * 주의 ::
 * store 는 선택 계층이라 없는 조합도 허용. asset 직렬화·저장 경로·삭제 규칙은 구현체 책임.
 * =========================================================
 */
#endif

using Cysharp.Threading.Tasks;

namespace HUtil.AssetHandler.Store {
    public interface IAssetStore<TKey, TAsset> {
        UniTask<bool> HasAsync(TKey key);
        UniTask<TAsset> LoadAsync(TKey key);
        UniTask SaveAsync(TKey key, TAsset asset);
        UniTask DeleteAsync(TKey key);
        UniTask ClearAsync();
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
 * 2026-04-25 (최초 설계) :: IAssetStore 초기 구현
 * =========================================================
 * "원격 source - 로컬 store - 메모리 cache" 3 계층 중 중간 계층의 추상화. 선택적이라
 * AssetProvider 가 null 주입을 허용. 구현체는 도메인 측 결정 (예: PlayerPrefs / SQLite /
 * Application.persistentDataPath 디렉토리). 5 가지 비동기 메서드로 CRUD + 전체 비움 표현.
 * =========================================================
 */
#endif
