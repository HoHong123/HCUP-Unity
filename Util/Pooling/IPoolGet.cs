using UnityEngine;

namespace Util.Pooling {
    public interface IPoolGet<TMono> where TMono : MonoBehaviour {
        public void OnGet(TMono mono);
    }
}
