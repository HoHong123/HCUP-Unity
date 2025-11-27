#if UNITY_EDITOR && ODIN_INSPECTOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using HUtil.Logger;

namespace HUtil.Editor {
    public class FileBrowser : OdinEditorWindow {
        [MenuItem("Data/HView/File Browser")]
        private static void Open() {
            var w = GetWindow<FileBrowser>();
            w.titleContent = new GUIContent("File Browser");
            w.Show();
        }

        #region Path
        [Title("Query")]
        [FolderPath(AbsolutePath = false, ParentFolder = "Assets")]
        public string folder = "Assets";
        #endregion

        #region Target Type
        [LabelText("Type")]
        [ValueDropdown(nameof(GetScriptableTypes))]
        public Type scriptableType = typeof(ScriptableObject);

        [ToggleLeft, LabelText("하위 폴더 포함")]
        public bool includeSubfolders = true;

        [Button(ButtonSizes.Large), GUIColor(0.35f, 0.8f, 0.9f)]
        public void Refresh() => _LoadAssets();
        #endregion

        #region GUI Button
        [ButtonGroup, Button("폴더 열기")]
        private void OpenFolder() {
            var address = "Assets/" + folder;
            var path = System.IO.Path.GetFullPath(address);
            EditorUtility.RevealInFinder(path);
        }

        [ButtonGroup, Button("모두 선택")]
        private void SelectAll() {
            Selection.objects = assets.Cast<UnityEngine.Object>().ToArray();
        }

        [ButtonGroup, Button("모두 Ping")]
        private void PingAll() {
            foreach (var a in assets)
                EditorGUIUtility.PingObject(a);
        }
        #endregion

        #region File List
        [Title("Results")]
        [Searchable]
        [ListDrawerSettings(DraggableItems = false, ShowIndexLabels = true, NumberOfItemsPerPage = 20)]
        [InlineEditor(InlineEditorObjectFieldModes.CompletelyHidden)]
        public List<ScriptableObject> assets = new();
        #endregion


        private IEnumerable<ValueDropdownItem<Type>> GetScriptableTypes() {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => {
                    Type[] ts;
                    try { ts = a.GetTypes(); }
                    catch { ts = Array.Empty<Type>(); }
                    return ts;
                })
                .Where(t => typeof(ScriptableObject).IsAssignableFrom(t) && !t.IsAbstract)
                .OrderBy(t => t.Name);

            // 상단 ScriptableObject 선택 가능
            yield return new ValueDropdownItem<Type>("[Any ScriptableObject]", typeof(ScriptableObject));

            foreach (var t in types) {
                yield return new ValueDropdownItem<Type>(t.FullName, t);
            }
        }

        private void _LoadAssets() {
            assets.Clear();
            if (string.IsNullOrEmpty(folder)) {
                HLogger.Warning("[SO Browser] 주소값이 비었습니다.");
                return;
            }

            var address = "Assets/" + folder;
            string typeFilter = scriptableType == typeof(ScriptableObject) ? "t:ScriptableObject" : $"t:{scriptableType.Name}";
            var guids = AssetDatabase.FindAssets(typeFilter, new[] { address });

            foreach (var guid in guids) {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (!includeSubfolders) {
                    var dir = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
                    var normFolder = address.Replace("\\", "/").TrimEnd('/');
                    if (!string.Equals(dir, normFolder, StringComparison.Ordinal)) {
                        continue;
                    }
                }

                var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (obj == null) continue;

                if (scriptableType != typeof(ScriptableObject) &&
                    !scriptableType.IsAssignableFrom(obj.GetType())) {
                    continue;
                }

                assets.Add(obj);
            }

            // 이름순 정렬
            assets = assets.OrderBy(a => a.name, StringComparer.OrdinalIgnoreCase).ToList();
            Repaint();
        }
    }
}

/* @Jason - PKH
 * === 스크립터블오브젝트를 확인할 수 있는 윈도우 기능 ===
 * 1. '오딘 인스펙터'를 활용하여 제작.
 * 2. 선택된 디렉토리 하위 디렉토리들까지 확인 가능하도록 설정.
 * 3. 윈도우에서 수정한 사항이 즉각 스크립터블오브젝트에 반영 가능.
 */
#endif
