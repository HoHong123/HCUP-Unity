using Cysharp.Threading.Tasks;
using HResource.Data;
using HResource.Subscription;
using UnityEngine;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 팩토리가 반환하는 자산 창고 계약. 소유자 없이 자산을 꺼내는 경로가 존재하지 않는다.
 *
 * 주요 기능 ::
 * GetAsync(Component, ...) - Component 소유자용. 파괴 시 자동 회수된다.
 * Release / ReleaseOwner(Component) - 정상 반납 경로.
 * Leash(object, Component anchor) - 순수 C# 객체용 창구. anchor 가 수명 상한을 준다.
 * ReclaimOrphans() - 소유자를 잃은 점유의 수동 일괄 회수. 부르는 시점은 개발자가 정한다.
 *
 * 사용법 ::
 * AssetProviderFactory 로 만든다. 소비자는 GetAsync(this, key, mode) 하나만 알면 된다.
 * 다 쓰면 Release(this, key) 를 부르는 것이 정상 플로우다.
 *
 * 주의 ::
 * 이 계약에 GetAsync(key, ...) 는 없다. 소유자를 생략한 획득을 타입 수준에서 막기 위해서다.
 * Leash 의 anchor 도 같은 이유로 필수다. 상한 없는 점유를 코드로 적을 수 없게 한다.
 * 순수 C# 소유자의 창구 반납은 의무다. anchor 에 맡기면 그 점유가 anchor 수명까지 유지된다.
 * Component 소유자에는 해당하지 않는다. 프로브가 자기 GameObject 에 붙어 회수 시점이
 * 소유자 수명과 일치하므로, 명시 반납은 점유 기간을 줄이려는 선택이다.
 *
 * 거부 계약 ::
 * 수명 상한을 걸 수 없으면 획득을 성립시키지 않는다. 파괴가 진행 중인 GameObject 에는
 * 파괴 프로브를 붙일 수 없으므로, 그 소유자의 GetAsync 는 로드를 시작하지 않고 default 를,
 * Leash 는 null 을 돌려준다. 두 경우 모두 HLogger.Error 로 원인을 남긴다.
 * 즉 teardown 도중의 획득은 실패할 수 있다. 자산은 파괴 전에 확보할 것.
 * Dispose 는 provider 자체 폐기다. 소유자 단위 반납은 ReleaseOwner 나 ICSharpAssetLeash.Dispose 다.
 *
 * 유일한 사각 :: Destroy(component)
 * 프로브는 GameObject 파괴만 본다. Destroy(gameObject) 는 잡지만, Destroy(component) 로
 * 소유자 컴포넌트만 지우면 GameObject 와 프로브가 살아있어 통지가 오지 않는다.
 * 그 점유는 provider 폐기까지 남으며 에디터 Owner Watcher 의 ORPHAN 행으로만 드러난다.
 * 잡으려면 매 프레임 소유자 순회가 필요한데 그 비용은 이 사각의 발생 빈도에 비해 크다.
 * 소유자 컴포넌트만 떼어낼 일이 있으면 그 전에 Release / ReleaseOwner 를 직접 부를 것.
 * =========================================================
 */
#endif

namespace HResource.Provider {
    public interface IAssetSource<TKey, TAsset> : System.IDisposable {
        #region Component Owner
        UniTask<TAsset> GetAsync(
            Component owner,
            TKey key,
            AssetLoadMode loadMode,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst);

        bool Release(Component owner, TKey key);
        int ReleaseOwner(Component owner);
        #endregion

        #region Plain Owner
        /// <summary>
        /// 순수 C# 소유자용 창구. anchor 가 수명 상한이다.
        /// 다 쓰면 반드시 Dispose 한다. anchor 는 최후 방어선이지 정상 반납 경로가 아니다.
        /// </summary>
        ICSharpAssetLeash<TKey, TAsset> Leash(object owner, Component anchor);
        #endregion

        #region Owner Independent
        bool TryGet(TKey key, out TAsset asset);
        void ClearCache();

        /// <summary> 소유자를 잃은 점유의 수동 일괄 회수. 반환값은 회수한 key 수. 자동 경로 없음 </summary>
        int ReclaimOrphans();
        UniTask ClearStoreAsync();
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-08 (수정) :: Leash 반납 규격을 의무형으로 명시
 *
 * 변경 ::
 * 창구 반납을 허용형에서 의무형으로 다시 적었다. "Dispose 하지 않고 버려도 anchor 가
 * 파괴되면 회수된다" 를 "다 쓰면 반드시 Dispose 한다. anchor 는 최후 방어선" 으로 바꿨다.
 * 적용 범위를 순수 C# 소유자로 못박고, Component 소유자는 해당 없음을 함께 적었다.
 *
 * 이유 ::
 * 같은 날 liveEntries 를 제거해 GC 된 순수 소유자를 ReclaimOrphans() 로 걷지 못하게
 * 됐다. 회수 시점이 anchor 파괴 하나로 좁아졌으므로 호출자가 지켜야 할 몫이 커졌다.
 * 종전 문구는 반납을 빠뜨려도 되는 것처럼 읽혔다.
 *
 * 결과 ::
 * 계약이 실제 회수 능력과 일치한다. 호출자가 anchor 에 기대는 비용을 문장에서 안다.
 *
 * 주의 ::
 * 강제 수단은 없다. 반납을 빠뜨려도 경고가 나오지 않는다. anchor 파괴 시점에 경고를
 * 내는 안은 검토 후 기각했다. anchor 에 맡기는 것 자체는 계약이 허용하는 종료라
 * 정상 사용을 실수로 신고하게 된다. 규격은 문서와 주석으로만 세운다.
 *
 * =========================================================
 * 2026-09-07 (수정) :: ReclaimOrphans 설명 정정
 *
 * 변경 ::
 * "살아있는 회수 창구가 없는" 을 "소유자가 이미 사라졌는데 남아있는" 으로 고쳤다.
 *
 * 이유 ::
 * 판정 기준이 창구 생존에서 소유자 생존으로 바뀌었는데 문구가 옛 기준을 가리키고 있었다.
 *
 * 결과 ::
 * 계약 설명과 구현이 같은 것을 말한다.
 *
 * 주의 ::
 * 인자 없는 형태와 반환 의미는 그대로다. 바뀐 것은 무엇을 orphan 으로 보는가뿐이다.
 *
 * =========================================================
 * 2026-09-06 (수정) :: ReclaimOrphans 추가
 *
 * 변경 ::
 * 창구를 잃은 점유를 일괄 회수하는 public 메서드를 계약에 넣었다.
 *
 * 이유 ::
 * 개발자가 원하는 시점에 코드로 직접 정리할 수단이 없었다. 에디터 창을 열어야만 가능했다.
 *
 * 결과 ::
 * assetProvider.ReclaimOrphans() 로 런타임에서 부를 수 있다. 자동으로 도는 경로는 없다.
 *
 * 주의 ::
 * 인자를 받지 않는다. AssetOwnerId 는 어셈블리 밖에서 만들 수 없고,
 * int 를 public 런타임에 열면 임의 정수로 남의 점유를 내려놓을 수 있다.
 *
 * =========================================================
 * 2026-09-04 (최초 설계) :: IAssetProvider 를 대체
 *
 * 변경 ::
 * IAssetProvider 를 폐기하고, 소유자를 요구하는 멤버만 남긴 IAssetSource 로 교체했다.
 * ownerId 를 직접 받던 오버로드는 전부 AssetProvider 의 internal 로 강등했다.
 *
 * 이유 ::
 * AssetOwnerId 는 struct 라 default 가 항상 존재해 매개변수로는 강제가 불가능했다.
 * 자산 접근 멤버를 소유자 경로 뒤로 옮기는 것이 타입으로 강제하는 유일한 방법이다.
 *
 * 결과 ::
 * source.GetAsync(key, mode) 는 컴파일되지 않는다. 우회 경로가 계약에서 사라졌다.
 * =========================================================
 */
#endif
