#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 모든 게임 모듈간 통신에 사용될 전역 값들을 정의하는 데이터모델입니다.
 * 필요에 따라 원하는 값을 추가할 수 있습니다.
 * =========================================================
 */
# endif

namespace HGame.Game.Flow {
    public sealed class GameContext {
        public float TimeScale = 1f;
    }
}
