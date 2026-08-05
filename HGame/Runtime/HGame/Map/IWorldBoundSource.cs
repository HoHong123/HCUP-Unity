using UnityEngine;

namespace HGame.Map {
    public interface IWorldBoundSource {
        bool TryGetWorldRect(out Rect rect);
    }
}
