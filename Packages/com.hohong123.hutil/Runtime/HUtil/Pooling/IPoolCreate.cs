using UnityEngine;

namespace HUtil.Pooling {
    public interface IPoolCreate<TMono> where TMono : MonoBehaviour {
        public void OnCreate(TMono mono);
    }
}