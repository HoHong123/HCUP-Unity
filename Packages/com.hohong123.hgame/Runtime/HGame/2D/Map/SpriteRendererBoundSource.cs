using UnityEngine;
using Sirenix.OdinInspector;

namespace HUtil._2D.Map {
    [DisallowMultipleComponent]
    public class SpriteRendererBoundsSource : MonoBehaviour, IWorldBoundSource {
        [Title("Boundary")]
        [SerializeField]
        SpriteRenderer spriteRender;

        public bool TryGetWorldRect(out Rect rect) {
            rect = default;
            if (!spriteRender) return false;

            var b = spriteRender.bounds;
            rect = new Rect(b.min, b.size);

            return true;
        }
    }
}