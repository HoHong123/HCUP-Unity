#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HResource.Subscription;

namespace HUtil.Editor.Subscription {
    [InitializeOnLoad]
    public static class AssetOwnerIdWatchRegistry {
        #region Public - Types
        [Serializable]
        public sealed class Entry {
            public int OwnerId;
            public UnityEngine.Object UnityOwner;
            public string ClassName;
            public string ContainerName;
            public string OwnerDisplayName;
            public string SourceTypeName;
            public string CreatedAt;
            public bool IsUnityObject;
            public bool IsAlive;
        }
        #endregion

        #region Fields
        static readonly Dictionary<int, Entry> table = new();
        static readonly List<int> removeBuffer = new();
        #endregion

        #region Properties
        public static IReadOnlyDictionary<int, Entry> Table => table;
        #endregion

        #region Constructors
        // 에디트 모드 구독. 도메인 로드마다 1회.
        static AssetOwnerIdWatchRegistry() {
            _Subscribe();
            EditorApplication.playModeStateChanged += _OnPlayModeStateChanged;
            EditorApplication.update += _EditorUpdate;
        }
        #endregion

        #region Subscription
        // AssetOwnerIdGenerator._ResetStatics 는 SubsystemRegistration 에서 OnIdCreated/OnIdReleased 를
        // null 로 비운다. 그 훅은 "런타임 구독자가 플레이 세션을 넘어 잔존하는 것" 을 막기 위한 것인데,
        // [InitializeOnLoad] 로 붙는 이 워처는 스스로 재구독할 방법이 없어 함께 끊겨 있었다.
        //
        // RuntimeInitializeLoadType 은 SubsystemRegistration → AfterAssembliesLoaded → BeforeSplashScreen
        // → BeforeSceneLoad → AfterSceneLoad 순서가 보장되므로, AfterAssembliesLoaded 에서 재구독하면
        // 리셋 이후임이 결정적이다. (Domain Reload 비활성 상태에서도 이 콜백은 진입할 때마다 실행된다.)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void _ResubscribeAfterRuntimeReset() {
            _Subscribe();
        }

        // 도메인 리로드 여부에 따라 두 진입점이 겹칠 수 있으므로 항상 해제 후 구독한다.
        private static void _Subscribe() {
            AssetOwnerIdGenerator.OnIdCreated -= _OnIdCreated;
            AssetOwnerIdGenerator.OnIdCreated += _OnIdCreated;

            AssetOwnerIdGenerator.OnIdReleased -= _OnIdReleased;
            AssetOwnerIdGenerator.OnIdReleased += _OnIdReleased;
        }
        #endregion

        #region Public - Register
        public static void Register(AssetOwnerId ownerId, object owner) => _OnIdCreated(ownerId, owner);
        public static void Unregister(AssetOwnerId ownerId) => _OnIdReleased(ownerId);
        public static void Clear() => table.Clear();
        #endregion

        #region Private - Register
        private static void _OnIdCreated(AssetOwnerId ownerId, object owner) {
            if (!ownerId.IsValid) return;
            Entry entry = _BuildEntry(ownerId, owner);
            table[ownerId.Value] = entry;
        }

        private static void _OnIdReleased(AssetOwnerId ownerId) {
            if (!ownerId.IsValid) return;
            table.Remove(ownerId.Value);
        }

        #endregion

        #region Private - Build
        static Entry _BuildEntry(AssetOwnerId ownerId, object owner) {
            Entry entry = new Entry {
                OwnerId = ownerId.Value,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
            };

            if (owner == null) {
                entry.ClassName = "(null)";
                entry.ContainerName = "(null)";
                entry.OwnerDisplayName = "(null)";
                entry.SourceTypeName = "(null)";
                entry.IsUnityObject = false;
                entry.IsAlive = false;
                return entry;
            }

            Type ownerType = owner.GetType();
            entry.ClassName = ownerType.Name;
            entry.SourceTypeName = ownerType.FullName ?? ownerType.Name;
            entry.OwnerDisplayName = owner.ToString();

            if (owner is UnityEngine.Object unityObject) {
                entry.UnityOwner = unityObject;
                entry.IsUnityObject = true;
                entry.IsAlive = unityObject != null;

                switch (unityObject) {
                case Component component:
                    entry.ContainerName = component.gameObject ? component.gameObject.name : "(Missing GameObject)";
                    entry.OwnerDisplayName = component.name;
                    break;

                case GameObject gameObject:
                    entry.ContainerName = gameObject.name;
                    entry.OwnerDisplayName = gameObject.name;
                    break;

                default:
                    entry.ContainerName = unityObject.name;
                    entry.OwnerDisplayName = unityObject.name;
                    break;
                }
            }
            else {
                entry.IsUnityObject = false;
                entry.IsAlive = true;
                entry.ContainerName = "(Non-Unity Owner)";
            }

            return entry;
        }
        #endregion

        #region Private - Update
        static void _EditorUpdate() {
            if (table.Count < 1) return;

            removeBuffer.Clear();

            foreach (KeyValuePair<int, Entry> pair in table) {
                Entry entry = pair.Value;
                if (entry == null) continue;
                if (!entry.IsUnityObject) continue;

                bool isAlive = entry.UnityOwner != null;
                entry.IsAlive = isAlive;

                if (isAlive) continue;

                removeBuffer.Add(pair.Key);
            }

            if (removeBuffer.Count < 1) return;

            for (int k = 0; k < removeBuffer.Count; k++) {
                table.Remove(removeBuffer[k]);
            }
        }

        static void _OnPlayModeStateChanged(PlayModeStateChange state) {
            // 2차 경로. 에디터 어셈블리에서 RuntimeInitializeOnLoadMethod 가 호출되지 않는 환경이면
            // AfterAssembliesLoaded 재구독이 통째로 누락되므로, 여기서 한 번 더 건다.
            // 이 시점은 Awake 이후일 수 있어 초기 발급을 놓칠 수 있다 — 1차 경로의 대체가 아니라 보완이다.
            if (state == PlayModeStateChange.EnteredPlayMode) {
                _Subscribe();
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode ||
                state == PlayModeStateChange.ExitingPlayMode) {
                Clear();
            }
        }
        #endregion
    }
}
#endif