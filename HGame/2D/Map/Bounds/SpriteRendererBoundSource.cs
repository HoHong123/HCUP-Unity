#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 2D환경 'MapManager' 미니맵 기능에 미니맵 범위를 설정하기 위한 오브젝트 스크립트입니다.
 * SpriteRenderer의 크기로 범위를 지정합니다.
 * =========================================================
 */
#endif

using UnityEngine;
using Util.OdinCompat;

namespace HGame._2D.Map {
    [DisallowMultipleComponent]
    public class SpriteRendererBoundsSource : MonoBehaviour, IWorldBoundSource {
        [HeaderOrTitle("Boundary")]
        [SerializeField]
        SpriteRenderer spriteRender;

        private void Awake() {
            if (!spriteRender) spriteRender = GetComponentInChildren<SpriteRenderer>();
        }

        public bool TryGetWorldRect(out Rect rect) {
            rect = default;
            if (!spriteRender) return false;

            var b = spriteRender.bounds;
            rect = new Rect(b.min, b.size);

            return true;
        }
    }
}