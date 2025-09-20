#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 2D 환경에서 패럴랙스 레이아웃 시스템을 구현한 스크립트입니다.
 * 
 * ** 사용법 **
 * 1. 'userParallaxFollow'로 카메라 위치에 따라 레이아웃도 조금씩 동일한 방향으로 움직이거나 고정할지 선택할 수 있습니다.
 * + 'parallaxFactor'를 통해 움직이는 속도를 조절할 수 있습니다. (0 = 고정, 1 = 카메라와 동일한 속도)
 * 2. 'randomGap'으로 초기화 과정에서 각 타일에 랜덤 여유 공간을 설정할 수 있습니다.
 * 3. 'tiles'에 반드시 2개 이상의 패럴랙스에 사용될 렌더러를 할당합니다.
 * =========================================================
 */
#endif

using Sirenix.OdinInspector;
using UnityEngine;
using Util.OdinCompat;

namespace HGame._2D.View {
    public partial class ParallexLayer : MonoBehaviour {
#if ODIN_INSPECTOR
        [HeaderOrTitle("Camera")]
        [SerializeField]
        Camera cam;

        Transform camTran;

        [HeaderOrTitle("Motion")]
        [SerializeField]
        bool useParallaxFollow = true;
        [SerializeField, ShowIf("useParallaxFollow")]
        [Range(0f, 1f)]
        float parallaxFactor = 0.2f;

        Vector3 preCamPos;

        [HeaderOrTitle("Tile")]
        [SerializeField]
        bool randomGap = false;
        [SerializeField, HideIf("randomGap")]
        float tileGap = 0f;
        [SerializeField, ShowIf("randomGap")]
        [Tooltip("X = min, Y = max")]
        Vector2 randomRange = new Vector2(0, 50);
        [SerializeField]
        SpriteRenderer[] tiles;
        [SerializeField, ReadOnly]
        Vector2 tileWorldSize;
#endif
    }
}