using HInspector;
using UnityEngine;

namespace HGame.H2D.Map {
    [DisallowMultipleComponent]
    public class SpriteRendererBoundsSource : MonoBehaviour, IWorldBoundSource {
        [HTitle("Boundary")]
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
