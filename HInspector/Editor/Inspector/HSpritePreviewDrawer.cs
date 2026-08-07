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
 * - Texture2D 전용 에셋용 Sprite.Create 결과는 경로당 1개만 만들어 캐시하고,
 *   어셈블리 리로드 직전 (AssemblyReloadEvents.beforeAssemblyReload) 에 일괄 파괴,
 *   원본 텍스처 리임포트 시에도 재생성
 * - 로드 실패는 캐시하지 않는다 — 경고만 별도 HashSet 으로 1회 억제
 * - foldoutStates 는 Selection.selectionChanged 마다 죽은 오브젝트 항목을 정리
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

        // key 로드가 실패했음을 기록해 경고를 1회로 억제한다. spriteCache 에는 실패를 올리지
        // 않는다 — null 을 캐시하면 이후 에셋을 추가해도 도메인 리로드 전까지 복구되지 않는다.
        static readonly HashSet<string> failedLoadKeys = new();

        // Texture2D 만 있는 에셋을 미리보기 하려고 Sprite.Create 로 만든 임시 스프라이트들.
        // 캐시가 없으면 리페인트마다 새 Sprite 가 생기고 아무도 Destroy 하지 않아 초당 수십 개가
        // 쌓인다. 에셋 경로당 1개만 만들고, 어셈블리 리로드 직전에 전부 파괴한다.
        static readonly Dictionary<string, Sprite> generatedSpriteCache = new();

        [InitializeOnLoadMethod]
        private static void _RegisterGeneratedSpriteCleanup() {
            AssemblyReloadEvents.beforeAssemblyReload -= _DestroyGeneratedSprites;
            AssemblyReloadEvents.beforeAssemblyReload += _DestroyGeneratedSprites;
            Selection.selectionChanged -= _PruneDeadFoldoutStates;
            Selection.selectionChanged += _PruneDeadFoldoutStates;
        }

        private static void _DestroyGeneratedSprites() {
            try {
                foreach (var generated in generatedSpriteCache.Values) {
                    try {
                        if (generated != null) Object.DestroyImmediate(generated);
                    }
                    catch (System.Exception e) {
                        // 개별 파괴 실패가 나머지 항목의 파괴·Clear 도달을 막지 않도록 격리.
                        HLogger.Error($"[HSpritePreview] Failed to destroy a generated sprite: {e}");
                    }
                }
            }
            finally {
                generatedSpriteCache.Clear();
            }
        }

        // foldoutStates 키는 "{instanceID}|{propertyPath}" 라 선택할 때마다 늘어나기만 하고
        // 제거 시점이 없었다(08 WST-1). 선택이 바뀔 때마다 더 이상 존재하지 않는 오브젝트의
        // 항목을 걷어내 세션 내내 단조 증가하는 것을 막는다.
        private static void _PruneDeadFoldoutStates() {
            if (foldoutStates.Count < 1) return;

            List<string> deadKeys = null;
            foreach (string key in foldoutStates.Keys) {
                int separatorIndex = key.IndexOf('|');
                if (separatorIndex <= 0) continue;
                if (!int.TryParse(key.Substring(0, separatorIndex), out int instanceId)) continue;
                if (instanceId != 0 && EditorUtility.InstanceIDToObject(instanceId) != null) continue;

                deadKeys ??= new List<string>();
                deadKeys.Add(key);
            }

            if (deadKeys == null) return;
            for (int k = 0; k < deadKeys.Count; k++) foldoutStates.Remove(deadKeys[k]);
        }

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

            if (sprite == null) {
                // 실패는 캐시하지 않는다(08 EDG-2) — null 을 spriteCache 에 올리면 키 오타를
                // 고치거나 에셋을 나중에 추가해도 도메인 리로드 전까지 복구되지 않는다.
                // 대신 경고만 1회로 억제한다.
                if (failedLoadKeys.Add(key))
                    HLogger.Warning($"[HSpritePreview] Sprite 로드 실패 — key: \"{key}\" (Resources 및 Addressables 모두 탐색)");
                return null;
            }

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

            // fake-null (파괴된 Unity 오브젝트) 을 그대로 돌려주지 않도록 == 비교로 확인한다.
            // texture 까지 함께 검사한다(08 EDG-3) — 원본 텍스처가 리임포트되면 Sprite 자신은
            // 살아있어도 sprite.texture 가 fake-null 이 되어 미리보기가 조용히 빈칸이 된다.
            if (generatedSpriteCache.TryGetValue(path, out var cachedGenerated)
                && cachedGenerated != null && cachedGenerated.texture != null) {
                return cachedGenerated;
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) return null;

            // 죽은 텍스처를 가리키던 이전 인스턴스를 재생성 전에 파괴한다 —
            // 그러지 않으면 캐시 교체 자체가 곧 누수다(이번 캐시 도입의 목적을 침해).
            if (cachedGenerated != null) Object.DestroyImmediate(cachedGenerated);

            var generated = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f));
            // 프로젝트에 저장되지 않는 임시 오브젝트임을 명시 — 씬/에셋 더티 유발 방지.
            generated.hideFlags = HideFlags.HideAndDontSave;
            generatedSpriteCache[path] = generated;
            return generated;
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
 * @Jason - PKH 2026.08.07 케이스 리포트 08 Medium 4건(WST-1·EDG-2·EDG-3·EXC-2) 반영
 *
 * # 변경
 * - EDG-2 : `_LoadFromKey` 가 로드 실패를 더 이상 `spriteCache` 에 캐시하지 않음.
 *   실패 키는 `failedLoadKeys` 로 옮겨 경고 1회 억제만 유지 — 이후 재시도 가능
 * - EDG-3 : `_LoadFromObject` 가 캐시된 generated Sprite 의 `texture != null` 까지 확인.
 *   리임포트로 죽은 텍스처를 가리키면 재생성 전 이전 인스턴스를 `DestroyImmediate`
 * - EXC-2 : `_DestroyGeneratedSprites` 개별 파괴를 try/catch 로 격리하고
 *   `generatedSpriteCache.Clear()` 를 `finally` 로 이동
 * - WST-1 : `Selection.selectionChanged` 구독 추가. 선택이 바뀔 때마다
 *   `foldoutStates` 에서 더 이상 존재하지 않는 오브젝트의 항목을 제거
 *
 * # 이유
 * - 근거: `Docs/Code/CaseReport/08_HInspector-에디터-캐시.md` 결함 의심 목록
 * - EDG-2 는 리포트 처방(성공 시에만 캐시 + 실패 키 별도 HashSet) 그대로 적용
 * - WST-1 은 완전한 LRU 대신 "선택 변경 시 죽은 항목 정리"로 최소 처리 —
 *   `EditorUtility.InstanceIDToObject` 로 판정하며 살아있는 항목은 그대로 유지
 *
 * # 주의
 * - 08 COR-2([HButton] Undo 계층 변경 미포착)·WST-2(표현식 파싱 negative 캐시)는
 *   각각 `HInspectorEditor.cs` / `HInspectorExpressionUtility.cs` 에서 별도 처리
 *
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
