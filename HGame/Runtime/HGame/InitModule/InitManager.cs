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

        InitContext context = new InitContext();
        CancellationTokenSource phaseCts;
        bool hasStarted;

        public InitPhaseType Phase => phase;


        protected override void Awake() {
            base.Awake();
            // base 가 중복 인스턴스를 Destroy 한 경우 초기화를 진행하지 않는다.
            if (instance != this) return;
            // 인스펙터 슬롯이 비었거나 참조가 Missing 이면 Sort 비교자에서 NRE 가 나고
            // Awake 가 중단되어 "싱글톤은 살아있는데 정렬은 안 된" 반쯤 초기화된 매니저가 남았다.
            int removed = modules.RemoveAll(m => m == null);
            if (removed > 0) {
                HLogger.Error($"[InitManager] {removed} null module slot(s) removed from '{name}'. Fix the inspector list.");
            }
            modules.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        // 필드명은 "OnEnable 마다" 를 약속했지만 소비 지점은 Start 였고, OnDisable 은 phase 를
        // None 으로 되돌렸다 — 비활성→재활성 후 매니저가 None 에 갇혔다. 이름대로 재개시킨다.
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

            // 종전에는 상태를 먼저 확정한 뒤 유효성을 검사해, 미정의 페이즈를 넘기면
            // 이전 페이즈만 파괴되고 아무 모듈도 진입하지 않은 채 매니저가 추락했다.
            if (!_IsSupportedPhase(phase)) {
                HLogger.Error($"[InitManager] SwitchGamePhaseAsync received an unsupported phase '{phase}'. State unchanged.");
                return;
            }

            var previousPhase = this.phase;
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
