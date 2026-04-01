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
        int delayMiliseconds;
        [SerializeField]
        string log;

        string time;


        private void Start() {
            time = TimeSpan.FromMilliseconds(delayMiliseconds).TotalSeconds.ToString("F2");
        }

        public async override UniTask OnEnterPrepare(GameContext ctx, CancellationToken ct) {
            DemoGameManager.Instance.StackLog($"<color=#FFD166>{log} OnEnterPrepare</color> start");
            await UniTask.Delay(delayMiliseconds);
            DemoGameManager.Instance.StackLog($"<color=#FFD166>{log} OnEnterPrepare</color> wait <color=#FFD166>{time}</color> seconds and done");
        }

        public async override UniTask OnEnterStart(GameContext ctx, CancellationToken ct) {
            DemoGameManager.Instance.StackLog($"<color=#3ED9A0>{log} OnEnterStart</color> start");
            await UniTask.Delay(delayMiliseconds);
            DemoGameManager.Instance.StackLog($"<color=#3ED9A0>{log} OnEnterStart</color> wait <color=#3ED9A0>{time}</color> seconds and done");
        }

        public async override UniTask OnEnterRun(GameContext ctx, CancellationToken ct) {
            DemoGameManager.Instance.StackLog($"<color=#4DA3FF>{log} OnEnterRun</color> start");
            await UniTask.Delay(delayMiliseconds);
            DemoGameManager.Instance.StackLog($"<color=#4DA3FF>{log} OnEnterRun</color> wait <color=#4DA3FF>{time}</color> seconds and done");
        }

        public async override UniTask OnEnterPause(GameContext ctx, CancellationToken ct) {
            DemoGameManager.Instance.StackLog($"<color=#B388EB>{log} OnEnterPause</color> start");
            await UniTask.Delay(delayMiliseconds);
            DemoGameManager.Instance.StackLog($"<color=#B388EB>{log} OnEnterPause</color> wait <color=#B388EB>{time}</color> seconds and done");
        }

        public async override UniTask OnEnterResume(GameContext ctx, CancellationToken ct) {
            DemoGameManager.Instance.StackLog($"<color=#BFC0C0>{log} OnEnterResume</color> start");
            await UniTask.Delay(delayMiliseconds);
            DemoGameManager.Instance.StackLog($"<color=#BFC0C0>{log} OnEnterResume</color> wait <color=#BFC0C0>{time}</color> seconds and done");
        }

        public async override UniTask OnEnterOver(GameContext ctx, CancellationToken ct) {
            DemoGameManager.Instance.StackLog($"<color=#F4A261>{log} OnEnterOver</color> start");
            await UniTask.Delay(delayMiliseconds);
            DemoGameManager.Instance.StackLog($"<color=#F4A261>{log} OnEnterOver</color> wait <color=#F4A261>{time}</color> seconds and done");
        }

        public async override UniTask OnEnterExit(GameContext ctx, CancellationToken ct) {
            DemoGameManager.Instance.StackLog($"<color=#EF6F6C>{log} OnEnterExit</color> start");
            await UniTask.Delay(delayMiliseconds);
            DemoGameManager.Instance.StackLog($"<color=#EF6F6C>{log} OnEnterExit</color> wait <color=#EF6F6C>{time}</color> seconds and done");
        }
    }
}
