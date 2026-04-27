namespace HGame.World.EventAction {
    public enum EventTargetType : byte{ 
        Tag = 1 << 0,
        Layer = 1 << 1,
        TagAndLayer = 1 << 2,
    };
}