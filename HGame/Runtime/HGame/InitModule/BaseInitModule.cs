#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * InitManager 페이즈 전환 훅을 제공하는 초기화 모듈 베이스 클래스입니다.
 *
 * 특징 / 지원기능 ::
 * + 페이즈별 OnEnter 훅 7종을 virtual 로 제공. 필요한 훅만 override.
 * + order 값으로 InitManager 내 실행 순서를 결정 (오름차순).
 *
 * 주의사항 ::
 * + OnEnterResume / OnEnterExit 은 IInitModule 계약에 없는 베이스 전용 확장 훅입니다.
 * =========================================================
 */
#endif

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HGame.Flow {
    [Serializable]
    public abstract class BaseInitModule : MonoBehaviour, IInitModule {
        [SerializeField]
        int order = 0;

        public int Order => order;

        public virtual UniTask OnEnterPrepare(InitContext context, CancellationToken ct) => UniTask.CompletedTask;
        public virtual UniTask OnEnterStart(InitContext context, CancellationToken ct) => UniTask.CompletedTask;
        public virtual UniTask OnEnterRun(InitContext context, CancellationToken ct) => UniTask.CompletedTask;
        public virtual UniTask OnEnterPause(InitContext context, CancellationToken ct) => UniTask.CompletedTask;
        public virtual UniTask OnEnterResume(InitContext context, CancellationToken ct) => UniTask.CompletedTask;
        public virtual UniTask OnEnterOver(InitContext context, CancellationToken ct) => UniTask.CompletedTask;
        public virtual UniTask OnEnterExit(InitContext context, CancellationToken ct) => UniTask.CompletedTask;
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.08.04 BaseGameModule 을 BaseInitModule 로 개칭
 *
 * # 변경
 * - 클래스명 BaseGameModule -> BaseInitModule, 파일명 동기화 (guid 보존).
 * - 구현 인터페이스 IGamePhaseModule -> IInitModule 개칭 반영.
 * - (후속) 페이즈 훅 시그니처의 GameContext -> InitContext 개칭 반영.
 *
 * # 이유
 * - "게임 모듈" 이라는 이름이 실제 역할(페이즈 훅 기반 초기화 단위)과 불일치 (Phase 1 HCUP 정리).
 *
 * =============================================================================
 */
#endif
