#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * ParticleSystem 풀링을 지원하기 위한 보조 컴포넌트입니다.
 * 파티클 재생이 끝난 시점을 감지하여 풀링 시스템에서 자동으로 Return 처리하기 위함입니다.
 * 
 * 기능 ::
 * ParticleSystem이 재생 종료될 때, OnStopped 이벤트를 통해 외부로 알림을 전달합니다.
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUtil.Pooling {
    public class ParticlePoolingSystem : MonoBehaviour {
        #region Fields
        [SerializeField]
        ParticleSystem particle;

        public event System.Action<ParticlePoolingSystem> OnStopped;
        #endregion

        #region Properties
        public ParticleSystem Particle => particle;
        #endregion

        #region Private - Unity Life Cycle
        private void Awake() {
            if (!particle) particle = GetComponent<ParticleSystem>();
            var main = particle.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        private void OnParticleSystemStopped() {
            OnStopped?.Invoke(this);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH 2026.03.10
 *
 * 주요 기능 ::
 * ParticleSystem 종료 감지
 * OnParticleSystemStopped
 *  + 파티클 종료 이벤트 발생
 *
 * 이벤트 ::
 * OnStopped
 *
 * 사용법 ::
 * pool.Get().Particle.Play();
 *
 * 기타 ::
 * 파티클 재생 종료 시 자동 Return 처리를 위한 보조 컴포넌트입니다.
 * =========================================================
 */
#endif