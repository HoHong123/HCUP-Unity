#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 초기화 모듈의 페이즈 훅 계약 인터페이스입니다.
 *
 * 주의사항 ::
 * + Resume / Exit 훅은 계약에 없습니다 - BaseInitModule 의 베이스 전용 확장 훅으로만 존재합니다.
 * =========================================================
 */
#endif

using Cysharp.Threading.Tasks;
using System.Threading;
using HGame.Flow;

public interface IInitModule {
    int Order { get; }
    UniTask OnEnterPrepare(GameContext ctx, CancellationToken ct);
    UniTask OnEnterStart(GameContext ctx, CancellationToken ct);
    UniTask OnEnterRun(GameContext ctx, CancellationToken ct);
    UniTask OnEnterPause(GameContext ctx, CancellationToken ct);
    UniTask OnEnterOver(GameContext ctx, CancellationToken ct);
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.08.04 IGamePhaseModule 을 IInitModule 로 개칭
 *
 * # 변경
 * - 타입명 IGamePhaseModule -> IInitModule, 파일명 IGamePhase.cs -> IInitModule.cs (guid 보존).
 *
 * # 이유
 * - 파일명(IGamePhase)과 타입명(IGamePhaseModule) 불일치 해소 + InitModule 계열 명명 통일 (Phase 1 HCUP 정리).
 *
 * =============================================================================
 */
#endif
