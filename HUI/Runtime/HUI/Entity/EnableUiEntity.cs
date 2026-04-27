using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace HUI.Entity {
    [Serializable]
    public class EnableUiEntity {
        [Title("Target Object")]
        [SerializeField]
        GameObject target;

        [Title("Press Settings")]
        [SerializeField]
        public bool enableOnDown = true;
        [SerializeField]
        public bool enableOnUp = false;

        [Title("Interaction Settings")]
        [SerializeField]
        public bool enableOnInteractive = true;
        [SerializeField]
        public bool enableOnNonInteractive = false;

        public GameObject Target => target;
    }
}