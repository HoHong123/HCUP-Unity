#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 2D환경 'MapManager' 미니맵 기능에 미니맵 범위를 설정하기 위한 인터페이스입니다.
 * 바운더리 설정이 가능한 모든 오브젝트를 지원하기 위해 사용됩니다.
 * =========================================================
 */
#endif

using UnityEngine;

namespace HGame._2D.Map {
    public interface IWorldBoundSource {
        bool TryGetWorldRect(out Rect rect);
    }
}