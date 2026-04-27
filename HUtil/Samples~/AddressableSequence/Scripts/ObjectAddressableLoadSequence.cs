#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * UnityEngine.Object 기반 Addressables 범용 로드 시퀀스입니다.
 *
 * 주의사항 ::
 * 1. Addressables에 등록된 에셋 자체를 테스트하기 위한 용도입니다.
 * 2. Prefab 내부 특정 컴포넌트나 Scene Instance를 직접 로드하는 용도가 아닙니다.
 * =========================================================
 */
#endif

using HUtil.Data.Sequence;

namespace HUtil.Sample.Addressable {
    public sealed class ObjectAddressableLoadSequence : AddressableLoadSequence<UnityEngine.Object> {
    }
}