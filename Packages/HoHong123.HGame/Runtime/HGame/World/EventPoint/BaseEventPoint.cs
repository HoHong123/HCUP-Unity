using System;
using UnityEngine;
using Sirenix.OdinInspector;
using HGame.Character;
using HGame.World.EventAction;
using HUtil.Logger;

namespace HUtil._2D.Map {
    public abstract class BaseEventPoint<T> : MonoBehaviour where T : ICharacterCommand {
        [Title("Filter")]
        [SerializeField]
        protected EventTargetType filterType = EventTargetType.Layer;
        [ShowIf("@filterType == EventTargetType.Tag || filterType == EventTargetType.TagAndLayer")]
        [SerializeField][LabelText("Target Tag")]
        protected string[] targetTags;
        [ShowIf("@filterType == EventTargetType.Layer || filterType == EventTargetType.TagAndLayer")]
        [SerializeField][LabelText("Target Layer")]
        protected LayerMask targetMask = ~0; // Everything

        [Title("Collider")]
        [SerializeField][Required]
        protected Collider2D eventCollider;

        public string[] TargetTags => targetTags;
        public LayerMask TargetMask => targetMask;

        public event Action<T> OnEvent;

        #region Matches
        protected bool LayerMatch(GameObject go) => ((1 << go.layer) & targetMask) != 0;
        protected bool TagMatch(GameObject go) {
            if (targetTags == null || targetTags.Length == 0) return false;
            for (int k = 0; k < targetTags.Length; k++) {
                if (go.CompareTag(targetTags[k])) return true;
            }
            return false;
        }
        protected bool CheckMatch(GameObject go) => filterType switch {
            EventTargetType.Tag => TagMatch(go),
            EventTargetType.Layer => LayerMatch(go),
            EventTargetType.TagAndLayer => TagMatch(go) && LayerMatch(go),
            _ => false
        };
        #endregion

        #region Triggers
        protected virtual void OnCollisionEnter2D(Collision2D collision) {
            if (!CheckMatch(collision.gameObject)) return;
            if (!collision.transform.TryGetComponent(out T target)) return;
            OnEvent?.Invoke(target);
        }

        protected virtual void OnTriggerEnter2D(Collider2D collision) {
            if (!CheckMatch(collision.gameObject)) return;
            if (!collision.transform.TryGetComponent(out T target)) return;
            OnEvent?.Invoke(target);
        }

        protected virtual void OnCollisionEnter(Collision collision) {
            if (!CheckMatch(collision.gameObject)) return;
            if (!collision.transform.TryGetComponent(out T target)) return;
            OnEvent?.Invoke(target);
        }

        protected virtual void OnTriggerEnter(Collider collision) {
            if (!CheckMatch(collision.gameObject)) return;
            if (!collision.transform.TryGetComponent(out T target)) return;
            OnEvent?.Invoke(target);
        }
        #endregion

#if UNITY_EDITOR
        [Title("Debug")]
        [SerializeField]
        bool fillArea = false;
        [SerializeField]
        Color areaColor = Color.red;

        private void OnValidate() {
            if (!eventCollider) TryGetComponent(out eventCollider);
            var allTags = UnityEditorInternal.InternalEditorUtility.tags;
            for (int k = 0; k < targetTags.Length; k++) {
                var tag = targetTags[k];
                if (string.IsNullOrEmpty(tag) || Array.IndexOf(allTags, tag) >= 0) continue;
                HLogger.Error($"{name}: '{tag}' 태그가 Tag Manager에 없습니다.", gameObject);
            }
        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = areaColor;
            if (fillArea) {
                Gizmos.DrawCube(eventCollider.bounds.center, eventCollider.bounds.size);
            }
            else {
                Gizmos.DrawWireCube(eventCollider.bounds.center, eventCollider.bounds.size);
            }
        }
#endif
    }
}
