#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * HImage 컴포넌트 전용 커스텀 인스펙터입니다.
 * HImage 컴포넌트의 에디터 작업 편의성을 높이고 Sprite 기반 UI 정렬 작업을 빠르게
 * 처리하기 위해 사용됩니다.
 *
 * 기능 ::
 * 1. HImage 전용 옵션 인스펙터 UI 제공
 * 2. Sprite 변경 시 RectTransform 정렬 옵션
 * 3. Base Position Bake 기능 제공
 * 4. Sprite Pivot 기준 정렬 기능 제공
 * 5. Alpha Hit Test 설정 및 적용 기능
 *
 * 특징 ::
 * Unity 기본 ImageEditor를 상속하여 기본 Image Inspector 기능을 유지하면서 전용 기능을
 * 추가 제공합니다.
 * =========================================================
 */

using UnityEditor;
using UnityEngine;

namespace HUI.Editor.ImageUI {
    [CustomEditor(typeof(HUI.ImageUI.HImage))]
    public class HImageEditor : UnityEditor.UI.ImageEditor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            serializedObject.Update();

            GUILayout.Space(8);
            _DrawAlphaHitTest();
            GUILayout.Space(8);

            var useChanged = serializedObject.FindProperty("alignOnSpriteChanged");
            var useCustomBase = serializedObject.FindProperty("useCustomBasePosition");
            EditorGUILayout.PropertyField(useChanged, new GUIContent("Align On Sprite Change"));
            EditorGUILayout.PropertyField(useCustomBase, new GUIContent("Use Custom Base Position"));

            var image = (HUI.ImageUI.HImage)target;
            if (GUILayout.Button("Bake Base Position")) {
                Undo.RecordObject(image.rectTransform, "Bake Base Position");
                image.BakeBasePosition();
                EditorUtility.SetDirty(image);
            }

            if (GUILayout.Button("Align To Sprite Pivot")) {
                Undo.RecordObject(image.rectTransform, "Align To Sprite Pivot");
                image.AlignToSpritePivot();
                EditorUtility.SetDirty(image);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void _DrawAlphaHitTest() {
            var useProp = serializedObject.FindProperty("useAlphaHitTest");
            var thresholdProp = serializedObject.FindProperty("alphaHitThreshold");

            if (useProp == null || thresholdProp == null) return;

            EditorGUILayout.PropertyField(useProp, new GUIContent("Use Alpha Hit Test"));

            using (new EditorGUI.DisabledScope(!useProp.boolValue)) {
                EditorGUILayout.PropertyField(thresholdProp, new GUIContent("Alpha Hit Threshold"));
                if (GUILayout.Button("Apply Alpha Hit Test")) {
                    var image = (HUI.ImageUI.HImage)target;
                    Undo.RecordObject(image, "Apply Alpha Hit Test");
                    image.ApplyAlphaHitTest();
                    EditorUtility.SetDirty(image);
                }
            }
        }
    }
}

/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. Alpha Hit Test 설정 UI 제공
 * 2. Sprite Pivot 기준 RectTransform 정렬
 * 3. Base Position Bake 기능
 * 4. Sprite 변경 시 자동 정렬 옵션
 *
 * 사용법 ::
 * HImage 컴포넌트를 선택하면
 * 기본 Image Inspector 아래에 추가 기능이 표시됩니다.
 *
 * 버튼 기능 ::
 * Bake Base Position
 *  + 현재 RectTransform 위치를 BasePosition으로 저장
 * Align To Sprite Pivot
 *  + Sprite Pivot 기준으로 RectTransform 정렬
 * Apply Alpha Hit Test
 *  + Alpha 기반 Raycast Hit Test 적용
 *
 * 기타 ::
 * Unity 기본 ImageEditor를 확장하여 제작된 커스텀 Inspector입니다.
 * =========================================================
 */
#endif
