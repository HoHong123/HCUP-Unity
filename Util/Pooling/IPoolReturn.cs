using UnityEngine;

public interface IPoolReturn<TMono> where TMono : MonoBehaviour {
    public void OnReturn(TMono mono);
}
