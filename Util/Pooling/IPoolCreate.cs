using UnityEngine;

public interface IPoolCreate<TMono> where TMono : MonoBehaviour {
    public void OnCreate(TMono mono);
}
