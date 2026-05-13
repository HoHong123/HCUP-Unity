#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 이 스크립트는 ScriptableObject 기반 데이터 에셋을 에디터에서 싱글톤처럼 접근하기
 * 위한 베이스 클래스입니다.
 *
 * AssetDatabase를 이용하여 특정 ScriptableObject 타입의 에셋을 자동 탐색하고
 * Instance로 로드합니다.
 *
 * 주요 기능 ::
 * 1. AssetDatabase에서 타입 기반으로 에셋 검색
 * 2. 최초 접근 시 자동 로드
 * 3. 에셋이 존재하지 않을 경우 임시 인스턴스 생성
 * 4. 버튼을 통해 ScriptableObject 파일 생성
 *
 * 사용법 ::
 * 1. ExcelLoader 또는 데이터 Loader 클래스에서 상속하여 사용합니다.
 *
 * 예시 ::
 * public class EquipmentTableLoader : AssetDatabaseInstance<EquipmentTableLoader>
 *
 * 2. Instance를 통해 접근합니다.
 *
 * var loader = EquipmentTableLoader.Instance;
 *
 * 동작 흐름 ::
 * Instance 호출
 *  → AssetDatabase.FindAssets
 *  → ScriptableObject 로드
 *  → 없으면 CreateInstance
 *
 * 주의사항 ::
 * 1. UNITY_EDITOR 환경에서만 동작합니다.
 * 2. 에셋이 없는 경우 메모리 인스턴스만 생성됩니다.
 * 3. 반드시 "파일 저장하기" 버튼으로 에셋을 생성해야 영구 저장됩니다.
 *
 * 사용 목적 ::
 * Excel Loader 등의 에디터 데이터 관리 시스템의 베이스 클래스입니다.
 * =========================================================
 */
#endif

using System;
using System.Linq;
using UnityEngine;
using HDiagnosis.Logger;

namespace HData.NPOI.Core {
    [Serializable]
    public abstract class AssetDatabaseInstance<T> : ScriptableObject
        where T : AssetDatabaseInstance<T>, new() {
#if UNITY_EDITOR
        #region Fields
        bool isLoadedFromAsset = true;
        public bool IsLoadedFromAsset => isLoadedFromAsset;
        #endregion

        #region Singleton Instance
        static T instance = null;
        public static T Instance {
            get {
                if(instance == null) {
                    var guid = UnityEditor.AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T).Name)).FirstOrDefault();
                    HLogger.Log(guid);
                    if(guid == default) {
                        instance = ScriptableObject.CreateInstance<T>();
                        instance.isLoadedFromAsset = false;
                    } else {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        HLogger.Log(path);
                        instance = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                    }
                }
                return instance;
            }
        }
        #endregion

        #region Public - Create Asset
        public void CreateAsset() {
            string path = UnityEditor.EditorUtility.SaveFilePanel("생성하기", "Assets", typeof(T).Name, "asset");
            if(!path.StartsWith(Application.dataPath)) return;
            path = path.Replace(Application.dataPath, "Assets");
            HLogger.Log(path);
            try {
                instance.isLoadedFromAsset = true;
                UnityEditor.AssetDatabase.CreateAsset(instance, path);
            }
            catch(Exception e) {
                HLogger.Error(e.Message);
                return;
            }
        }

        internal void CreateAssetAt(string assetPath) {
            if (string.IsNullOrEmpty(assetPath)) {
                HLogger.Error("[AssetDatabaseInstance] assetPath가 비어있습니다.");
                return;
            }
            try {
                instance.isLoadedFromAsset = true;
                UnityEditor.AssetDatabase.CreateAsset(instance, assetPath);
                UnityEditor.AssetDatabase.SaveAssets();
            }
            catch (Exception e) {
                HLogger.Error(e.Message);
                instance.isLoadedFromAsset = false;
            }
        }
        #endregion
#endif
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.13 CreateAssetAt(string) 추가
 *
 * # 추가
 * - CreateAssetAt(string assetPath) : 파일 피커 없이 지정 경로에 에셋 생성
 * - ExcelLoaderEditor 에서 ImportData 시 dataOutputPath 를 받아 자동 생성용
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 컨벤션 정리 — private 필드 접근제어자 제거
 *
 * # 변경
 * - private bool isLoadedFromAsset → bool isLoadedFromAsset (접근제어자 제거)
 * - private static T instance → static T instance (접근제어자 제거)
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 Logger 네임스페이스 수정
 *
 * # 변경
 * - using HUtil.Logger → using HDiagnosis.Logger (패키지 분리 후 올바른 경로로 수정)
 *
 * =============================================================================
 * @Jason - PKH 2026.05.12 M2 — Odin SerializedScriptableObject 제거, IMGUI CustomEditor 이관
 *
 * # 변경
 * - Sirenix SerializedScriptableObject 상속 제거 → ScriptableObject 단일 상속
 * - [InfoBox], [Button], [HideIf] Odin 어트리뷰트 제거
 * - CreateAsset() public 노출 → AssetDatabaseInstanceEditor에서 Reflection 호출
 *
 * # 추가
 * - IsLoadedFromAsset 프로퍼티 (에셋 존재 여부 판별, Editor에서 버튼 표시 분기용)
 *
 * =============================================================================
 */
#endif
