#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetProvider 의 조회 우선순위 정책을 정의하는 열거형.
 *
 * 주요 기능 ::
 * 5 가지 정책 (CacheFirst / LocalStoreFirst / LocalStoreOnly / SourceFirst / SourceOnly).
 * provider 의 _GetByFetchModeAsync switch 분기 키.
 *
 * 사용법 ::
 * AssetRequest 또는 GetAsync 의 인자로 전달. 도메인 코드가 호출 지점마다 정책을 명시 선언.
 * 기본값은 CacheFirst (메모리 우선, 가장 흔한 케이스).
 *
 * 주의 ::
 * load mode 와 다른 개념. provider 는 이 값에 따라 cache / store / source 호출 순서를 바꿈.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Data {
    public enum AssetFetchMode : byte {
        CacheFirst = 0,
        LocalStoreFirst = 1,
        LocalStoreOnly = 2,
        SourceFirst = 3,
        SourceOnly = 4,
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
 * 2026-04-25 (최초 설계) :: AssetFetchMode 초기 구현
 * =========================================================
 * fetch 정책을 enum 으로 명시화하여 도메인 코드가 호출 시 의도를 한 토큰으로 표현하게.
 * Cache vs LocalStore vs Source 의 우선순위·fallback 분기를 5 가지로 압축.
 * byte 기반으로 메모리 절약. 정책 5 종은 AssetProvider._GetByFetchModeAsync 의 switch 와
 * 1:1 대응 — enum 추가 시 switch 도 동시 갱신 필요.
 * =========================================================
 */
#endif
