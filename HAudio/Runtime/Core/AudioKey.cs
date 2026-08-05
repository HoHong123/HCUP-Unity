namespace HAudio.Core {
    [System.Serializable]
    public struct AudioKey : System.IEquatable<AudioKey> {
        [UnityEngine.SerializeField]
        AudioMajorCategory major;
        [UnityEngine.SerializeField]
        int id;


        public AudioMajorCategory Major => major;
        public int Id => id;


        public AudioKey(AudioMajorCategory type, int id) {
            this.major = type;
            this.id = id;
        }


        public bool Equals(AudioKey other) => major == other.major && id == other.id;
        public override bool Equals(object obj) => obj is AudioKey other && Equals(other);
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
