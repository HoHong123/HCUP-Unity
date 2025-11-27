using UnityEngine;

namespace HUtil.Pooling {
    public interface IPoolReturn<TMono> where TMono : MonoBehaviour {
        public void OnReturn(TMono mono);
    }
}