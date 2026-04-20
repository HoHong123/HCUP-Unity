using UnityEngine;
using HInspector;

namespace HGame.H2D.Map {
    [DisallowMultipleComponent]
    public class Box2DBoundSource : MonoBehaviour, IWorldBoundSource {
        [HTitle("Boundary")]
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
