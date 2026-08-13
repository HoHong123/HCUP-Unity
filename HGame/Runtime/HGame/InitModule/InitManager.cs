#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * InitModule 스택의 페이즈 상태머신 싱글톤 베이스 클래스입니다.
 *
 * 특징 / 지원기능 ::
 * + modules 리스트를 Order 오름차순 정렬 후 페이즈 전환 시 순차 await.
 * + Game{Phase}Async 공개 API 7종으로 페이즈 전환 트리거.
 * + PhaseEntered 이벤트로 전환 완료 관측, 재진입 깊이 상한(8)으로 순환 재귀 차단.
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
using HDiagnosis.Logger;
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

        // 모듈 훅 안에서 다른 Game*Async 를 호출하는 재진입 깊이가 이 값을 넘으면
        // A<->B 교대 트리거 같은 순환 재귀를 강제 차단한다.
        const int MAX_TRANSITION_DEPTH = 8;

        InitContext context = new InitContext();
        CancellationTokenSource phaseCts;
        bool hasStarted;
        int transitionDepth;

        public InitPhaseType Phase => phase;

        // 성공적으로 완료된 전환만 알린다 - 취소·예외 경로에서는 호출하지 않는다.
        public event System.Action<InitPhaseType> PhaseEntered;


        protected override void Awake() {
            base.Awake();
            if (instance != this) return;
            int removed = modules.RemoveAll(m => m == null);
            if (removed > 0) {
                HLogger.Error($"[InitManager] {removed} null module slot(s) removed from '{name}'. Fix the inspector list.");
            }
            modules.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        protected virtual void OnEnable() {
            if (!hasStarted) return;
            if (autoPrepareOnEnable) GamePrepareAsync().Forget();
        }

        protected virtual void Start() {
            hasStarted = true;
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

            // 종전에는 상태를 먼저 확정한 뒤 유효성을 검사해
            if (!_IsSupportedPhase(phase)) {
                HLogger.Error($"[InitManager] SwitchGamePhaseAsync received an unsupported phase '{phase}'. State unchanged.");
                return;
            }

            // 모듈 훅 안에서 다시 Game*Async 를 호출하면 이 메서드가 재진입된다.
            // A<->B 교대 트리거처럼 사이클이 있으면 재진입 깊이가 무한히 쌓이므로 상한에서 거부한다.
            if (transitionDepth >= MAX_TRANSITION_DEPTH) {
                HLogger.Error($"[InitManager] SwitchGamePhaseAsync to '{phase}' rejected — reentrant transition depth exceeded {MAX_TRANSITION_DEPTH}. Check for a cycle between module hooks.");
                return;
            }
            var previousPhase = this.phase;
            transitionDepth++;
            if (transitionDepth > 1) {
                // 재진입 상태 — 외곽 루프는 이 전환이 끝난 뒤 OperationCanceledException 으로
                // 조용히 종료되며 잔여 모듈에 진입하지 못한다. 그 사실을 진단 가능하게 남긴다.
                HLogger.Log($"[InitManager] Reentrant phase transition to '{phase}' triggered from within '{previousPhase}' module hook (depth={transitionDepth}). Outer loop's remaining modules will be skipped once superseded.");
            }

            try {
                this.phase = phase;

                phaseCts?.Cancel();
                var previousCts = phaseCts;
                phaseCts = new CancellationTokenSource();
                var cts = phaseCts;
                var ct = cts.Token;
                // 관측 중인 토큰의 소스를 즉시 Dispose 하면 이후 등록에서 ObjectDisposedException 이 난다.
                // Cancel 신호만 먼저 보내고 실제 해제는 한 프레임 뒤로 미룬다.
                _DisposeLater(previousCts);

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

                try {
                    foreach (var m in modules) {
                        // 이전 페이즈 루프가 새 전환의 Cancel 을 무시하고 계속 주행하면
                        // 두 상태머신이 동시에 돈다 — 모듈 사이마다 취소를 검사한다.
                        ct.ThrowIfCancellationRequested();
                        await enterPhase(m);
                    }
                    PhaseEntered?.Invoke(phase);
                }
                catch (System.OperationCanceledException) {
                    // 새 페이즈 전환에 의해 대체된 루프의 정상 종료 경로 — 상위로 전파하지 않는다.
                }
                catch (System.Exception e) {
                    // 종전에는 비취소 예외가 catch 를 통과해 탈출하면서도 phase 는 새 페이즈를
                    // 주장했다 — Phase 를 읽는 모든 코드가 전환 성공으로 오판했다.
                    HLogger.Error($"[InitManager] Phase transition to '{phase}' failed: {e}");
                    if (ReferenceEquals(phaseCts, cts)) this.phase = previousPhase;
                    throw;
                }
            }
            finally {
                transitionDepth--;
            }
        }

        private static bool _IsSupportedPhase(InitPhaseType phase) => phase switch {
            InitPhaseType.Prepare => true,
            InitPhaseType.Start => true,
            InitPhaseType.Running => true,
            InitPhaseType.Pause => true,
            InitPhaseType.Resume => true,
            InitPhaseType.Over => true,
            InitPhaseType.Exit => true,
            _ => false,
        };

        private static void _DisposeLater(CancellationTokenSource target) {
            if (target == null) return;
            _DisposeNextFrameAsync(target).Forget();
        }

        private static async UniTaskVoid _DisposeNextFrameAsync(CancellationTokenSource target) {
            await UniTask.NextFrame();
            target.Dispose();
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.08.07 재진입 가드 + 전환 완료 이벤트 추가 (케이스 리포트 RE-1/RE-2/TST-1)
 *
 * # 변경
 * - transitionDepth 필드 + MAX_TRANSITION_DEPTH(8) 상수 추가, SwitchGamePhaseAsync 전체를
 *   try/finally 로 감싸 재진입 깊이를 추적.
 * - 재진입 시 진단 로그 1줄(HLogger.Log), 상한 초과 시 거부 + 에러 로그.
 * - PhaseEntered 이벤트 추가 - foreach 루프가 성공 종료된 직후에만 발화.
 *
 * # 이유
 * - RE-1: 모듈이 자신의 훅 안에서 다른 Game*Async 를 호출하면 외곽 루프가 무음으로
 *   OperationCanceledException 종료돼 잔여 모듈이 진입 기회 없이 사라졌다 - 로그로 가시화.
 * - RE-2: A<->B 교대 트리거는 동일 페이즈 가드를 매번 통과해 재귀 깊이 상한이 없었다.
 * - TST-1: Start() 의 자동 전환이 .Forget() 이라 PlayMode 테스트가 완료를 관측할 수 없었다.
 *
 * # 결과
 * - `Docs/Code/CaseReport/02_InitManager-페이즈-전환.md` 의 RE-1/RE-2/TST-1 반영 완료.
 *
 * # 주의
 * - EXC-2(취소 시 일반 로그)·RACE-2(context 동시기록)는 이번 변경 범위 밖 - 별도 세션 이월.
 *
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
