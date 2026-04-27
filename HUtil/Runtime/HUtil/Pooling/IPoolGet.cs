using UnityEngine;

namespace HUtil.Pooling {
    public interface IPoolGet<TMono> where TMono : MonoBehaviour {
        public void OnGet(TMono mono);
    }
}
