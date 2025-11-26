using UnityEngine;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;

namespace HUtil._2D.Map {
    [DisallowMultipleComponent]
    public class TilemapBoundSource : MonoBehaviour, IWorldBoundSource {
        [Title("Boundary")]
        [SerializeField]
        Tilemap tilemap;

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
