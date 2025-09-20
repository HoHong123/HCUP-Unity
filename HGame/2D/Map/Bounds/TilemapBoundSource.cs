#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 2D환경 'MapManager' 미니맵 기능에 미니맵 범위를 설정하기 위한 오브젝트 스크립트입니다.
 * Tilemap의 크기로 범위를 지정합니다.
 * =========================================================
 */
#endif

using UnityEngine;
using UnityEngine.Tilemaps;
using Util.OdinCompat;

namespace HGame._2D.Map {
    [DisallowMultipleComponent]
    public class TilemapBoundSource : MonoBehaviour, IWorldBoundSource {
        [HeaderOrTitle("Boundary")]
        [SerializeField]
        Tilemap tilemap;

        private void Awake() {
            if (!tilemap) tilemap = GetComponentInChildren<Tilemap>();
        }

        public bool TryGetWorldRect(out Rect rect) {
            rect = default;
            if (!tilemap) return false;

            var cell = tilemap.cellBounds; // Grid space
            var min = tilemap.CellToWorld(cell.min);
            var max = tilemap.CellToWorld(cell.max);
            var size = (Vector2)(max - min);
            rect = new Rect(min, size);

            return size.x > 0 && size.y > 0;
        }
    }
}
