using System;
using UnityEngine;
using HInspector;

namespace HUI.Entity {
    [Serializable]
    public class EnableUiEntity {
        [HTitle("Target Object")]
        [SerializeField]
        GameObject target;

        [HTitle("Press Settings")]
        [SerializeField]
        public bool enableOnDown = true;
        [SerializeField]
        public bool enableOnUp = false;

        [HTitle("Interaction Settings")]
        [SerializeField]
        public bool enableOnInteractive = true;
        [SerializeField]
        public bool enableOnNonInteractive = false;

        public GameObject Target => target;
    }
}