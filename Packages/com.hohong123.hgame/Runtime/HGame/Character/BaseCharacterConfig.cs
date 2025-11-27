using UnityEngine;
using Sirenix.OdinInspector;

namespace HGame.Character {
    public class BaseCharacterConfig : ScriptableObject, ICharacterReadOnly {
        [Title("Meta")]
        [SerializeField]
        protected int uid;
        [SerializeField]
        protected string charName;
        [SerializeField]
        protected Sprite icon;
        //[SerializeField]
        //protected string iconPath;

        public int UID => uid;
        public string Name => charName;
        public Sprite Icon => icon;
        //public string SpritePath => iconPath;
    }
}