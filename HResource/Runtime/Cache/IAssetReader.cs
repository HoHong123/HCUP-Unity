#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetHandler 캐시의 조회 책임만 분리한 ISP 계약 인터페이스.
 *
 * 주요 기능 ::
 * TryGet(key) - 점유 갱신 없이 단순 존재 확인.
 *
 * 사용법 ::
 * IAssetCache 가 본 인터페이스 + IAssetWriter + IAssetReleaser 를 합집합으로 노출.
 * 읽기 전용 cache decorator 구현 시 본 인터페이스만 구현 가능.
 *
 * 주의 ::
 * 조회는 점유를 바꾸지 않는다 - 점유 등록은 IAssetWriter.Save 만 담당한다.
 * "조회하며 점유를 올리는" API 는 의도적으로 두지 않는다 (AssetProvider 의 호출자별
 * 1:1 등록/해제 규약과 이중 카운트가 나기 때문. 2026-08-06 TryLoad 제거 참조).
 * =========================================================
 */
#endif

namespace HResource.Cache {
    public interface IAssetReader<TKey, TAsset> {
        bool TryGet(TKey key, out TAsset asset);
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-08-06 (수정) :: TryLoad 2종 제거 (케이스 리포트 01 TST-2)
 *
 * 변경 ::
 * bool TryLoad(TKey, out TAsset) / bool TryLoad(TKey, AssetOwnerId, out TAsset) 삭제.
 * 조회 계약은 TryGet 하나로 축소. using HResource.Subscription 도 함께 제거 (미사용).
 *
 * 이유 ::
 * dedupe 수정으로 provider 의 캐시 조회가 _TryPeekCache(=TryGet) 로 일원화된 뒤
 * TryLoad 는 호출자 0건이 되었다. 그런데 "조회하며 점유를 올린다" 는 의미가 인터페이스에
 * 남아 있으면, 후속 개발자가 이를 쓰는 순간 _GetAsync 의 호출자별 1:1 등록 규약과
 * 이중 카운트가 난다. 죽은 API 가 아니라 틀린 규약을 유도하는 API 라서 제거한다.
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
 * 2026-04-25 (최초 설계) :: IAssetReader 초기 구현
 *
 * IAssetCache 의 ISP (Interface Segregation Principle) 분리. Reader / Writer / Releaser 를
 * 별도 계약으로 분리하여 부분 책임만 노출하는 cache decorator 구현이 가능하도록.
 * 합집합인 IAssetCache 는 OnAssetRemoved 이벤트만 추가.
 * =========================================================
 */
#endif
