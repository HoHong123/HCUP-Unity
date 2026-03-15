#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 일반 C# 클래스 타입을 대상으로 사용하는 풀링 클래스입니다.
 *
 * 특징 ::
 * - new() 제약을 사용하는 클래스 풀링
 * - BasePool을 기반으로 동작
 * - 객체 생성 시 자동으로 onCreate 이벤트 호출
 * =========================================================
 */
#endif

using System;

namespace HUtil.Pooling {
    public class ClassPool<T> : BasePool<T> where T : class, new() {
        #region Public - Constructors
        public ClassPool(int initSize = 1,
            Action<T> onCreate = null, Action<T> onGet = null,
            Action<T> onReturn = null, Action<T> onDispose = null)
            : base(onCreate, onGet, onReturn, onDispose)
            => Init(initSize);
        #endregion

        #region Protected - Create
        protected override T Create() {
            var newOne = new T();
            onCreate?.Invoke(newOne);
            return newOne;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * new() 제약 기반 클래스 풀링
 *
 * 생성 방식 ::
 * new T()
 *
 * 사용법 ::
 * var pool = new ClassPool<MyClass>(10);
 * var obj = pool.Get();
 *
 * 기타 ::
 * 일반 C# 객체 풀링 전용 클래스입니다.
 * =========================================================
 */
#endif