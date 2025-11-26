using UnityEngine;

namespace HUtil.Pooling {
    public class ParticlePoolingSystem : MonoBehaviour {
        [SerializeField]
        ParticleSystem particle;

        public event System.Action<ParticlePoolingSystem> OnStopped;

        public ParticleSystem Particle => particle;


        private void Awake() {
            if (!particle) particle = GetComponent<ParticleSystem>();
            var main = particle.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        private void OnParticleSystemStopped() {
            OnStopped?.Invoke(this);
        }
    }
}