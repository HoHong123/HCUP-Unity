using UnityEngine;
using Sirenix.OdinInspector;
using HUtil.Collection;

namespace HGame._2D.Layer {
    public class ParallexLayer : MonoBehaviour {
        [Title("Camera")]
        [SerializeField]
        Camera cam;

        Transform camTran;

        [Title("Motion")]
        [SerializeField]
        bool useParallaxFollow = true;
        [SerializeField][Range(0f, 1f)]
        [ShowIf("useParallaxFollow")]
        float parallaxFactor = 0.2f;

        Vector3 preCamPos;

        [Title("Tile")]
        [SerializeField]
        bool randomGap = false;
        [SerializeField][HideIf("randomGap")]
        float tileGap = 0f;
        [SerializeField][ShowIf("randomGap")]
        [Tooltip("X = min, Y = max")]
        Vector2 randomRange = new Vector2(0, 50);
        [SerializeField]
        SpriteRenderer[] tiles;
        [SerializeField][ReadOnly]
        Vector2 tileWorldSize;

        int leftCount = 0;
        int rightCount = 0;
        float tileWidth = 0f;
        float[] multiTileWidth;
        CircularList<Transform> tileTrans;

        private float _GetTileWith => (randomGap ? multiTileWidth[tileTrans.Pivot] : tileWidth);


        #region Unity Life Cycle
        private void Start() {
            camTran = cam.transform;
            if (useParallaxFollow) {
                preCamPos = camTran.position;
            }

            tileWorldSize = tiles[0].bounds.size;
            tileWidth = tileWorldSize.x;

            if (randomGap) {
                int size = tiles.Length;
                float min = Mathf.Min(randomRange.x, randomRange.y);
                float max = Mathf.Max(randomRange.x, randomRange.y);
                multiTileWidth = new float[size];
                for (int k = 0; k < size; k++)
                    multiTileWidth[k] = tileWidth + Random.Range(min, max);
            }
            else {
                tileWidth += tileGap;
            }

            bool isOdd = tiles.Length % 2 != 0;
            tileTrans = new(pivot: tiles.Length / 2 - (isOdd ? 0 : 1), size: tiles.Length);
            foreach (var tile in tiles) {
                tileTrans.Add(tile.transform);
            }
#if !UNITY_EDITOR
            tiles = null;
#endif

            leftCount = (tileTrans.Count - 1) / 2;
            rightCount = tileTrans.Count - 1 - leftCount;

            _RepositionLine(tileTrans.CurrentItem);
        }

        private void LateUpdate() {
            if (useParallaxFollow) {
                Vector3 camDelta = camTran.position - preCamPos;
                if (camDelta.sqrMagnitude > 0f) {
                    Vector3 move = new Vector3(camDelta.x * parallaxFactor, 0f, 0f);
                    for (int k = 0; k < tileTrans.Count; k++)
                        tileTrans.Items[k].position += move;
                }
                preCamPos = camTran.position;
            }

            var center = tileTrans.CurrentItem;
            float half = _GetTileWith * 0.5f;
            if (camTran.position.x - center.position.x > half) {
                _ShiftRight();
            }
            else if (center.position.x - camTran.position.x > half) {
                _ShiftLeft();
            }
        }
#endregion

        #region Init
        private void _RepositionLine(Transform center) {
            // Allocate left side (Closest first)
            for (int k = 1; k <= leftCount; k++) {
                var leftTile = tileTrans.Items[(tileTrans.Pivot - k + tileTrans.Count) % tileTrans.Count];
                leftTile.position = center.position + Vector3.left * (_GetTileWith * k);
            }
            // Allocate right side
            for (int k = 1; k <= rightCount; k++) {
                var rightTile = tileTrans.Items[(tileTrans.Pivot + k) % tileTrans.Count];
                rightTile.position = center.position + Vector3.right * (_GetTileWith * k);
            }
        }
        #endregion

        #region Shift
        private void _ShiftRight() {
            var left = tileTrans.PeekOffset(-leftCount);
            var right = tileTrans.PeekOffset(rightCount);
            left.position = right.position + Vector3.right * _GetTileWith;
            tileTrans.MoveNext();
        }

        private void _ShiftLeft() {
            var left = tileTrans.PeekOffset(-leftCount);
            var right = tileTrans.PeekOffset(rightCount);
            right.position = left.position + Vector3.left * _GetTileWith;
            tileTrans.MovePrev();
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* @Jason - PKH 05.09.25
 * 1. 유틸 패키지의 순환 리스트를 사용하여 패럴랙스 기능을 구현합니다.
 * 2. 제공된 패럴랙스 이미지 리소스의 개수에 따라 짝수 혹은 홀수 배경타일이 존재할 수 있습니다.
 * + 짝수의 경우, (중간 - 1)의 이미지가 센터(시작) 이미지로 인식됩니다. 
 * + 센터의 위치는 해당 패럴랙스가 시작하는 카메라의 중심으로 설정하시고 좌우에 들어갈 이미지들을 배치하시면 됩니다.
 * ++ Ex) 2개의 이미지 = 1번째가 센터
 * ++ Ex) 4개의 이미지 = 2번째가 센터
 * ++ Ex) 5개의 이미지 = 3번째가 센터
 * 
 * @Jason - PKH 06.09.25
 * TODO :: Shift 리전의 기능들을 따로 모듈화할 예정.
 */
#endif