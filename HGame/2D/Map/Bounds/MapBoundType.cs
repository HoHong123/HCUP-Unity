#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 2D 환경 '맵 반경 설정' 타입 열거형 파일입니다.
 * 1. WorldBox : 단일 BoxCollider2D로 맵 반경을 설정하는 인덱스
 * 2. BoundSource : 'IWorldBoundSource'를 사용하여 맵 반경을 설정하는 인덱스
 * 3. Absolute : 절대좌표로 맵 반경을 설정하는 인덱스
 * =========================================================
 */
#endif
namespace HGame._2D.Map {
    public enum MapBoundType {
        WorldBox,
        BoundSource,
        Absolute,
    }
}
