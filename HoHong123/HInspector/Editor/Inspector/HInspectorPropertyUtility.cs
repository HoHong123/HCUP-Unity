#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace HInspector.Editor {
    internal static class HInspectorPropertyUtility {
        static readonly Dictionary<Type, Dictionary<string, MemberInfo>> memberCache = new Dictionary<Type, Dictionary<string, MemberInfo>>();

        #region Public Functions
        public static object GetParentObject(SerializedProperty property) {
            if (property == null || property.serializedObject == null)
                return null;

            object currentObject = property.serializedObject.targetObject;
            if (currentObject == null)
                return null;

            string normalizedPath = property.propertyPath.Replace(".Array.data[", "[");
            string[] elements = normalizedPath.Split('.');

            for (int i = 0; i < elements.Length - 1; i++) {
                currentObject = _ResolvePathElement(currentObject, elements[i]);
                if (currentObject == null)
                    return null;
            }

            return currentObject;
        }

        public static bool TryGetMemberValue(object targetObject, string memberName, out object value) {
            return _TryGetMemberValue(targetObject, memberName, out value);
        }

        public static bool TryInvokeParameterlessOrSingleParameterMethod(object targetObject, string methodName, object argument) {
            if (targetObject == null || string.IsNullOrEmpty(methodName))
                return false;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo[] methods = targetObject.GetType().GetMethods(flags);

            for (int i = 0; i < methods.Length; i++) {
                MethodInfo methodInfo = methods[i];
                if (methodInfo.Name != methodName)
                    continue;

                ParameterInfo[] parameters = methodInfo.GetParameters();
                if (parameters.Length == 1) {
                    if (!_TryConvertValue(argument, parameters[0].ParameterType, out object convertedArgument))
                        continue;

                    methodInfo.Invoke(targetObject, new[] { convertedArgument });
                    return true;
                }

                if (parameters.Length == 0) {
                    methodInfo.Invoke(targetObject, null);
                    return true;
                }
            }

            return false;
        }

        public static bool TryCompare(object left, object right, out int result) {
            result = 0;

            if (left == null || right == null)
                return false;

            Type leftType = left.GetType();
            Type rightType = right.GetType();

            if (leftType == rightType && left is IComparable comparable) {
                result = comparable.CompareTo(right);
                return true;
            }

            if (_IsNumeric(leftType) && _IsNumeric(rightType)) {
                double leftValue = Convert.ToDouble(left);
                double rightValue = Convert.ToDouble(right);
                result = leftValue.CompareTo(rightValue);
                return true;
            }

            if (leftType.IsEnum) {
                if (rightType == typeof(string)) {
                    if (!Enum.IsDefined(leftType, right))
                        return false;

                    object parsed = Enum.Parse(leftType, (string)right);
                    result = Convert.ToInt32(left).CompareTo(Convert.ToInt32(parsed));
                    return true;
                }

                if (_IsNumeric(rightType)) {
                    result = Convert.ToInt32(left).CompareTo(Convert.ToInt32(right));
                    return true;
                }

                if (rightType.IsEnum && leftType == rightType) {
                    result = Convert.ToInt32(left).CompareTo(Convert.ToInt32(right));
                    return true;
                }
            }

            if (rightType.IsEnum) {
                if (leftType == typeof(string)) {
                    if (!Enum.IsDefined(rightType, left))
                        return false;

                    object parsed = Enum.Parse(rightType, (string)left);
                    result = Convert.ToInt32(parsed).CompareTo(Convert.ToInt32(right));
                    return true;
                }

                if (_IsNumeric(leftType)) {
                    result = Convert.ToInt32(left).CompareTo(Convert.ToInt32(right));
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetSerializedValue(SerializedProperty property, out object value) {
            switch (property.propertyType) {
            case SerializedPropertyType.Integer:
                value = property.intValue;
                return true;
            case SerializedPropertyType.Boolean:
                value = property.boolValue;
                return true;
            case SerializedPropertyType.Float:
                value = property.floatValue;
                return true;
            case SerializedPropertyType.String:
                value = property.stringValue;
                return true;
            case SerializedPropertyType.Vector2:
                value = property.vector2Value;
                return true;
            case SerializedPropertyType.Vector3:
                value = property.vector3Value;
                return true;
            case SerializedPropertyType.Enum:
                value = property.enumValueIndex;
                return true;
            case SerializedPropertyType.ObjectReference:
                value = property.objectReferenceValue;
                return true;
            default:
                value = null;
                return false;
            }
        }
        #endregion

        #region Private Functions
        static object _ResolvePathElement(object currentObject, string element) {
            if (currentObject == null || string.IsNullOrEmpty(element))
                return null;

            int bracketIndex = element.IndexOf('[');
            if (bracketIndex < 0)
                return _GetDirectMemberValue(currentObject, element);

            string collectionName = element.Substring(0, bracketIndex);
            object collectionObject = _GetDirectMemberValue(currentObject, collectionName);
            if (collectionObject == null)
                return null;

            int indexStart = bracketIndex + 1;
            int indexLength = element.IndexOf(']') - indexStart;
            if (indexLength <= 0)
                return null;

            if (!int.TryParse(element.Substring(indexStart, indexLength), out int index))
                return null;

            return _GetIndexedValue(collectionObject, index);
        }

        static object _GetIndexedValue(object collectionObject, int index) {
            if (collectionObject == null || index < 0)
                return null;

            if (collectionObject is IList list) {
                if (index >= list.Count)
                    return null;

                return list[index];
            }

            if (collectionObject is IEnumerable enumerable) {
                IEnumerator enumerator = enumerable.GetEnumerator();
                int currentIndex = 0;

                while (enumerator.MoveNext()) {
                    if (currentIndex == index)
                        return enumerator.Current;

                    currentIndex++;
                }
            }

            return null;
        }

        static object _GetDirectMemberValue(object targetObject, string memberName) {
            if (!_TryGetMemberValue(targetObject, memberName, out object value))
                return null;

            return value;
        }

        static bool _TryGetMemberValue(object targetObject, string memberName, out object value) {
            value = null;

            if (targetObject == null || string.IsNullOrEmpty(memberName))
                return false;

            MemberInfo memberInfo = _GetCachedMember(targetObject.GetType(), memberName);
            if (memberInfo == null)
                return false;

            try {
                switch (memberInfo) {
                case FieldInfo fieldInfo:
                    value = fieldInfo.GetValue(targetObject);
                    return true;
                case PropertyInfo propertyInfo:
                    value = propertyInfo.GetValue(targetObject);
                    return true;
                case MethodInfo methodInfo:
                    value = methodInfo.Invoke(targetObject, null);
                    return true;
                default:
                    return false;
                }
            }
            catch {
                return false;
            }
        }

        static MemberInfo _GetCachedMember(Type type, string memberName) {
            if (type == null || string.IsNullOrEmpty(memberName))
                return null;

            if (!memberCache.TryGetValue(type, out Dictionary<string, MemberInfo> typeCache)) {
                typeCache = new Dictionary<string, MemberInfo>();
                memberCache[type] = typeCache;
            }

            if (typeCache.TryGetValue(memberName, out MemberInfo cachedMemberInfo))
                return cachedMemberInfo;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            FieldInfo fieldInfo = type.GetField(memberName, flags);
            if (fieldInfo != null) {
                typeCache[memberName] = fieldInfo;
                return fieldInfo;
            }

            PropertyInfo propertyInfo = type.GetProperty(memberName, flags);
            if (propertyInfo != null) {
                typeCache[memberName] = propertyInfo;
                return propertyInfo;
            }

            MethodInfo methodInfo = type.GetMethod(memberName, flags, null, Type.EmptyTypes, null);
            if (methodInfo != null) {
                typeCache[memberName] = methodInfo;
                return methodInfo;
            }

            typeCache[memberName] = null;
            return null;
        }

        static bool _TryConvertValue(object sourceValue, Type targetType, out object convertedValue) {
            convertedValue = null;

            try {
                if (targetType == null)
                    return false;

                if (sourceValue == null) {
                    if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                        return false;

                    convertedValue = null;
                    return true;
                }

                Type sourceType = sourceValue.GetType();

                if (targetType.IsAssignableFrom(sourceType)) {
                    convertedValue = sourceValue;
                    return true;
                }

                if (targetType.IsEnum) {
                    if (sourceType == typeof(string)) {
                        convertedValue = Enum.Parse(targetType, (string)sourceValue);
                        return true;
                    }

                    if (_IsNumeric(sourceType)) {
                        convertedValue = Enum.ToObject(targetType, sourceValue);
                        return true;
                    }
                }

                if (_IsNumeric(sourceType) && _IsNumeric(targetType)) {
                    convertedValue = Convert.ChangeType(sourceValue, targetType);
                    return true;
                }

                convertedValue = Convert.ChangeType(sourceValue, targetType);
                return true;
            }
            catch {
                convertedValue = null;
                return false;
            }
        }

        static bool _IsNumeric(Type type) {
            return type == typeof(byte) ||
                   type == typeof(sbyte) ||
                   type == typeof(short) ||
                   type == typeof(ushort) ||
                   type == typeof(int) ||
                   type == typeof(uint) ||
                   type == typeof(long) ||
                   type == typeof(ulong) ||
                   type == typeof(float) ||
                   type == typeof(double) ||
                   type == typeof(decimal);
        }
        #endregion
    }
}
#endif