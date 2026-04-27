#if UNITY_EDITOR
/* =========================================================
 * 데이터 암호화 / 복호화 기능을 정의하는 인터페이스입니다.
 *
 * 목적 ::
 * 암호화 알고리즘 구현을 추상화하기 위함입니다.
 * =========================================================
 */
#endif

namespace HData.Encrypt {
    public interface IEncryptor {
        byte[] Encrypt(byte[] plain);
        bool TryDecrypt(byte[] cipher, out byte[] plain);
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 사용법 ::
 * AesEncryptor 등 구현체에서 사용됩니다.
 * =========================================================
 */
#endif