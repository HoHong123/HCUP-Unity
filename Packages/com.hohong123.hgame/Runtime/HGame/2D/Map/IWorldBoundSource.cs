using UnityEngine;

public interface IWorldBoundSource {
    bool TryGetWorldRect(out Rect rect);
}