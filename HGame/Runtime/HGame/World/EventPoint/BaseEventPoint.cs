using System;
using UnityEngine;
using HGame.Character;
using HGame.World.EventAction;
using HDiagnosis.Logger;
using HInspector;

namespace HGame.H2D.Map {
    public abstract class BaseEventPoint<T> : MonoBehaviour where T : ICharacterCommand {
        [HTitle("Filter")]
        [SerializeField]
        protected EventTargetType filterType = EventTargetType.Layer;
        [HShowIf("@filterType == EventTargetType.Tag || filterType == EventTargetType.TagAndLayer")]
        [SerializeField][HLabelText("Target Tag")]
        protected string[] targetTags;
        [HShowIf("@filterType == EventTargetType.Layer || filterType == EventTargetType.TagAndLayer")]
        [SerializeField][HLabelText("Target Layer")]
        protected LayerMask targetMask = ~0; // Everything

        [HTitle("Collider")]
        [SerializeField][HRequired]
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
        // filterType 은 [Flags] 비트 조합이다 — 정확값 switch 는 Tag|Layer(3) 조합이 TagAndLayer(3)
        // 이외의 값으로 들어오면(예: 코드에서 직접 OR) 전부 거부했다. 비트 판정으로 교정.
        protected bool CheckMatch(GameObject go) {
            bool requireTag = (filterType & EventTargetType.Tag) != 0;
            bool requireLayer = (filterType & EventTargetType.Layer) != 0;
            if (!requireTag && !requireLayer) return false;
            if (requireTag && !TagMatch(go)) return false;
            if (requireLayer && !LayerMatch(go)) return false;
            return true;
        }
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
        [HTitle("Debug")]
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
