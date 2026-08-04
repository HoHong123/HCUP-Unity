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

            switch (phase) {
            case InitPhaseType.Prepare:
                foreach (var m in modules) await m.OnEnterPrepare(context, ct);
                break;
            case InitPhaseType.Start:
                foreach (var m in modules) await m.OnEnterStart(context, ct);
                break;
            case InitPhaseType.Running:
                foreach (var m in modules) await m.OnEnterRun(context, ct);
                break;
            case InitPhaseType.Pause:
                foreach (var m in modules) await m.OnEnterPause(context, ct);
                break;
            case InitPhaseType.Resume:
                foreach (var m in modules) await m.OnEnterResume(context, ct);
                break;
            case InitPhaseType.Over:
                foreach (var m in modules) await m.OnEnterOver(context, ct);
                break;
            case InitPhaseType.Exit:
                foreach (var m in modules) await m.OnEnterExit(context, ct);
                break;
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
