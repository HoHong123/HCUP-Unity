using UnityEngine;
using Sirenix.OdinInspector;

namespace HUtil._2D.Map {
    [DisallowMultipleComponent]
    public class CompositeBoundSource : MonoBehaviour, IWorldBoundSource {
        [Title("Boundary")]
        [SerializeField]
        CompositeCollider2D composite;

        public bool TryGetWorldRect(out Rect rect) {
            rect = default;
            if (!composite) return false;

            var b = composite.bounds;
            rect = new Rect(b.min, b.size);

            return b.size.x > 0 && b.size.y > 0;
        }
    }
}