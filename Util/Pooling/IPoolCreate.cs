using UnityEngine;

namespace Util.Pooling {
    public interface IPoolCreate<TMono> where TMono : MonoBehaviour {
        public void OnCreate(TMono mono);
    }
}