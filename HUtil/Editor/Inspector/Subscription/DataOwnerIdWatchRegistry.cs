#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HUtil.Data.Subscription;

namespace HUtil.Editor.Subscription {
    [InitializeOnLoad]
    public static class DataOwnerIdWatchRegistry {
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
        static DataOwnerIdWatchRegistry() {
            DataOwnerIdGenerator.OnIdCreated += _OnIdCreated;
            DataOwnerIdGenerator.OnIdReleased += _OnIdReleased;
            EditorApplication.playModeStateChanged += _OnPlayModeStateChanged;
            EditorApplication.update += _EditorUpdate;
        }
        #endregion

        #region Public - Register
        public static void Register(DataOwnerId ownerId, object owner) => _OnIdCreated(ownerId, owner);
        public static void Unregister(DataOwnerId ownerId) => _OnIdReleased(ownerId);
        public static void Clear() => table.Clear();
        #endregion

        #region Private - Register
        public static void _OnIdCreated(DataOwnerId ownerId, object owner) {
            if (!ownerId.IsValid) return;
            Entry entry = _BuildEntry(ownerId, owner);
            table[ownerId.Value] = entry;
        }

        public static void _OnIdReleased(DataOwnerId ownerId) {
            if (!ownerId.IsValid) return;
            table.Remove(ownerId.Value);
        }

        #endregion

        #region Private - Build
        static Entry _BuildEntry(DataOwnerId ownerId, object owner) {
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
            if (state == PlayModeStateChange.EnteredEditMode ||
                state == PlayModeStateChange.ExitingPlayMode) {
                Clear();
            }
        }
        #endregion
    }
}
#endif