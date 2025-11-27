using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HGame.Flow {
    [Serializable]
    public abstract class BaseGameModule : MonoBehaviour, IGamePhaseModule {
        [SerializeField]
        int order = 0;

        public int Order => order;

        public virtual UniTask OnEnterPrepare(GameContext context, CancellationToken ct) => UniTask.CompletedTask;
        public virtual UniTask OnEnterStart(GameContext context, CancellationToken ct) => UniTask.CompletedTask;
        public virtual UniTask OnEnterRun(GameContext context, CancellationToken ct) => UniTask.CompletedTask;
        public virtual UniTask OnEnterOver(GameContext context, CancellationToken ct) => UniTask.CompletedTask;
        public virtual UniTask OnEnterPause(GameContext context, CancellationToken ct) => UniTask.CompletedTask;
    }
}