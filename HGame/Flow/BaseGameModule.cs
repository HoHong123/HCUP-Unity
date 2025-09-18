#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 게임 플로우 설정 모듈 스크립트입니다.
 * 
 * ** 사용법 **
 * 1. 해당 스크립트를 상속 받는 스크립트를 생성합니다.
 * 2. 자식 스크립트 컴포넌트를 빈 게임 오브젝트에 입력합니다.
 * 3. 자식 스크립트에서 원하는 순간에 동작할 작업을 선언합니다.
 * 4. order를 통해 각 작업의 우선순위를 결정합니다.
 * 5. 'Util.Game.Flow.GameManager'를 상속받는 각 프로젝트의 게임매니저 스크립트의 'modules'에 자식 스크립트들을 등록합니다.
 * Ps. 데모 스크립트를 확인할 수 있습니다.
 * =========================================================
 */
#endif

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HGame.Game.Flow {
    [Serializable]
    public abstract class BaseGameModule : MonoBehaviour, IGamePhaseModule {
        [SerializeField]
        int order = 0;

        public int Order => order;

        public virtual UniTask OnEnterPrepare(GameContext context, CancellationToken ct) => UniTask.CompletedTask;
        public virtual UniTask OnEnterStart(GameContext context, CancellationToken ct) => UniTask.CompletedTask;
        public virtual UniTask OnEnterRun(GameContext context, CancellationToken ct) => UniTask.CompletedTask;
        public virtual UniTask OnEnterPause(GameContext context, CancellationToken ct) => UniTask.CompletedTask;
        public virtual UniTask OnEnterOver(GameContext context, CancellationToken ct) => UniTask.CompletedTask;
        public virtual UniTask OnEnterPost(GameContext context, CancellationToken ct) => UniTask.CompletedTask;
    }
}