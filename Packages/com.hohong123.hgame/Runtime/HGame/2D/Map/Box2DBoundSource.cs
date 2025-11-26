using UnityEngine;
using Sirenix.OdinInspector;

namespace HUtil._2D.Map {
    [DisallowMultipleComponent]
    public class Box2DBoundSource : MonoBehaviour, IWorldBoundSource {
        [Title("Boundary")]
        [SerializeField]
        BoxCollider2D box;

        public bool TryGetWorldRect(out Rect rect) {
            rect = default;
            if (!box) return false;

            var b = box.bounds;
            rect = new Rect(b.min, b.size);

            return true;
        }
    }
}