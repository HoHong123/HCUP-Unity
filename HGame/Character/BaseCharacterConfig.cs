#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 게임 캐릭터에 사용될 가장 기본이되는 캐릭터 정보를 담는 SO 파일입니다.
 * =========================================================
 */
#endif

using UnityEngine;
using Sirenix.OdinInspector;

namespace HGame.Game.Character {
    public class BaseCharacterConfig : ScriptableObject {
        [Title("Meta")]
        [SerializeField]
        protected int uid;
        [SerializeField]
        protected string charName;

        public int UID => uid;
        public string Name => charName;
    }
}