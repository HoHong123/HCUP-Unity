using UnityEngine;

namespace HGame.Sample.Parallex {
    public class SampleInputManager : MonoBehaviour {
        [SerializeField]
        SWUInputAction input;

        private void Awake() {
            input = new();
        }

        private void OnEnable() {
            input.Enable();
        }

        private void OnDisable() {
            input.Disable();
        }
    }
}
