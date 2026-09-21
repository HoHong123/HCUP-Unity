#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 앞뒤 공백만 지우는 key 정규화. Addressables 주소와 섞인 로더 구성의 기본값.
 *
 * 주요 기능 ::
 * " Sprites/A.png " 를 "Sprites/A.png" 로 맞춘다. 확장자와 경로는 건드리지 않는다.
 *
 * 사용법 ::
 * AssetProviderFactory.CreateAddressable 과, 규칙을 넘기지 않은 AssetProviderFactory.Create 가 넣는다.
 *
 * 주의 ::
 * Addressables 주소는 임의 문자열이라 기본 주소 "Assets/Sprites/A.png" 처럼 확장자가 붙어 있다.
 * 확장자를 지우면 에셋을 찾지 못하므로 Resources 규칙을 쓰면 안 된다.
 * =========================================================
 */
#endif

namespace HResource.Load {
    public sealed class TrimKeyNormalizer : IAssetKeyNormalizer<string> {
        #region Public - Normalize
        public string Normalize(string key) {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;
            return key.Trim();
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-21 (최초 설계) :: AddressableAssetLoader 의 Trim 정규화를 provider 입구 규칙으로 분리
 *
 * 변경 ::
 * AddressableAssetLoader._NormalizeKey 의 Trim 을 그대로 옮겼다.
 *
 * 이유 ::
 * key 정규화를 provider 입구 한 곳으로 모으면서 Addressables 쪽 규칙도 같은 계약 아래로 옮겼다.
 * =========================================================
 */
#endif
