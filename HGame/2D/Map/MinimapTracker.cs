#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 미니맵에 표현되는 오브젝트 트래커 스크립트입니다.
 * 
 * ** 사용법 **
 * 1. 미니맵에 표현되어야하는 게임 캐릭터에게 컴포넌트로 추가합니다.
 * 2. 캐릭터 콜라이더를 입력합니다.
 * 3. 'useIcon'으로 캐릭터 아이콘 혹은 기본 아이콘 이미지를 사용합니다.
 * 4. 'scaleByCollider'로 현재 캐릭터의 콜라이더 크기에 비례하여 미니맵에 표현될 이미지 크기를 조절합니다.
 * =========================================================
 */
#endif

using UnityEngine;
using Sirenix.OdinInspector;
using HGame.Game.Character;

namespace HGame._2D.Map {
    [DisallowMultipleComponent]
    public class MinimapTrackable : MonoBehaviour {
        [Title("Config")]
        [SerializeField, ReadOnly]
        CharacterConfig config;

        [Title("References")]
        [SerializeField]
        Collider2D charCollider;

        [Title("Icon")]
        [SerializeField]
        bool useIcon = false;
        [SerializeField]
        bool scaleByCollider = false;
        [SerializeField]
        float iconSizeMin = 6f;
        [SerializeField]
        float iconSizeMax = 18f;

        [Title("Visibility")]
        [SerializeField]
        bool showWhenOutOfBounds = false;

        public bool UseIcon => useIcon;
        public bool ScaleByCollider => scaleByCollider;
        public bool ShowWhenOutOfBounds => showWhenOutOfBounds;
        public float IconSizeMin => iconSizeMin;
        public float IconSizeMax => iconSizeMax;
        public Sprite Icon => config.Icon;
        public Transform Target => transform;
        public Collider2D Collider => charCollider;

        public void Init(CharacterConfig config) {
            this.config = config;
        }
    }
}
