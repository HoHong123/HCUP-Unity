using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using HUtil.Inspector;
using HGame.Sample.Module;

namespace HGame.Flow.Sample {
    // Add this module into 'GameManager.modules' list.
    public class DemoPhaseModule : BaseGameModule {
        [HTitle("Demo Value")]
        [SerializeField]
        int delay;
        [SerializeField]
        string log;

        string time;


        private void Start() {
            time = TimeSpan.FromMilliseconds(delay).TotalSeconds.ToString("F2");
        }

        public async override UniTask OnEnterPrepare(GameContext ctx, CancellationToken ct) {
            DemoGameManager.Instance.StackLog($"{log} OnEnterPrepare start delay");
            await UniTask.Delay(delay);
            DemoGameManager.Instance.StackLog($"{log} OnEnterPrepare wait {time} seconds and done");
        }

        public async override UniTask OnEnterStart(GameContext ctx, CancellationToken ct) {
            DemoGameManager.Instance.StackLog($"{log} OnEnterStart start delay");
            await UniTask.Delay(delay);
            DemoGameManager.Instance.StackLog($"{log} OnEnterStart wait {time} seconds and done");
        }

        public async override UniTask OnEnterRun(GameContext ctx, CancellationToken ct) {
            DemoGameManager.Instance.StackLog($"{log} OnEnterRun start delay");
            await UniTask.Delay(delay);
            DemoGameManager.Instance.StackLog($"{log} OnEnterRun wait {time} seconds and done");
        }

        public async override UniTask OnEnterPause(GameContext ctx, CancellationToken ct) {
            DemoGameManager.Instance.StackLog($"{log} OnEnterPause start delay");
            await UniTask.Delay(delay);
            DemoGameManager.Instance.StackLog($"{log} OnEnterPause wait {time} seconds and done");
        }

        public async override UniTask OnEnterResume(GameContext ctx, CancellationToken ct) {
            DemoGameManager.Instance.StackLog($"{log} OnEnterResume start delay");
            await UniTask.Delay(delay);
            DemoGameManager.Instance.StackLog($"{log} OnEnterResume wait {time} seconds and done");
        }

        public async override UniTask OnEnterOver(GameContext ctx, CancellationToken ct) {
            DemoGameManager.Instance.StackLog($"{log} OnEnterOver start delay");
            await UniTask.Delay(delay);
            DemoGameManager.Instance.StackLog($"{log} OnEnterOver wait {time} seconds and done");
        }

        public async override UniTask OnEnterExit(GameContext ctx, CancellationToken ct) {
            DemoGameManager.Instance.StackLog($"{log} OnEnterExit start delay");
            await UniTask.Delay(delay);
            DemoGameManager.Instance.StackLog($"{log} OnEnterExit wait {time} seconds and done");
        }
    }
}
