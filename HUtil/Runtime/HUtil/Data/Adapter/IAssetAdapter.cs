#if UNITY_EDITOR
/* =========================================================
 * Unity Asset을 Runtime 데이터로 변환하기 위한 Adapter 인터페이스입니다.
 * Asset과 Runtime Data 구조를 분리하기 위한 변환 계층을 제공합니다.
 *
 * 주의사항 ::
 * 1. Convert는 Asset을 Runtime Data 객체로 변환합니다.
 * 2. Asset 타입은 반드시 UnityEngine.Object 기반이어야 합니다.
 * =========================================================
 */
#endif

namespace HUtil.Data.Adapter {
    public interface IAssetAdapter<in TAsset, out TResult>
        where TAsset : UnityEngine.Object {
        TResult Convert(TAsset asset);
    }
}