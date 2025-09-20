#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 각 게임 모듈에 사용되는 실행될 타이밍 함수들을 가진 인터페이스입니다.
 * =========================================================
 */
#endif

using Cysharp.Threading.Tasks;
using System.Threading;
using HGame.Game.Flow;

namespace HGame.Game.Flow {
    public interface IGamePhaseModule {
        int Order { get; }
        UniTask OnEnterPrepare(GameContext ctx, CancellationToken ct);
        UniTask OnEnterStart(GameContext ctx, CancellationToken ct);
        UniTask OnEnterRun(GameContext ctx, CancellationToken ct);
        UniTask OnEnterPause(GameContext ctx, CancellationToken ct);
        UniTask OnEnterOver(GameContext ctx, CancellationToken ct);
        UniTask OnEnterPost(GameContext ctx, CancellationToken ct);
    }
}