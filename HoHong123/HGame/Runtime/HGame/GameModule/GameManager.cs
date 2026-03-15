using System.Threading;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using HUtil.Inspector;

namespace HGame.Flow {
    public class GameManager<TSelf>:
        HUtil.Core.SingletonBehaviour<TSelf>
        where TSelf : GameManager<TSelf> {
        [HTitle("Flow")]
        [SerializeField]
        protected bool autoPrepareOnEnable = true;
        [SerializeField]
        protected GamePhaseType phase = GamePhaseType.None;
        [SerializeField]
        [Tooltip("Modules (children or same GameObject)")]
        List<BaseGameModule> modules = new();

        GameContext context = new GameContext();
        CancellationTokenSource phaseCts;

        public GamePhaseType Phase => phase;


        protected override void Awake() {
            modules.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        protected virtual void OnEnable() { }

        protected virtual void Start() {
            if (autoPrepareOnEnable) GamePrepareAsync();
        }

        protected virtual void OnDisable() {
            phaseCts?.Cancel();
            phaseCts?.Dispose();
            phaseCts = null;
            phase = GamePhaseType.None;
        }


        public virtual UniTask GamePrepareAsync() => SwitchGamePhaseAsync(GamePhaseType.Prepare);
        public virtual UniTask GameStartAsync() => SwitchGamePhaseAsync(GamePhaseType.Start);
        public virtual UniTask GameRunAsync() => SwitchGamePhaseAsync(GamePhaseType.Running);
        public virtual UniTask GamePauseAsync() => SwitchGamePhaseAsync(GamePhaseType.Pause);
        public virtual UniTask GameResumeAsync() => SwitchGamePhaseAsync(GamePhaseType.Resume);
        public virtual UniTask GameOverAsync() => SwitchGamePhaseAsync(GamePhaseType.Over);
        public virtual UniTask GameExitAsync() => SwitchGamePhaseAsync(GamePhaseType.Exit);


        protected async UniTask SwitchGamePhaseAsync(GamePhaseType phase) {
            if (Phase == phase) return;
            this.phase = phase;

            phaseCts?.Cancel();
            phaseCts?.Dispose();
            phaseCts = new CancellationTokenSource();
            var ct = phaseCts.Token;

            switch (phase) {
            case GamePhaseType.Prepare:
                foreach (var m in modules) await m.OnEnterPrepare(context, ct);
                break;
            case GamePhaseType.Start:
                foreach (var m in modules) await m.OnEnterStart(context, ct);
                break;
            case GamePhaseType.Running:
                foreach (var m in modules) await m.OnEnterRun(context, ct);
                break;
            case GamePhaseType.Pause:
                foreach (var m in modules) await m.OnEnterPause(context, ct);
                break;
            case GamePhaseType.Resume:
                foreach (var m in modules) await m.OnEnterPause(context, ct);
                break;
            case GamePhaseType.Over:
                foreach (var m in modules) await m.OnEnterOver(context, ct);
                break;
            case GamePhaseType.Exit:
                foreach (var m in modules) await m.OnEnterExit(context, ct);
                break;
            }
        }
    }
}
