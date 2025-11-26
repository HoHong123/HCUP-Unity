using System;

namespace HUtil.Pooling {
    public class ClassPool<T> : BasePool<T> where T : class, new() {
        public ClassPool(int initSize = 1,
            Action<T> onCreate = null, Action<T> onGet = null,
            Action<T> onReturn = null, Action<T> onDispose = null)
            : base(onCreate, onGet, onReturn, onDispose)
            => Init(initSize);

        protected override T Create() {
            var newOne = new T();
            onCreate?.Invoke(newOne);
            return newOne;
        }
    }
}

#if UNITY_EDITOR
/* @Jason - PKH
 * 
 */
#endif