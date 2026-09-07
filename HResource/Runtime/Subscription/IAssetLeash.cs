using Cysharp.Threading.Tasks;
using HResource.Data;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 순수 C# 객체용 자산 창구 계약. 소유자 하나에 묶인다.
 *
 * 주요 기능 ::
 * GetAsync / TryGet / Release - 전부 이 창구의 ownerId 로 귀속된다.
 * Dispose - 이 소유자의 점유를 일괄 반납한다.
 *
 * 사용법 ::
 * using var leash = source.Leash(this, anchor); 로 받는다. 블록을 벗어나면 반드시 반납된다.
 *
 * 주의 ::
 * Component 소유자는 이 계약이 필요 없다. source.GetAsync(this, ...) 가 파괴 시 자동 회수한다.
 * 순수 객체에는 파괴 이벤트가 없어 스스로는 회수하지 못한다. 그래서 anchor 를 필수로 받아
 * 그 파괴를 상한으로 삼는다. using 은 즉시 반납, anchor 는 최후 보증이다.
 * =========================================================
 */
#endif

namespace HResource.Subscription {
    public interface IAssetLeash<TKey, TAsset> : System.IDisposable {
        UniTask<TAsset> GetAsync(
            TKey key,
            AssetLoadMode loadMode,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst);

        bool TryGet(TKey key, out TAsset asset);
        bool Release(TKey key);
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 *
 * =========================================================
 * 2026-09-04 (최초 설계) :: IAssetLease 를 대체
 * =========================================================
 * 변경 ::
 * 단일 key 수명 핸들이던 IAssetLease 를 폐기하고, 소유자 단위 창구로 재정의했다.
 *
 * 이유 ::
 * key 단위 lease 는 호출자가 key 마다 핸들을 들게 만들어 실제로 아무도 쓰지 않았다(호출 0건).
 * 소유자 단위로 묶으면 using 한 번이 그 객체의 모든 점유를 덮는다.
 *
 * =========================================================
 * 2026-09-04 (수정) :: anchor 필수화
 * =========================================================
 * 변경 ::
 * 발급 경로가 Leash(object owner, Component anchor) 로 바뀌었다.
 *
 * 이유 ::
 * using 을 쓰지 않고 창구를 버리면 아무도 회수하지 못했다. anchor 를 필수 인자로 만들면
 * 상한 없는 점유를 코드로 적을 수 없다.
 * =========================================================
 */
#endif
