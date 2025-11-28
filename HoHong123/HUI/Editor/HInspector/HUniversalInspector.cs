using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HUI.HEditor {
    public abstract class HUniversalInspector : Editor {
        #region Fields
        Dictionary<string, FieldMeta> fieldMetaMap;
        #endregion

        #region Constructor / Initialize
        private void _BuildFieldMetaCache() {
            fieldMetaMap = new Dictionary<string, FieldMeta>();

            Type targetType = target.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            FieldInfo[] fields = targetType.GetFields(flags);

            foreach (FieldInfo field in fields) {
                if (field.IsNotSerialized)
                    continue;

                HInspectorAttribute[] attributes = field
                    .GetCustomAttributes(typeof(HInspectorAttribute), true)
                    .Cast<HInspectorAttribute>()
                    .OrderBy(a => a.Order)
                    .ToArray();

                if (attributes.Length == 0)
                    continue;

                fieldMetaMap[field.Name] = new FieldMeta(field, attributes);
            }
        }
        #endregion

        #region Unity Life Cycle
        private void OnEnable() {
            _BuildFieldMetaCache();
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;

            while (property.NextVisible(enterChildren)) {
                enterChildren = false;

                if (property.name == "m_Script") {
                    using (new EditorGUI.DisabledScope(true)) {
                        EditorGUILayout.PropertyField(property, true);
                    }

                    continue;
                }

                _DrawProperty(property);
            }

            serializedObject.ApplyModifiedProperties();
        }
        #endregion

        #region Functions
        private void _DrawProperty(SerializedProperty property) {
            if (!fieldMetaMap.TryGetValue(property.name, out FieldMeta meta)) {
                EditorGUILayout.PropertyField(property, true);
                return;
            }

            object targetObject = target;

            // 1) Visibility (ShowIf / HideIf)
            if (!_EvaluateVisibility(meta, targetObject)) return;

            // 2) Layout (Title / BoxGroup / ListDrawer / SerializeDictionary 등)
            _BeginLayout(meta, property);

            // 3) ReadOnly, Min/Max, Slider 등 적용
            bool wasGUIEnabled = GUI.enabled;
            //GUI.enabled = _EvaluateReadOnly(meta, targetObject) ? false : wasGUIEnabled;

            _DrawValueWithConstraints(meta, property, targetObject);

            GUI.enabled = wasGUIEnabled;

            // 4) Layout 종료
            _EndLayout(meta);

            // 5) OnValueChanged 처리
            _HandleOnValueChanged(meta, targetObject);
        }

        private bool _EvaluateVisibility(FieldMeta meta, object targetObject) {
            // HShowIf / HHideIf 속성들 평가
            // 하나라도 false면 숨김 등 원하는 정책으로 구현
            return true;
        }

        private void _BeginLayout(FieldMeta meta, SerializedProperty property) {
            // HTitle, HBoxGroup, HListDrawer, HSerializeDictionary 등
            // 그룹 시작 / 박스 시작 / 라벨 출력 등 처리
        }

        private void _DrawValueWithConstraints(FieldMeta meta, SerializedProperty property, object targetObject) {
            // HMin, HMax, HMinMaxSlider 등 적용
            // 내부에서 property.floatValue / intValue 조정 + PropertyField 호출
            EditorGUILayout.PropertyField(property, true);
        }

        private void _EndLayout(FieldMeta meta) {
            // BeginLayout와 짝 맞는 End 처리
        }

        private void _HandleOnValueChanged(FieldMeta meta, object targetObject) {
            // 이전 값 캐싱 -> 값 변경 감지 -> HOnValueChangedAttribute 호출
            // 초기 버전에서는 생략해도 됨 (추후 확장)
        }
        #endregion

        #region Debug
#if UNITY_EDITOR
#endif
        #endregion

        public class FieldMeta {
            public FieldInfo FieldInfo { get; }
            public HInspectorAttribute[] Attributes { get; }

            public FieldMeta(FieldInfo fieldInfo, HInspectorAttribute[] attributes) {
                FieldInfo = fieldInfo;
                Attributes = attributes;
            }

            public IEnumerable<T> GetAttributes<T>() where T : HInspectorAttribute {
                return Attributes.OfType<T>();
            }

            public T GetAttribute<T>() where T : HInspectorAttribute {
                return Attributes.OfType<T>().FirstOrDefault();
            }
        }
    }
}
