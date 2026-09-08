using Cysharp.Threading.Tasks;
using HResource.Data;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 순수 C# 객체용 자산 창구 계약. 소유자 하나에 묶인다.
 *
 * 주요 기능 ::
 * GetAsync / Release - 이 창구의 ownerId 로 귀속된다.
 * TryGet - 캐시 조회일 뿐이라 점유를 만들지 않는다. 소유자와 무관하다.
 * Dispose - 이 소유자의 점유를 일괄 반납한다.
 *
 * 사용법 ::
 * using var leash = source.Leash(this, anchor); 로 받는다. 블록을 벗어나면 반드시 반납된다.
 * 창구를 필드로 들면 소유자가 IDisposable 을 구현하고 자기 Dispose 에서 창구를 닫는다.
 *
 * 규격 ::
 * 이 규격은 순수 C# 소유자 전용이다. Component 소유자는 자기 GameObject 파괴가 곧
 * 회수라 해당하지 않는다.
 * 반납은 의무다. 다 쓴 시점에 Dispose 를 부른다.
 * 창구를 필드로 들면 소유자가 IDisposable 을 구현하고 자기 Dispose 에서 창구를 닫는다.
 * anchor 는 비정상 종료를 막는 최후 방어선이지 정상 반납 경로가 아니다.
 * anchor 에 맡기면 그 점유가 anchor 수명까지 유지된다. anchor 가 씬 루트면 씬이 끝날 때까지다.
 * 2026-09-08 부터 GC 된 순수 소유자는 ReclaimOrphans() 로도 걷히지 않는다. 근거는 Dev Log.
 *
 * 주의 ::
 * Component 소유자는 이 계약이 필요 없다. source.GetAsync(this, ...) 가 파괴 시 자동 회수한다.
 * 순수 객체에는 파괴 이벤트가 없어 스스로는 회수하지 못한다. 그래서 anchor 를 필수로 받아
 * 그 파괴를 상한으로 삼는다.
 * =========================================================
 */
#endif

namespace HResource.Subscription {
    public interface ICSharpAssetLeash<TKey, TAsset> : System.IDisposable {
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
 * 2026-09-08 (수정 3) :: 타입 이름에 CSharp 을 넣는다
 *
 * 변경 ::
 * IAssetLeash -> ICSharpAssetLeash, 중첩 구현 AssetLeash -> CSharpAssetLeash.
 * 파일과 .meta 도 함께 옮겼다(GUID 보존). 참조 14 파일을 갱신했다.
 *
 * 이유 ::
 * 이 타입은 순수 C# 소유자 전용인데 이름만 봐서는 Component 소유자도 쓰는 것으로
 * 읽힌다. 반납 의무가 이쪽에만 붙으므로 적용 범위가 이름에서 드러나야 한다.
 * 사람과 에이전트 모두 파일명 단계에서 걸러낼 수 있게 하려는 것이다.
 *
 * 결과 ::
 * 타입 이름에 CSharp 이 있으면 순수 C# 소유자 전용이라는 규칙이 선다.
 *
 * 주의 ::
 * IAssetSource 는 개명 대상이 아니다. GetAsync(Component) / Release(Component) /
 * ReleaseOwner(Component) 를 직접 들고 있어 Component 경로가 본체다. Leash 하나만
 * 순수 C# 용이므로 여기에 CSharp 을 붙이면 나머지 멤버를 잘못 표기하게 된다.
 * 로그 태그 [AssetLeash] 는 그대로 둔다. Fingerprint 등 Component 경로도 이 태그로
 * 남기므로 계층 이름이지 타입 이름이 아니다.
 *
 * =========================================================
 * 2026-09-08 (수정 2) :: 반납 규격을 의무형으로 명시
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
 * 2026-09-08 (수정) :: TryGet 의 귀속 설명을 사실로 정정
 *
 * 변경 ::
 * 헤더가 GetAsync / TryGet / Release 셋 다 ownerId 로 귀속된다고 적고 있었다.
 * TryGet 을 그 목록에서 빼고 캐시 조회임을 따로 적었다.
 *
 * 이유 ::
 * CSharpAssetLeash.TryGet 은 provider.TryGet 을 그대로 부르고, 그쪽은 ownerId 를 받지
 * 않는다(AssetProvider.cs). 헤더만 보고 쓰면 점유가 생긴 줄 알고 반납을 빠뜨린다.
 *
 * 결과 ::
 * 이 인터페이스에서 점유를 만드는 것은 GetAsync 하나임이 헤더에서 드러난다.
 *
 * 주의 ::
 * 코드는 바뀌지 않았다. 계약을 바꾼 것이 아니라 계약 설명을 실제와 맞춘 것이다.
 *
 * =========================================================
 * 2026-09-04 (최초 설계) :: IAssetLease 를 대체
 *
 * 변경 ::
 * 단일 key 수명 핸들이던 IAssetLease 를 폐기하고, 소유자 단위 창구로 재정의했다.
 *
 * 이유 ::
 * key 단위 lease 는 호출자가 key 마다 핸들을 들게 만들어 실제로 아무도 쓰지 않았다(호출 0건).
 * 소유자 단위로 묶으면 using 한 번이 그 객체의 모든 점유를 덮는다.
 *
 * =========================================================
 * 2026-09-04 (수정) :: anchor 필수화
 *
 * 변경 ::
 * 발급 경로가 Leash(object owner, Component anchor) 로 바뀌었다.
 *
 * 이유 ::
 * using 을 쓰지 않고 창구를 버리면 아무도 회수하지 못했다. anchor 를 필수 인자로 만들면
 * 상한 없는 점유를 코드로 적을 수 없다.
 * =========================================================
 */
#endif
