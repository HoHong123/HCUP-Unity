#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * InitModule 스택의 페이즈 상태머신 싱글톤 베이스 클래스입니다.
 *
 * 특징 / 지원기능 ::
 * + modules 리스트를 Order 오름차순 정렬 후 페이즈 전환 시 순차 await.
 * + Game{Phase}Async 공개 API 7종으로 페이즈 전환 트리거.
 *
 * 사용법 ::
 * + 씬 매니저가 InitManager<TSelf> 를 상속하고 modules 에 BaseInitModule 들을 등록합니다.
 * =========================================================
 */
#endif

using System.Threading;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using HInspector;

namespace HGame.Flow {
    public class InitManager<TSelf>:
        HCore.SingletonBehaviour<TSelf>
        where TSelf : InitManager<TSelf> {
        [HTitle("Flow")]
        [SerializeField]
        protected bool autoPrepareOnEnable = true;
        [SerializeField]
        protected InitPhaseType phase = InitPhaseType.None;
        [SerializeField]
        [Tooltip("Modules (children or same GameObject)")]
        List<BaseInitModule> modules = new();

        InitContext context = new InitContext();
        CancellationTokenSource phaseCts;

        public InitPhaseType Phase => phase;


        protected override void Awake() {
            base.Awake();
            // base 가 중복 인스턴스를 Destroy 한 경우 초기화를 진행하지 않는다.
            if (instance != this) return;
            modules.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        protected virtual void OnEnable() { }

        protected virtual void Start() {
            // UniTask 를 버리면 초기화 실패가 로그 없이 사라진다 — Forget 이 예외를 로깅한다.
            if (autoPrepareOnEnable) GamePrepareAsync().Forget();
        }

        protected virtual void OnDisable() {
            phaseCts?.Cancel();
            phaseCts?.Dispose();
            phaseCts = null;
            phase = InitPhaseType.None;
        }


        public virtual UniTask GamePrepareAsync() => SwitchGamePhaseAsync(InitPhaseType.Prepare);
        public virtual UniTask GameStartAsync() => SwitchGamePhaseAsync(InitPhaseType.Start);
        public virtual UniTask GameRunAsync() => SwitchGamePhaseAsync(InitPhaseType.Running);
        public virtual UniTask GamePauseAsync() => SwitchGamePhaseAsync(InitPhaseType.Pause);
        public virtual UniTask GameResumeAsync() => SwitchGamePhaseAsync(InitPhaseType.Resume);
        public virtual UniTask GameOverAsync() => SwitchGamePhaseAsync(InitPhaseType.Over);
        public virtual UniTask GameExitAsync() => SwitchGamePhaseAsync(InitPhaseType.Exit);


        protected async UniTask SwitchGamePhaseAsync(InitPhaseType phase) {
            if (Phase == phase) return;
            this.phase = phase;

            phaseCts?.Cancel();
            phaseCts?.Dispose();
            phaseCts = new CancellationTokenSource();
            var ct = phaseCts.Token;

            System.Func<BaseInitModule, UniTask> enterPhase = phase switch {
                InitPhaseType.Prepare => m => m.OnEnterPrepare(context, ct),
                InitPhaseType.Start => m => m.OnEnterStart(context, ct),
                InitPhaseType.Running => m => m.OnEnterRun(context, ct),
                InitPhaseType.Pause => m => m.OnEnterPause(context, ct),
                InitPhaseType.Resume => m => m.OnEnterResume(context, ct),
                InitPhaseType.Over => m => m.OnEnterOver(context, ct),
                InitPhaseType.Exit => m => m.OnEnterExit(context, ct),
                _ => null,
            };
            if (enterPhase == null) return;

            try {
                foreach (var m in modules) {
                    // 이전 페이즈 루프가 새 전환의 Cancel 을 무시하고 계속 주행하면
                    // 두 상태머신이 동시에 돈다 — 모듈 사이마다 취소를 검사한다.
                    ct.ThrowIfCancellationRequested();
                    await enterPhase(m);
                }
            }
            catch (System.OperationCanceledException) {
                // 새 페이즈 전환에 의해 대체된 루프의 정상 종료 경로 — 상위로 전파하지 않는다.
            }
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.08.04 GameManager 를 InitManager 로 개칭
 *
 * # 변경
 * - 클래스명 GameManager<TSelf> -> InitManager<TSelf>, 파일명 동기화 (guid 보존).
 * - 참조 타입 GameContext -> InitContext, GamePhaseType -> InitPhaseType 개칭 반영.
 * - 페이즈 전환 API(GamePrepareAsync 등) 메서드명은 유지 - 게임 수명주기 의미가 정확함.
 *
 * # 이유
 * - InitModule 폴더 내 스크립트의 Game 접두를 Init 으로 통일 (Phase 1 HCUP 정리 후속).
 *
 * =============================================================================
 */
#endif
