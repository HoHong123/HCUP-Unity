#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueLineNode.LocalizationUID 기반 로컬라이즈 텍스트에서 portrait.* 인라인 이벤트를 추출.
 *
 * 특징 / 지원기능 ::
 * + Build(node, registry) — AssetDatabase → 부모 카탈로그 조회 → 로컬라이즈 텍스트 파싱 → PortraitEventInstruction 목록
 * + ResolveSprite(ins, registry) — Pose/Show 동사의 포즈 키 → SpriteKey Addressables 로드 (WaitForCompletion, static 캐시)
 * + 로컬라이즈 텍스트 해시 캐시 — 동일 텍스트 재파싱 방지 (GC 최소화)
 *
 * 주의사항 ::
 * editorLocalizationSO 미연결 시 Build는 빈 목록 반환 (포트레이트 스트립 미표시).
 * 텍스트 해시 충돌 시 잘못된 미리보기 가능 (에디터 전용, 런타임 영향 없음).
 * registry null → ResolveSprite 는 항상 null 반환.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HDialogue.Editor {
    public static class DialogueLinePortraitTimelineBuilder {
        #region Cache
        static readonly Dictionary<int, IReadOnlyList<PortraitEventInstruction>> cache = new();
        static readonly Dictionary<string, Sprite> spriteCache = new();
        static readonly Dictionary<string, AsyncOperationHandle<Sprite>> addrHandles = new();
        #endregion

        #region Public
        public static IReadOnlyList<PortraitEventInstruction> Build(
            DialogueLineNode node, CharacterRegistrySO registry) {
            if (node == null || string.IsNullOrEmpty(node.LocalizationUID))
                return Array.Empty<PortraitEventInstruction>();

            string assetPath = AssetDatabase.GetAssetPath(node);
            if (string.IsNullOrEmpty(assetPath))
                return Array.Empty<PortraitEventInstruction>();

            DialogueCatalogSO catalog = AssetDatabase.LoadAssetAtPath<DialogueCatalogSO>(assetPath);
            if (catalog == null || !catalog.EditorTryGetLocalizedText(node.LocalizationUID, out string rawText))
                return Array.Empty<PortraitEventInstruction>();

            int hash = rawText.GetHashCode();
            if (cache.TryGetValue(hash, out IReadOnlyList<PortraitEventInstruction> cached))
                return cached;

            IReadOnlyList<DialogueToken> tokens = DialogueTagParser.Parse(rawText);
            List<PortraitEventInstruction> result = null;
            for (int k = 0; k < tokens.Count; k++) {
                DialogueToken token = tokens[k];
                if (token.Type != DialogueTokenType.Event) continue;
                if (!PortraitEventParser.TryParse(token.StringArg, out PortraitEventInstruction ins)) continue;
                if (result == null) result = new List<PortraitEventInstruction>();
                result.Add(ins);
            }

            IReadOnlyList<PortraitEventInstruction> ro = result != null
                ? (IReadOnlyList<PortraitEventInstruction>)result.AsReadOnly()
                : Array.Empty<PortraitEventInstruction>();
            cache[hash] = ro;
            return ro;
        }

        public static Sprite ResolveSprite(PortraitEventInstruction ins, CharacterRegistrySO registry) {
            if (registry == null) return null;
            if (ins.Verb != PortraitVerb.Pose && ins.Verb != PortraitVerb.Show) return null;
            if (string.IsNullOrEmpty(ins.TargetCharacterKey) || ins.Args.Length == 0) return null;
            if (!registry.TryGet(ins.TargetCharacterKey, out CharacterPortraitSetSO set)) return null;
            if (!set.TryGetPose(ins.Args[0], out PortraitPose pose)) return null;
            return _LoadSprite(pose.SpriteKey);
        }
        #endregion

        #region Private
        private static Sprite _LoadSprite(string key) {
            if (string.IsNullOrEmpty(key)) return null;
            if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;
            try {
                AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(key);
                Sprite sprite = handle.WaitForCompletion();
                if (handle.Status == AsyncOperationStatus.Succeeded && sprite != null) {
                    addrHandles[key] = handle;
                    spriteCache[key] = sprite;
                    return sprite;
                }
                Addressables.Release(handle);
            } catch (System.Exception e) {
                // 미리보기 실패는 치명적이지 않지만 무음이면 키 오타를 못 찾는다 — null 캐시라 키당 1회만 출력된다.
                UnityEngine.Debug.LogWarning($"[DialogueLinePortraitTimelineBuilder] Preview sprite load failed. key='{key}' :: {e.Message}");
            }
            spriteCache[key] = null;
            return null;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: PortraitPose.Sprite → SpriteKey Addressables 로드
 *
 * # 변경
 * - ResolveSprite(): `pose.Sprite` → `_LoadSprite(pose.SpriteKey)`.
 * - _LoadSprite(key): Addressables.LoadAssetAsync<Sprite>.WaitForCompletion 동기 로드 추가.
 * - spriteCache / addrHandles 정적 캐시 추가 (에디터 리페인트 중복 로드 방지).
 * - using UnityEngine.AddressableAssets / ResourceManagement.AsyncOperations 추가.
 *
 * # 이유
 * - PortraitPose.Sprite(직접 참조) → SpriteKey(Addressable 키) 전환에 따른 컴파일 에러 수정.
 * - 에디터 전용 동기 로드(WaitForCompletion): 에디터 GUI 스레드에서 사용하므로 허용.
 *   런타임은 CharacterPortraitController._LoadAndSetSpriteAsync(UniTask async) 경로 사용.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: RawText → LocalizationUID 로컬라이즈 텍스트 연동
 *
 * # 변경
 * - Build(): `node.RawText` → `node.LocalizationUID` 참조.
 * - AssetDatabase.GetAssetPath(node) → LoadAssetAtPath<DialogueCatalogSO> → EditorTryGetLocalizedText 경로 추가.
 *   로컬라이즈 텍스트 조회 실패(SO 미연결 / UID 없음) 시 Array.Empty 반환.
 * - 해시 캐시 키 기준: node.RawText → 조회된 rawText.
 * - using UnityEditor 추가.
 *
 * # 이유
 * - DialogueLineNode.rawText 제거(→ LocalizationUID) 에 따른 컴파일 에러 수정.
 * - portrait 이벤트 태그는 로컬라이즈 텍스트 본문에 있으므로 UID만으로는 파싱 불가.
 *   DialogueLineNodePreviewDrawer._BuildTextPreview와 동일한 카탈로그 조회 패턴 적용.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueLinePortraitTimelineBuilder 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 5 — DialogueLineNode 바디 포트레이트 타임라인 스트립 데이터 빌더
 *
 * # 설계 결정
 * - DialogueTagParser.Parse 재사용: rawText → Event 토큰만 필터 → PortraitEventParser.TryParse
 * - rawText.GetHashCode() 캐시: 에디터 반복 리페인트에서 재파싱 방지
 * - 지연 List 할당: portrait 이벤트 없는 라인은 result == null 유지 → Array.Empty 반환
 *
 * =============================================================================
 */
#endif
