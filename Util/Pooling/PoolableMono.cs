using System;
using Sirenix.OdinInspector;

namespace Util.Pooling {
    [Serializable]
    public abstract class PoolableMono : SerializedMonoBehaviour {
        public abstract void OnCreate(PoolableMono mono);
        public abstract void OnGet(PoolableMono mono);
        public abstract void OnReturn(PoolableMono mono);
        public abstract void OnDispose(PoolableMono mono);
    }
}

#if UNITY_EDITOR
/* Dev Log
 * @Jason - PKH
 * ** 풀링매니저('PoolManager')에 사용되지 않는 풀링 오브젝트라면 해당 클래스를 상속 받을 필요가 없습니다. **
 * 풀링 매니저('PoolManager')는 매니저가 존재하는 씬 전역에서 골고루 사용되는 필수 풀링 객체를 생성 및 관리합니다.
 * 이때, 'UnityPoolEntity' 클래스 인스턴스가 풀링 매니저가 사용할 객체의 정보를 포함합니다.
 * 각 객체의 풀링에 필요한 스크립트 컨포넌트가 다른 기타 컴포넌트들과 구별되기 위하여 해당 클래스를 작성했습니다.
 * -------------------------------------------------------------------------------------------------------------------------------------------------------------------------
 * ** If it is a pooling object that is not used in the 'PoolManager' system, there is no need to inherit the class. **
 * The 'PoolManager' creates and manages the required pooling objects that are use globally in the scene which the manager exists.
 * The 'UnityPoolEntity' class instance contains the information of the object that the pooling manager will use.
 * Written to distinguish the scripts needed for pooling from the various MonoBehaviour components of the prefab set on each 'UnityPoolEntity' object.
 * 
 * ==================================================
 * How to use ::
 * public virtual void OnCreate(PoolableMono mono) {
 *      // Use simple 'MonoBehaviour' feature
 *      mono.gameObject.SetActive(false);
 *      
 *      // Use derived class features(value, functions etc)
 *      var child = mono as 자식클래스;
 *      child.InnerValue = SomethingSomething;
 *      child.Run();
 *      child.OnSleep?.Invoke();
 * }
 */
#endif