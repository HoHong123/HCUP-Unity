using System.Threading;
using Cysharp.Threading.Tasks;
using HUtil.Logger;

namespace HGame.Flow.Sample {
    // Add this module into 'GameManager.modules' list.
    public class DemoPhaseModule : BaseGameModule {
        public override UniTask OnEnterPrepare(GameContext ctx, CancellationToken ct) {
            HLogger.Log("OnEnterPrepare");
            return UniTask.CompletedTask;
        }

        public override UniTask OnEnterStart(GameContext ctx, CancellationToken ct) {
            HLogger.Log("OnEnterStart");
            return UniTask.CompletedTask;
        }

        public override UniTask OnEnterRun(GameContext ctx, CancellationToken ct) {
            HLogger.Log("OnEnterRun");
            return UniTask.CompletedTask;
        }

        public override UniTask OnEnterPause(GameContext ctx, CancellationToken ct) {
            HLogger.Log("OnEnterPause");
            return UniTask.CompletedTask;
        }

        public override UniTask OnEnterOver(GameContext ctx, CancellationToken ct) {
            HLogger.Log("OnEnterOver");
            return UniTask.CompletedTask;
        }
    }
}