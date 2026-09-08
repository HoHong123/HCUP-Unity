#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 풀링 시스템의 유연성과 일관성을 생각하여 작성한 클래스입니다.
 * 원하는 어떠한 타입이든 모두 적용 가능한 것이 첫번째 목표였습니다.
 * 그리고 런타임 전, 미리 씬 혹은 백그라운드에 설정할 수 있는 기능을 위하여
 * 'UnityPoolEntity' 객체를 통해 원하는 값을 미리 정의하고 'PoolManager'가 시작함과 동시에
 * 해당 값을 토대로 풀링 객체들을 생성하여 키값으로 가져오는 것을 두번째 목표로 하였습니다.
 * 
 * 사용법 ::
 * 1. 생성자를 통해 각 오브젝트 풀링에 사용될 값들을 초기화합니다.
 * + '생성, 호출, 반환, 제거' 단계에서 추가적으로 진행될 이벤트를 지정할 수 있습니다.
 * + '초기 생성 개수, 각 오브젝트 부모(게임오브젝트의 경우)'를 지정할 수 있습니다.
 * 2. 필요에 따라 현재 풀링 시스템의 정보를 확인할 수 있습니다.
 * + 각 콜랙션의 길이 확인
 * + 현재 Get으로 호출된 오브젝트들 확인
 * + 할당 가능한 오브젝트 크기 확인
 * 3. BasePool을 상속 받는 자식 풀링시스템에서 생성(Create)를 선언합니다.
 * 4. 오브젝트 풀링이 필요한 곳에서 자식 풀링시스템을 생성하여 메모리 할당 후 사용합니다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using HDiagnosis.Logger;

namespace HUtil.Pooling {
    public abstract class BasePool<T> : IDisposable where T : class {
        #region Fields
        protected readonly Stack<T> pool = new();
        protected readonly HashSet<T> activatedPool = new();

        protected Action<T> onCreate = null;
        protected Action<T> onGet = null;
        protected Action<T> onReturn = null;
        protected Action<T> onDispose = null;
        #endregion

        #region Properties
        public int CountTotal => pool.Count + activatedPool.Count;
        public int CountAvaliable => pool.Count;
        public int CountActivated => activatedPool.Count;
        public HashSet<T> Activates => activatedPool;
        #endregion

        #region Public - Util
        public override string ToString() {
#if UNITY_EDITOR
            if (pool.Count + activatedPool.Count == 0) {
                return $"[{typeof(T)} Pool] No pool object found.";
            }
            else {
                return $"[{typeof(T)} Pool] Active objects :: \n" +
                    $"Totall :: {CountTotal}\n" +
                    $"Waiting :: {CountAvaliable}\n" +
                    $"Activated :: {CountActivated}\n";
            }
#else
            return base.ToString();
#endif
        }
        #endregion

        #region Protected - Constructors
        protected BasePool(
            Action<T> onCreate = null, Action<T> onGet = null,
            Action<T> onReturn = null, Action<T> onDispose = null) {
            this.onCreate = onCreate;
            this.onGet = onGet;
            this.onReturn = onReturn;
            this.onDispose = onDispose;
        }
        #endregion

        #region Public - Init
        public virtual void Init(int capacity) {
            if (CountTotal >= capacity) return;
            int require = capacity - CountTotal;
            Create(require);
        }
        #endregion

        #region Public - Create
        public virtual void Create(int count) {
            for (int k = 0; k < count; k++) {
                pool.Push(Create());
            }
        }
        #endregion

        #region Public - Get
        public virtual T Get() {
            if (pool.Count == 0) {
                pool.Push(Create());
            }

            var obj = pool.Pop();
            onGet?.Invoke(obj);
            activatedPool.Add(obj);
            return obj;
        }
        #endregion

        #region Protected - Validation
        /// <summary> 풀 객체가 아직 쓸 수 있는 상태인지 판정한다. Unity 객체 파생은 fake-null 을 보도록 재정의한다. </summary>
        protected virtual bool IsAlive(T obj) => obj != null;
        #endregion

        #region Public - Return
        public virtual void Return(T obj) {
            if (!activatedPool.Contains(obj)) {
                HLogger.Warning("[Pool] None pool object try return.");
                return;
            }

            // 파괴된 객체는 재사용 스택으로 되돌리지 않는다. 활성 목록에서만 지워 구멍을 메운다.
            // onReturn 도 부르지 않는다. 파괴된 대상을 건드리면 MissingReferenceException 이 난다.
            if (!IsAlive(obj)) {
                activatedPool.Remove(obj);
                HLogger.Warning(
                    $"[Pool] Destroyed object of type '{typeof(T).Name}' was returned and dropped. " +
                    $"The pool will create a new instance on the next Get. " +
                    $"Check the caller that destroyed it while it was still rented.");
                return;
            }

            onReturn?.Invoke(obj);
            pool.Push(obj);
            activatedPool.Remove(obj);
        }
        #endregion

        #region Public - Discard
        /// <summary> 외부 요인으로 사용 불가가 된 대여 중 객체를 장부에서만 제거한다. 반환 스택에는 넣지 않는다. </summary>
        public virtual bool Discard(T obj) {
            if (!activatedPool.Remove(obj)) {
                HLogger.Warning("[Pool] Discard target is not an activated object. Discard only objects handed out by Get.");
                return false;
            }
            return true;
        }
        #endregion

        #region Public - Dispose
        public virtual void Dispose() {
            foreach (var obj in pool) {
                onDispose?.Invoke(obj);
            }
            foreach (var obj in activatedPool) {
                HLogger.Warning($"[Pool] Object of type '{typeof(T).Name}' was not returned before Dispose.");
                onDispose?.Invoke(obj);
            }
            pool.Clear();
            activatedPool.Clear();
        }
        #endregion

        #region Public Abstract - Create
        protected abstract T Create();
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-04 (수정) :: 파괴된 객체의 Return 처리
 *
 * 변경 ::
 * IsAlive(T) 가상 훅 신설. Return 이 파괴된 객체를 받으면 재사용 스택에 넣지 않고
 * activatedPool 에서만 제거한다. ComponentPool / GameObjectPool 이 Unity fake-null 로 재정의.
 *
 * 이유 ::
 * AudioSpatialPool.PlayAt(clip, newParent) 로 외부 객체의 자식이 된 AudioSource 는 그 부모가
 * 파괴되면 함께 파괴된다. 기존 Return 은 파괴 여부를 보지 않고 pool.Push 하여 파괴된 객체가
 * 재사용 대상으로 들어갔고, 다음 Get 이 그것을 반환했다.
 *
 * 결과 ::
 * 파괴된 객체가 Get 으로 다시 나오지 않는다. CountTotal 이 줄지만 Get 이 필요 시 새로 만든다.
 *
 * 주의 ::
 * Dispose 의 activatedPool 순회는 여전히 파괴된 객체에도 onDispose 를 호출한다. 같은 성격의
 * 수정 대상이지만 이번 범위 밖이다.
 * =========================================================
 *
 * 2026-09-02 (수정) :: Discard 추가 - 외부 파괴 객체의 장부 정리 경로
 *
 * 변경 ::
 * 1. `Discard(T)` 추가. `activatedPool` 에서만 제거하고 `pool` 스택에는 넣지 않는다.
 *    대여 중이 아닌 객체를 넘기면 경고 후 false 를 돌려준다.
 *
 * 이유 ::
 * `Return` 은 `onReturn` 을 반드시 거치므로 이미 파괴된 UnityEngine.Object 에는 쓸 수 없다.
 * `onReturn` 이 `Stop()` / `SetParent()` 같은 인스턴스 API 를 호출해 MissingReferenceException
 * 이 나기 때문이다. 그렇다고 호출부가 `if (obj)` 로 걸러 건너뛰면 그 객체는 `activatedPool` 에
 * 영원히 남아 풀이 재고 손실을 인지하지 못한다. 반환과 폐기를 다른 연산으로 가른다.
 *
 * 결과 ::
 * 호출부는 "살아있으면 Return, 죽었으면 Discard" 로 두 경로를 명시적으로 처리할 수 있다.
 * `CountActivated` 가 실제 대여 수와 다시 일치한다.
 *
 * 주의 ::
 * `Discard` 는 `onDispose` 를 부르지 않는다. 대상이 이미 파괴된 상황을 전제하기 때문이다.
 * 살아있는 객체를 의도적으로 풀에서 빼내는 용도로 쓰면 그 객체의 해제는 호출부 책임이 된다.
 *
 * =========================================================
 *
 * @Jason - PKH
 * 풀링 시스템의 유연성과 일관성을 생각하여 작성한 클래스입니다.
 * 원하는 어떠한 타입이든 모두 적용 가능한 것이 첫번째 목표였습니다.
 * 그리고 런타임 전, 미리 씬 혹은 백그라운드에 설정할 수 있는 기능을 위하여
 * 'UnityPoolEntity' 객체를 통해 원하는 값을 미리 정의하고 'PoolManager'가 시작함과 동시에
 * 해당 값을 토대로 풀링 객체들을 생성하여 키값으로 가져오는 것을 두번째 목표로 하였습니다.
 * ------------------------------------
 * 이를 베이스로 다음 3개의 파생 클래스가 작성되었습니다.
 * 1. 게임오브젝트(GameObjectPool)
 * + 초기 클래스로 수정 혹은 제거될 수 있습니다.
 * 2. 일반 C# 클래스(ClassPool)
 * 3. 풀링이 가능한('IPoolable'을 상속받은 'PoolableMono'클래스) 오브젝트 = 유니티 GUI 친화적 환경을 위한 클래스
 * + 일반 MonoBehaviour 타입을 풀링 타겟으로 설정시, 실제 풀링에 사용될 컴포넌트 추출이 어려워 생성되었습니다.
 * ------------------------------------
 * @Jason - PKH
 * 1. 최근 반환된 오브젝트가 메모리 캐시에 더 가깝게 존재하는 경향이 있어서 큐에서 스택으로 변환했습니다.
 * 2. HashSet으로 Return 과정의 중복성 검사를 진행합니다.
 * ------------------------------------
 * @Jason - PKH 14.09.25
 * 1. public Create함수를 선언하여 필요시 여분의 풀링오브젝트를 미리생성하도록 만듭니다.
 */
#endif