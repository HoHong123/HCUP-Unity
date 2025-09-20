using UnityEngine;

namespace Util.Pooling {
    public interface IPoolDispose<TMono> where TMono : MonoBehaviour {
        public void OnDispose(TMono mono);
    }
}