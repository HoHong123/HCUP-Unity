#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Assets 하위 Unity 상대 경로 폴더를 재귀 생성하는 에디터 유틸.
 *
 * 사용 ::
 * - AssetFolderUtility.EnsureFolder("Assets/Data/Localization/Locales")
 * =========================================================
 */
#endif

using UnityEditor;

namespace HExcel.Core {
    public static class AssetFolderUtility {
#if UNITY_EDITOR
        /// <summary> unityPath("Assets/...") 폴더가 없으면 부모부터 재귀 생성. </summary>
        public static void EnsureFolder(string unityPath) {
            if (AssetDatabase.IsValidFolder(unityPath)) return;

            int lastSlash = unityPath.LastIndexOf('/');
            if (lastSlash <= 0) return;

            string parent = unityPath.Substring(0, lastSlash);
            string folderName = unityPath.Substring(lastSlash + 1);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
#endif
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.07.04 클래스 본체 UNITY_EDITOR 가드 추가
 *
 * # 변경
 * - 클래스 본체 #if UNITY_EDITOR 가드 추가 (ExcelLoader/AssetDatabaseInstance/HcupLocalizationTableLoader 디렉터리 관용 정합)
 *
 * =============================================================================
 * @Jason - PKH 2026.07.03 최초 작성
 *
 * # 목적
 * - HcupLocalizationTableLoader private _EnsureDirectory 를 공용 static 으로 추출
 * - HUnityLocalizationTableLoader 의 Locales 폴더 생성에도 공용 사용 (DRY)
 *
 * =============================================================================
 */
#endif
