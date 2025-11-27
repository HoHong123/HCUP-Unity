namespace HGame.Character {
    public interface ICharacterReadOnly {
        public int UID { get; }
        public string Name { get; }
        //public string SpritePath { get; }
        public UnityEngine.Sprite Icon { get; }
    }
}