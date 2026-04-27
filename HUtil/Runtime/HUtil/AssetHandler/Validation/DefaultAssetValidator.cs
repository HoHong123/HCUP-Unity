#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 기본 asset validator 구현. 도메인별 세부 규칙은 포함하지 않고 최소 유효성만 검사.
 *
 * 주요 기능 ::
 * CanLoad — string key 의 IsNullOrWhiteSpace + 일반 참조형 null 검사.
 * IsValid — UnityEngine.Object null 함정 (== null operator overload) + 일반 참조형 null 분리 검사.
 *
 * 사용법 ::
 * AssetProviderFactory.Create 의 기본 validator 로 자동 주입. 도메인별 추가 규칙 (예: GUID
 * 형식 강제) 이 필요하면 IAssetValidator 별도 구현체로 교체.
 *
 * 주의 ::
 * Unity Object 의 == null 은 destroyed instance 에 대해 ReferenceEquals 와 다른 결과를 반환
 * (Unity 의 native object lifecycle 상호작용). 본 validator 는 두 규칙을 명시적으로 분리하여
 * Unity Object 와 일반 참조형 모두 안전하게 검증.
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUtil.AssetHandler.Validation {
    public sealed class DefaultAssetValidator<TKey, TAsset> : IAssetValidator<TKey, TAsset> {
        #region Public - Validate
        public bool CanLoad(TKey key) {
            if (key is string stringKey) {
                return !string.IsNullOrWhiteSpace(stringKey);
            }

            if (ReferenceEquals(key, null)) {
                return false;
            }

            return true;
        }

        public bool IsValid(TKey key, TAsset asset) {
            if (!CanLoad(key)) return false;

            if (asset is Object unityObject) {
                return unityObject != null;
            }

            if (ReferenceEquals(asset, null)) {
                return false;
            }

            return true;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 *
 * =========================================================
 * 2026-04-26 (수정) :: 헤더 형틀 통합 + Dev Log 형식 도입
 * =========================================================
 * 변경 ::
 * 기존 헤더 (상단 도입+주의사항 + 하단 주요기능/사용법/이벤트/기타) 를 한 곳에 통합하여
 * §11 형틀 통일. 하단 Dev Log 영역 추가. 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드.
 *
 * 이유 ::
 * 글로벌 CLAUDE.md §11 룰 일괄 적용.
 *
 * =========================================================
 * 2026-04-25 (최초 설계) :: DefaultAssetValidator 초기 구현
 * =========================================================
 * Unity Object 의 == null operator overload 함정을 명시적으로 처리하는 게 핵심 — destroyed
 * Unity 객체는 ReferenceEquals 로는 null 이 아니지만 == 로는 null 로 판정됨. 본 validator 는
 * `asset is Object unityObject` 로 Unity Object 분리 후 == 사용, 일반 참조형은 ReferenceEquals
 * 사용. checker 가 아니라 validator — 정책 판단보다 입력/결과의 최소 유효성만 검증.
 * =========================================================
 */
#endif
