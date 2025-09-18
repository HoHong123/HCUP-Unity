using UnityEngine;

public interface IPoolDispose<TMono> where TMono : MonoBehaviour {
    public void OnDispose(TMono mono);
}
