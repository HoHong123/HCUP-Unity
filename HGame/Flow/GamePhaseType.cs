#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 게임 모듈들이 실행될 타이밍을 나타내는 열거형 파일입니다.
 * 1. None : 모듈 실행 제외
 * 2. Prepare : 초기화 단계 실행 모듈
 * 3. Start : 게임 시작 단계 실행 모듈
 * 4. Running : 게임 진행 단계 실행 모듈
 * 5. Pause : 게임 일시정지 단계 실행 모듈
 * 6. Over : 게임 종료 단계 실행 모듈
 * 7. Post : 게임 종료 후 후처리 단계 실행 모듈
 * =========================================================
 */
#endif

namespace HGame.Game.Flow {
    public enum GamePhaseType {
        None,
        Prepare,
        Start,
        Running,
        Pause,
        Over,
        Post,
    }
}