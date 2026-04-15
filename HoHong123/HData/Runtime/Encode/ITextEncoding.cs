#if UNITY_EDITOR
/* =========================================================
 * 텍스트 인코딩 / 디코딩 기능을 정의하는 인터페이스입니다.
 *
 * 목적 ::
 * byte[] 데이터를 문자열로 변환하거나 문자열을 byte[]로 복원하는 규격을 제공합니다.
 * =========================================================
 */
#endif

namespace HUtil.Encode {
    public interface ITextEncoding {
        string Encode(byte[] data);
        bool TryDecode(string text, out byte[] data);
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH 2026.03.10
 *
 * 주요 기능 ::
 * Encode
 * TryDecode
 *
 * 사용법 ::
 * Base64TextEncoding 등 구현체에서 사용됩니다.
 *
 * 기타 ::
 * 데이터 저장 시스템에서 공통 인코딩 인터페이스입니다.
 * =========================================================
 */
#endif