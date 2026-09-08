#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetHandler 캐시의 해제 책임만 분리한 ISP 계약 인터페이스.
 *
 * 주요 기능 ::
 * Release(key, ownerId) - 그 소유자의 이 key 점유 해제.
 * 소유자 없는 Release 오버로드는 2026-09-04 에 삭제했다.
 * ReleaseOwner(ownerId) - owner 가 잡은 모든 key 일괄 해제 (역인덱스 활용).
 * ReleaseAll() / Clear() - 전역 해제 vs 전체 비움.
 *
 * 사용법 ::
 * AssetProvider 가 owner lifecycle 종료 시 ReleaseOwner 호출. 도메인 코드는 단일 점유
 * 해제만 필요하면 provider 의 Release(owner, key) 를 부르거나 ICSharpAssetLeash.Dispose 로 위임.
 *
 * 주의 ::
 * key 해제와 owner 해제의 의미를 구현체에서 명확히 나눠야 함. ReleaseAll 과 Clear 의 차이도
 * 구현체가 일관되게 유지 (MemoryAssetCache 는 동일 동작 - 모든 Item 제거 + OnAssetRemoved 발생).
 * =========================================================
 */
#endif

using HResource.Subscription;

namespace HResource.Cache {
    public interface IAssetReleaser<TKey> {
        bool Release(TKey key, AssetOwnerId ownerId);
        int ReleaseOwner(AssetOwnerId ownerId);
        void ReleaseAll();
        void Clear();
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-07 (수정) :: CaptureOwners 제거
 *
 * 변경 ::
 * 어제 넣은 CaptureOwners 를 계약에서 뺐다. 헤더 항목과 using 도 함께 되돌렸다.
 *
 * 이유 ::
 * 캐시 쪽 소유자 열거로 orphan 을 판정하려던 설계가 틀린 것으로 드러나 호출처가 0 이 됐다.
 *
 * 결과 ::
 * 해제 계약이 원래의 네 메서드로 돌아왔다.
 *
 * 주의 ::
 * 소유자 열거가 다시 필요하면 캐시가 아니라 leash 계층에서 얻는다. 생존을 아는 쪽은 그쪽이다.
 *
 * =========================================================
 * 2026-09-06 (수정) :: CaptureOwners 추가
 *
 * 변경 ::
 * 점유를 들고 있는 소유자 신원을 버퍼로 내주는 CaptureOwners 를 계약에 넣었다.
 *
 * 이유 ::
 * orphan 정리는 "캐시가 아는 소유자" 목록에서 시작한다. 캐시 밖에서는 그 목록을 볼 수 없었다.
 *
 * 결과 ::
 * AssetProvider.ReclaimOrphans 가 이 목록을 받아 생존 판정 뒤 회수한다.
 *
 * 주의 ::
 * 버퍼를 뜬 뒤 회수하면 소유자 테이블이 바뀐다. 순회 도중 회수하지 말 것.
 *
 * =========================================================
 * 2026-04-26 (수정) :: 헤더 형틀 통합 + Dev Log 형식 도입
 *
 * 변경 ::
 * 기존 헤더 (도입 + 주의사항) 에 "주요 기능 / 사용법" 섹션 추가하여 §11 형틀 통일.
 * 하단 Dev Log 영역 추가. 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드.
 *
 * 이유 ::
 * 글로벌 CLAUDE.md §11 룰 일괄 적용.
 *
 * =========================================================
 * 2026-04-25 (최초 설계) :: IAssetReleaser 초기 구현
 *
 * IAssetCache 의 ISP 분리 (Reader/Writer/Releaser 3 갈래). 해제 단일 책임만 노출.
 * 5 가지 메서드는 점유 단위 (단일 key / 단일 owner / 전체) 와 의미 (해제 vs 비움) 의 조합.
 * ReleaseOwner 가 ownerTable 역인덱스를 활용해 owner 가 잡은 모든 key 를 일괄 회수.
 * =========================================================
 */
#endif
