namespace HGame.Sound.Core {
    [System.Serializable]
    public struct SoundKey : System.IEquatable<SoundKey> {
        [UnityEngine.SerializeField]
        SoundMajorCategory major;
        [UnityEngine.SerializeField]
        int id;


        public SoundMajorCategory Major => major;
        public int Id => id;


        public SoundKey(SoundMajorCategory type, int id) {
            this.major = type;
            this.id = id;
        }


        public bool Equals(SoundKey other) => major == other.major && id == other.id;
        public override bool Equals(object obj) => obj is SoundKey other && Equals(other);
        public override string ToString() => $"Sound Major :: {major} / ID :: {id}";
        public override int GetHashCode() {
            unchecked {
                var hash = (int)major;
                hash = (hash * 397) ^ id;
                return hash;
            }
        }
    }
}
