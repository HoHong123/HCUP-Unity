#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 게임 캐릭터에 사용될 기본 캐릭터 정보를 담는 스크립트입니다.
 * =========================================================
 */
#endif

using UnityEngine;

namespace HGame.Game.Character {
    public class CharacterConfig : BaseCharacterConfig {
        [SerializeField]
        protected Sprite charIcon;

        public Sprite Icon => charIcon;
    }
}