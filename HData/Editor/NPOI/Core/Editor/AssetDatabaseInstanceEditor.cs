#if UNITY_EDITOR
/* =========================================================
 * AssetDatabaseInstance<T> 전용 CustomEditor
 *
 * 특징 ::
 * - 에셋 미존재 시 "파일 저장하기" 버튼 표시 (IsLoadedFromAsset == false 조건)
 * - open generic [CustomEditor(typeof(AssetDatabaseInstance<>), true)]로 파생 클래스 전체 적용
 *
 * 주의사항 ::
 * - target 을 AssetDatabaseInstance<T> 로 직접 캐스팅 불가 (generic 타입 소거).
 *   Reflection으로 IsLoadedFromAsset/CreateAsset 에 접근.
 * =========================================================
 */
#endif

#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HData.NPOI.Core.Editor {
    [CustomEditor(typeof(AssetDatabaseInstance<>), true)]
    public class AssetDatabaseInstanceEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            _DrawCreateAssetSection();
        }

        private void _DrawCreateAssetSection() {
            var isLoadedProp = target.GetType().GetProperty(
                "IsLoadedFromAsset",
                BindingFlags.Public | BindingFlags.Instance);
            if (isLoadedProp == null) return;
            if ((bool)isLoadedProp.GetValue(target)) return;

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "최초 한 번만 생성합니다. 저장 이후엔 자동으로 생성된 파일을 불러옵니다.",
                MessageType.Info);

            if (GUILayout.Button("파일 저장하기", GUILayout.Height(50))) {
                target.GetType()
                    .GetMethod("CreateAsset", BindingFlags.Public | BindingFlags.Instance)
                    ?.Invoke(target, null);
            }
        }
    }
}
#endif
