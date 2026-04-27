#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 씬 전환에 사용되는 열거형 스크립트입니다.
 * 씬전환 코드 사용될 열거형으로 유지보수와 관리 편의성을 위해 작성되었습니다.
 * 사용되는 프로젝트에 맞추어 개선하면 됩니다.
 * =========================================================
 */
#endif

namespace HCore.Scene {
    public enum SceneKey : int {
        Bootstrap,
        Lobby,
        Game,

        Loading,


        Etc1 = 4000,
        Etc2,

        OutGameTutorial = 9000,
        InGameTutorial,

        Test = 10000,
    }
}