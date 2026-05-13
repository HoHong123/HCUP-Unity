#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * [HSpritePreview] PropertyAttribute 전용 CustomPropertyDrawer.
 * 인스펙터 필드 하단에 Foldout 형태로 Sprite 미리보기를 렌더합니다.
 *
 * 특징 ::
 * - string 필드 : Resources.Load → Addressables.WaitForCompletion 순서로 탐색
 * - Object 필드 : Sprite → Texture2D 순으로 대응
 * - Foldout 기본값 = 열린 상태, 상태는 에디터 세션 동안 static 캐시로 유지
 *
 * 주의사항 ::
 * - PropertyDrawer 인스턴스는 Unity 가 재사용 → 모든 캐시 static 선언
 * - Addressables 핸들은 에디터 세션 동안 해제하지 않음 (텍스처 언로드 방지)
 * =========================================================
 */

using System.Collections.Generic;
using HDiagnosis.Logger;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HInspector.Editor {
    [CustomPropertyDrawer(typeof(HSpritePreviewAttribute))]
    public class HSpritePreviewDrawer : PropertyDrawer {
        static readonly Dictionary<string, Sprite> spriteCache   = new();
        static readonly Dictionary<string, AsyncOperationHandle<Sprite>> addrHandles   = new();
        static readonly Dictionary<string, bool> foldoutStates = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            float baseHeight = EditorGUIUtility.singleLineHeight;
            if (!_GetFoldout(property)) return baseHeight;
            float previewSize = ((HSpritePreviewAttribute)attribute).Size;
            return baseHeight + 2f + previewSize;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            float singleLine = EditorGUIUtility.singleLineHeight;
            float labelWidth = EditorGUIUtility.labelWidth;

            // 필드 행 : Foldout(라벨) + PropertyField(값)
            bool isOpen = _GetFoldout(property);
            isOpen = EditorGUI.Foldout(
                new Rect(position.x, position.y, labelWidth, singleLine),
                isOpen, label, true);
            _SetFoldout(property, isOpen);

            EditorGUI.PropertyField(
                new Rect(position.x + labelWidth, position.y, position.width - labelWidth, singleLine),
                property, GUIContent.none);

            if (!isOpen) return;

            // 미리보기 행
            float previewSize = ((HSpritePreviewAttribute)attribute).Size;
            var previewRect = new Rect(
                position.x + labelWidth,
                position.y + singleLine + 2f,
                previewSize, previewSize);

            var sprite = _GetSprite(property);
            if (sprite != null)
                _DrawSprite(previewRect, sprite);
            else
                EditorGUI.LabelField(previewRect, "—", EditorStyles.centeredGreyMiniLabel);
        }

        #region Private - Foldout State
        private static string _GetKey(SerializedProperty property) {
            var target = property.serializedObject?.targetObject;
            int id = target != null ? target.GetInstanceID() : 0;
            return id + "|" + property.propertyPath;
        }

        private static bool _GetFoldout(SerializedProperty property) {
            if (!foldoutStates.TryGetValue(_GetKey(property), out bool state)) return true;
            return state;
        }

        private static void _SetFoldout(SerializedProperty property, bool value) {
            foldoutStates[_GetKey(property)] = value;
        }
        #endregion

        #region Private - Sprite Loading
        private static Sprite _GetSprite(SerializedProperty property) {
            return property.propertyType switch {
                SerializedPropertyType.String          => _LoadFromKey(property.stringValue),
                SerializedPropertyType.ObjectReference => _LoadFromObject(property.objectReferenceValue),
                _                                      => null,
            };
        }

        private static Sprite _LoadFromKey(string key) {
            if (string.IsNullOrEmpty(key)) return null;
            if (spriteCache.TryGetValue(key, out var cached)) return cached;

            var sprite = Resources.Load<Sprite>(key);

            if (sprite == null) {
                try {
                    var handle = Addressables.LoadAssetAsync<Sprite>(key);
                    sprite = handle.WaitForCompletion();
                    if (handle.Status == AsyncOperationStatus.Succeeded && sprite != null) {
                        addrHandles[key] = handle;
                    } else {
                        Addressables.Release(handle);
                        sprite = null;
                    }
                }
                catch (System.Exception) {
                    sprite = null;
                }
            }

            if (sprite == null)
                HLogger.Warning($"[HSpritePreview] Sprite 로드 실패 — key: \"{key}\" (Resources 및 Addressables 모두 탐색)");

            spriteCache[key] = sprite;
            return sprite;
        }

        private static Sprite _LoadFromObject(Object obj) {
            if (obj == null) return null;
            if (obj is Sprite s) return s;

            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) return null;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null) return Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f));

            return null;
        }
        #endregion

        #region Private - Sprite Draw
        private static void _DrawSprite(Rect rect, Sprite sprite) {
            if (sprite.texture == null) return;
            Texture2D tex = sprite.texture;
            Rect sr = sprite.textureRect;
            var uv = new Rect(
                sr.x / tex.width,
                sr.y / tex.height,
                sr.width  / tex.width,
                sr.height / tex.height);
            GUI.DrawTextureWithTexCoords(rect, tex, uv, true);
        }
        #endregion
    }
}
#endif

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.13 로드 실패 시 HLogger.Warning 추가
 *
 * # 변경
 * - _LoadFromKey() : sprite == null (Resources + Addressables 모두 탐색 실패) 시
 *   HLogger.Warning 발화 후 null 캐싱
 * - HCUP.HInspector.Editor.asmdef : HCUP.HDiagnosis 참조 추가
 *
 * # 구현 결정
 * - 경고는 최초 탐색 시 1회만 발화 — null 캐싱 이후 재탐색 없으므로 매 프레임 중복 없음
 * - 빈 문자열(key == null / empty)은 경고 제외 — 필드 미입력 상태는 정상
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 미리보기 인라인 → Foldout 하단 분리
 *
 * # 변경
 * - GetPropertyHeight() : Foldout 열림/닫힘 상태에 따라 가변 높이 반환
 * - OnGUI() : 인라인(우측 정사각) → Foldout 라벨 + 값 필드 분리, 미리보기 행 하단 배치
 * - foldoutStates 캐시 추가 (static Dictionary<string, bool>)
 * - _GetKey / _GetFoldout / _SetFoldout 헬퍼 추가
 *
 * # 구현 결정
 * - 키 = instanceId + "|" + propertyPath : propertyPath 단독 사용 시 다른 SO 인스턴스의
 *   동일 필드가 Foldout 상태를 공유하는 문제 차단
 * - 기본값 = 열린 상태 : TryGetValue 실패 시 true 반환 (신규 필드는 항상 미리보기 노출)
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 HSpritePreviewDrawer 베이스 코드 생성
 *
 * # 목적
 * - HData.NPOI.Core.Editor 의 SpritePreviewDrawer 를 HInspector 패키지로 이전
 * - HSpritePreviewAttribute 와 동일 귀속처 (HInspector.Editor 네임스페이스)
 *
 * # 구현 결정
 * - static cache (spriteCache / addrHandles) : PropertyDrawer 인스턴스 재사용 대응.
 *   인스턴스 레벨 캐시는 재사용 시 초기화로 스프라이트 언로드 발생
 * - Addressables 핸들 미해제 : Release 시 refcount 0 → 텍스처 언로드 → 렌더 불가
 * - catch (System.Exception) : Addressables 키 미등록·카탈로그 미초기화 등
 *   다양한 예외를 null 폴백으로 수렴 (에디터 전용, 렌더 실패 = 대시 표시로 처리)
 *
 * =============================================================================
 */
#endif
