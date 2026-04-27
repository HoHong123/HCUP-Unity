#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HInspector.Editor {
    [CustomPropertyDrawer(typeof(HInspectorAttribute), true)]
    public class HInspectorPropertyDrawer : PropertyDrawer {
        #region Private Fields
        const float RequiredBoxHeight = 24f;
        const float RequiredBoxTopGap = 2f;

        // HListDrawer의 DefaultExpandedState는 세션당 1회만 초기화되어 사용자의 접기 조작을 방해하지 않아야 한다.
        static readonly HashSet<string> _listDefaultExpandedApplied = new HashSet<string>();
        #endregion

        #region Public Functions
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            HInspectorAttribute[] attributes = _GetAttributes();
            if (!_IsVisible(property, attributes)) return 0f;

            _ApplyListDrawerState(property, attributes);

            float totalHeight = EditorGUI.GetPropertyHeight(property, label, true);

            HMinMaxSliderAttribute minMaxSliderAttribute = attributes.OfType<HMinMaxSliderAttribute>().FirstOrDefault();
            if (minMaxSliderAttribute != null && property.propertyType == SerializedPropertyType.Vector2) {
                totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            HRequiredAttribute requiredAttribute = attributes.OfType<HRequiredAttribute>().FirstOrDefault();
            if (requiredAttribute != null && _IsRequiredEmpty(property)) {
                totalHeight += RequiredBoxHeight + RequiredBoxTopGap;
            }

            return totalHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            HInspectorAttribute[] attributes = _GetAttributes();
            if (!_IsVisible(property, attributes)) return;

            _ApplyListDrawerState(property, attributes);

            bool isReadOnly = _EvaluateReadOnly(property, attributes);
            GUIContent resolvedLabel = _ResolveLabel(label, attributes);

            bool previousEnabled = GUI.enabled;
            if (isReadOnly) GUI.enabled = false;

            EditorGUI.BeginChangeCheck();
            _DrawProperty(position, property, resolvedLabel, attributes);
            bool isChanged = EditorGUI.EndChangeCheck();

            GUI.enabled = previousEnabled;

            _DrawRequiredWarning(position, property, attributes);

            if (!isChanged) return;

            _ApplyPostConstraints(property, attributes);
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();

            _ProcessOnValueChanged(property, attributes);
        }
        #endregion

        #region Private Functions
        HInspectorAttribute[] _GetAttributes() {
            if (fieldInfo == null) return Array.Empty<HInspectorAttribute>();

            return fieldInfo
                .GetCustomAttributes(typeof(HInspectorAttribute), true)
                .Cast<HInspectorAttribute>()
                .OrderBy(attribute => attribute.Order)
                .ToArray();
        }

        bool _IsVisible(SerializedProperty property, HInspectorAttribute[] attributes) {
            object parentObject = HInspectorPropertyUtility.GetParentObject(property);
            if (parentObject == null) return true;

            for (int k = 0; k < attributes.Length; k++) {
                if (attributes[k] is HHideIfAttribute hideIfAttribute) {
                    if (_TryEvaluateCondition(parentObject, hideIfAttribute, out bool hideResult)) {
                        if (hideResult) return false;
                    }

                    continue;
                }

                if (attributes[k] is HShowIfAttribute showIfAttribute) {
                    if (_TryEvaluateCondition(parentObject, showIfAttribute, out bool showResult)) {
                        if (!showResult) return false;
                    }
                    else {
                        return false;
                    }
                }
            }

            return true;
        }

        bool _EvaluateReadOnly(SerializedProperty property, HInspectorAttribute[] attributes) {
            HListDrawerAttribute listAttribute = attributes.OfType<HListDrawerAttribute>().FirstOrDefault();
            if (listAttribute != null && listAttribute.IsReadOnly && _IsCollectionField()) return true;

            HEnableIfAttribute enableIfAttribute = attributes.OfType<HEnableIfAttribute>().FirstOrDefault();
            if (enableIfAttribute != null) return !_EvaluateEnableIf(property, enableIfAttribute);

            HReadOnlyAttribute readOnlyAttribute = attributes.OfType<HReadOnlyAttribute>().FirstOrDefault();
            if (readOnlyAttribute == null) return false;

            if (string.IsNullOrEmpty(readOnlyAttribute.ConditionMemberName)) {
                return true;
            }

            object parentObject = HInspectorPropertyUtility.GetParentObject(property);
            if (parentObject == null) return true;

            if (!HInspectorPropertyUtility.TryGetMemberValue(parentObject, readOnlyAttribute.ConditionMemberName, out object value)) {
                return true;
            }

            if (value is bool boolValue) {
                return readOnlyAttribute.Inverse ? !boolValue : boolValue;
            }

            return true;
        }

        bool _EvaluateEnableIf(SerializedProperty property, HEnableIfAttribute attribute) {
            object parentObject = HInspectorPropertyUtility.GetParentObject(property);
            if (parentObject == null) return true;

            if (attribute.IsExpression)
                return HInspectorExpressionUtility.TryEvaluate(parentObject, attribute.Expression, out bool exprResult) && exprResult;

            if (string.IsNullOrEmpty(attribute.Condition))
                return true;

            if (!HInspectorPropertyUtility.TryGetMemberValue(parentObject, attribute.Condition, out object value))
                return false;

            if (value is bool boolValue)
                return boolValue;

            return true;
        }

        GUIContent _ResolveLabel(GUIContent originalLabel, HInspectorAttribute[] attributes) {
            HHideLabelAttribute hideLabelAttribute = attributes.OfType<HHideLabelAttribute>().FirstOrDefault();
            if (hideLabelAttribute != null) return GUIContent.none;

            HLabelTextAttribute labelTextAttribute = attributes.OfType<HLabelTextAttribute>().FirstOrDefault();
            if (labelTextAttribute != null) return new GUIContent(labelTextAttribute.Text, originalLabel.tooltip);

            return originalLabel;
        }

        bool _IsRequiredEmpty(SerializedProperty property) {
            switch (property.propertyType) {
            case SerializedPropertyType.ObjectReference:
                return property.objectReferenceValue == null;
            case SerializedPropertyType.String:
                return string.IsNullOrEmpty(property.stringValue);
            case SerializedPropertyType.ExposedReference:
                return property.exposedReferenceValue == null;
            default:
                return false;
            }
        }

        void _DrawRequiredWarning(Rect fieldRect, SerializedProperty property, HInspectorAttribute[] attributes) {
            HRequiredAttribute requiredAttribute = attributes.OfType<HRequiredAttribute>().FirstOrDefault();
            if (requiredAttribute == null) return;

            if (!_IsRequiredEmpty(property)) return;

            string message = string.IsNullOrEmpty(requiredAttribute.Message)
                ? $"'{property.displayName}' is required"
                : requiredAttribute.Message;

            float fieldBottom = fieldRect.y + EditorGUI.GetPropertyHeight(property, true);
            Rect warningRect = new Rect(fieldRect.x, fieldBottom + RequiredBoxTopGap, fieldRect.width, RequiredBoxHeight);
            EditorGUI.HelpBox(warningRect, message, MessageType.Warning);
        }

        bool _TryEvaluateCondition(object parentObject, HShowIfAttribute attribute, out bool result) {
            result = false;

            if (parentObject == null) return false;

            if (attribute.IsExpression)
                return HInspectorExpressionUtility.TryEvaluate(parentObject, attribute.Expression, out result);

            if (string.IsNullOrEmpty(attribute.MemberName))
                return false;

            if (!HInspectorPropertyUtility.TryGetMemberValue(parentObject, attribute.MemberName, out object currentValue))
                return false;

            if (!attribute.HasCompareValue) {
                if (currentValue is bool boolValue) {
                    result = boolValue;
                    return true;
                }

                return false;
            }

            if (!HInspectorPropertyUtility.TryCompare(currentValue, attribute.CompareValue, out int compareResult))
                return false;

            switch (attribute.CompareType) {
            case HCompareType.Equals:
                result = compareResult == 0;
                return true;
            case HCompareType.NotEquals:
                result = compareResult != 0;
                return true;
            case HCompareType.Greater:
                result = compareResult > 0;
                return true;
            case HCompareType.Less:
                result = compareResult < 0;
                return true;
            case HCompareType.GreaterOrEqual:
                result = compareResult >= 0;
                return true;
            case HCompareType.LessOrEqual:
                result = compareResult <= 0;
                return true;
            default:
                return false;
            }
        }

        void _DrawProperty(Rect position, SerializedProperty property, GUIContent label, HInspectorAttribute[] attributes) {
            HMinMaxSliderAttribute minMaxSliderAttribute = attributes.OfType<HMinMaxSliderAttribute>().FirstOrDefault();
            if (minMaxSliderAttribute != null) {
                _DrawMinMaxSlider(position, property, label, minMaxSliderAttribute);
                return;
            }

            EditorGUI.PropertyField(position, property, label, true);
        }

        void _DrawMinMaxSlider(Rect position, SerializedProperty property, GUIContent label, HMinMaxSliderAttribute attribute) {
            switch (property.propertyType) {
            case SerializedPropertyType.Vector2: {
                    Rect controlRect = EditorGUI.PrefixLabel(position, label);

                    float lineHeight = EditorGUIUtility.singleLineHeight;
                    float verticalSpacing = EditorGUIUtility.standardVerticalSpacing;

                    Rect sliderRect = new Rect(controlRect.x, controlRect.y, controlRect.width, lineHeight);

                    float halfWidth = (controlRect.width - 4f) * 0.5f;

                    Rect minRect = new Rect(controlRect.x, controlRect.y + lineHeight + verticalSpacing, halfWidth, lineHeight);
                    Rect maxRect = new Rect(minRect.x + halfWidth + 4f, minRect.y, halfWidth, lineHeight);

                    Vector2 currentValue = property.vector2Value;
                    float minValue = currentValue.x;
                    float maxValue = currentValue.y;

                    EditorGUI.MinMaxSlider(sliderRect, ref minValue, ref maxValue, attribute.Min, attribute.Max);
                    minValue = EditorGUI.FloatField(minRect, minValue);
                    maxValue = EditorGUI.FloatField(maxRect, maxValue);

                    minValue = Mathf.Clamp(minValue, attribute.Min, attribute.Max);
                    maxValue = Mathf.Clamp(maxValue, attribute.Min, attribute.Max);

                    if (maxValue < minValue)
                        maxValue = minValue;

                    property.vector2Value = new Vector2(minValue, maxValue);
                    break;
                }

            case SerializedPropertyType.Float: {
                    float currentValue = property.floatValue;
                    float nextValue = EditorGUI.Slider(position, label, currentValue, attribute.Min, attribute.Max);
                    property.floatValue = Mathf.Clamp(nextValue, attribute.Min, attribute.Max);
                    break;
                }

            case SerializedPropertyType.Integer: {
                    int currentValue = property.intValue;
                    float sliderValue = EditorGUI.Slider(position, label, currentValue, attribute.Min, attribute.Max);
                    property.intValue = Mathf.RoundToInt(Mathf.Clamp(sliderValue, attribute.Min, attribute.Max));
                    break;
                }

            default:
                EditorGUI.PropertyField(position, property, label, true);
                break;
            }
        }

        void _ApplyPostConstraints(SerializedProperty property, HInspectorAttribute[] attributes) {
            HMinAttribute minAttribute = attributes.OfType<HMinAttribute>().FirstOrDefault();
            HMaxAttribute maxAttribute = attributes.OfType<HMaxAttribute>().FirstOrDefault();

            if (minAttribute == null && maxAttribute == null)
                return;

            switch (property.propertyType) {
            case SerializedPropertyType.Integer: {
                    int value = property.intValue;

                    if (minAttribute != null)
                        value = Mathf.Max(value, Mathf.RoundToInt(minAttribute.Min));

                    if (maxAttribute != null)
                        value = Mathf.Min(value, Mathf.RoundToInt(maxAttribute.Max));

                    property.intValue = value;
                    break;
                }

            case SerializedPropertyType.Float: {
                    float value = property.floatValue;

                    if (minAttribute != null)
                        value = Mathf.Max(value, minAttribute.Min);

                    if (maxAttribute != null)
                        value = Mathf.Min(value, maxAttribute.Max);

                    property.floatValue = value;
                    break;
                }

            case SerializedPropertyType.Vector2: {
                    Vector2 value = property.vector2Value;

                    if (minAttribute != null) {
                        value.x = Mathf.Max(value.x, minAttribute.Min);
                        value.y = Mathf.Max(value.y, minAttribute.Min);
                    }

                    if (maxAttribute != null) {
                        value.x = Mathf.Min(value.x, maxAttribute.Max);
                        value.y = Mathf.Min(value.y, maxAttribute.Max);
                    }

                    property.vector2Value = value;
                    break;
                }
            }
        }

        void _ProcessOnValueChanged(SerializedProperty property, HInspectorAttribute[] attributes) {
            HOnValueChangedAttribute onValueChangedAttribute = attributes.OfType<HOnValueChangedAttribute>().FirstOrDefault();
            if (onValueChangedAttribute == null) return;

            if (string.IsNullOrEmpty(onValueChangedAttribute.MethodName)) return;

            object parentObject = HInspectorPropertyUtility.GetParentObject(property);
            if (parentObject == null) return;

            if (!HInspectorPropertyUtility.TryGetSerializedValue(property, out object currentValue))
                currentValue = null;

            HInspectorPropertyUtility.TryInvokeParameterlessOrSingleParameterMethod(parentObject, onValueChangedAttribute.MethodName, currentValue);
            EditorUtility.SetDirty(property.serializedObject.targetObject);
        }

        void _ApplyListDrawerState(SerializedProperty property, HInspectorAttribute[] attributes) {
            if (!_IsCollectionField()) return;

            HListDrawerAttribute listAttribute = attributes.OfType<HListDrawerAttribute>().FirstOrDefault();
            if (listAttribute == null) return;
            if (!listAttribute.DefaultExpandedState) return;

            UnityEngine.Object targetObject = property.serializedObject.targetObject;
            if (targetObject == null) return;

            // 세션당 1회만 isExpanded를 강제. 이후 프레임은 사용자 조작을 존중한다.
            string key = targetObject.GetInstanceID() + ":" + property.propertyPath;
            if (_listDefaultExpandedApplied.Contains(key)) return;

            _listDefaultExpandedApplied.Add(key);
            property.isExpanded = true;
        }

        bool _IsCollectionField() {
            if (fieldInfo == null) return false;

            Type fieldType = fieldInfo.FieldType;
            if (fieldType.IsArray) return true;
            if (typeof(IList).IsAssignableFrom(fieldType)) return true;

            return false;
        }
        #endregion
    }
}
#endif
