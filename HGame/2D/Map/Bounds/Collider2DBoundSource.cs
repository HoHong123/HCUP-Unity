#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 2D환경 'MapManager' 미니맵 기능에 미니맵 범위를 설정하기 위한 오브젝트 스크립트입니다.
 * Collider2D 오브젝트의 바운더리를 확인하여 범위를 지정합니다.
 * =========================================================
 */
#endif

using UnityEngine;
using Util.OdinCompat;

namespace HGame._2D.Map {
    [DisallowMultipleComponent]
    public class Collider2DBoundSource : MonoBehaviour, IWorldBoundSource {
        [HeaderOrTitle("Boundary")]
        [SerializeField]
        Collider2D coll2D;

        private void Awake() {
            if (!coll2D) coll2D = GetComponentInChildren<Collider2D>();
        }

        public bool TryGetWorldRect(out Rect rect) {
            rect = default;
            if (!coll2D) return false;

            var bound = coll2D.bounds;
            rect = new Rect(bound.min, bound.size);

            return true;
        }
    }
}