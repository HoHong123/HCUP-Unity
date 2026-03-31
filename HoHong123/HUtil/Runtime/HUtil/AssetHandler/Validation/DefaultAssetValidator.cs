using UnityEngine;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 기본 asset validator 구현 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 도메인별 세부 규칙은 포함하지 않고 최소 유효성만 검사합니다.
 * 2. UnityEngine.Object null 규칙과 일반 참조형 null 규칙을 구분합니다.
 * =========================================================
 */
#endif

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
 * @Jason - PKH
 * 주요 기능 ::
 * 1. key의 로드 가능 여부를 검사합니다.
 * 2. 로드 결과 asset의 최소 유효성을 검사합니다.
 *
 * 사용법 ::
 * 1. 기본 provider 조합에서 기본 validator로 사용합니다.
 * 2. 추가 도메인 규칙이 필요하면 별도 validator 구현으로 교체합니다.
 *
 * 이벤트 ::
 * 1. 직접 이벤트는 없습니다.
 * 2. 검사 결과에 따라 provider가 다음 흐름을 결정합니다.
 *
 * 기타 ::
 * 1. checker가 아니라 validator 역할에 맞춘 구현입니다.
 * 2. 정책 판단보다 최소 입력 검증에 집중합니다.
 * =========================================================
 */
#endif
