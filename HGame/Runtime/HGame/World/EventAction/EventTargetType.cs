using System;

namespace HGame.World.EventAction {
    [Flags]
    public enum EventTargetType : byte {
        Tag = 1 << 0,
        Layer = 1 << 1,
        TagAndLayer = Tag | Layer,
    };
}