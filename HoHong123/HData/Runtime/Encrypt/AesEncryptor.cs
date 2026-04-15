#if UNITY_EDITOR
/* =========================================================
 * AES 기반 데이터 암호화 / 복호화 유틸리티입니다.
 *
 * 특징 ::
 * AES CBC Mode
 * PKCS7 Padding 사용
 *
 * 암호화 구조 ::
 * payload = IV(16byte) + Cipher
 *
 * Key 생성 ::
 * pepper 문자열 > SHA256 > AES Key
 *
 * 주의사항 ::
 * 동일 pepper 문자열이 유지되어야 복호화 가능합니다.
 * =========================================================
 */
#endif

using System;
using System.Text;
using System.Security.Cryptography;

namespace HData.Encrypt {
    public sealed class AesEncryptor : IEncryptor {
        #region Fields
        readonly byte[] keyBytes;
        #endregion

        #region Public - Constructors
        public AesEncryptor(string pepper) {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsFalse(string.IsNullOrEmpty(pepper));
#endif
            keyBytes = _DeriveKeyBytes(pepper);
        }
        #endregion

        #region Public - Encrypt
        public byte[] Encrypt(byte[] plain) {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsNotNull(plain);
#endif

            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = keyBytes;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);
            var payload = new byte[aes.IV.Length + cipher.Length]; // IV(16) + CIPHER
            Buffer.BlockCopy(aes.IV, 0, payload, 0, aes.IV.Length);
            Buffer.BlockCopy(cipher, 0, payload, aes.IV.Length, cipher.Length);

            return payload;
        }
        #endregion

        #region Public - Decrypt
        public bool TryDecrypt(byte[] cipher, out byte[] plain) {
            plain = Array.Empty<byte>();
            if (cipher == null || cipher.Length <= 16) return false;

            var iv = new byte[16];
            Buffer.BlockCopy(cipher, 0, iv, 0, iv.Length);

            var realCipher = new byte[cipher.Length - iv.Length];
            Buffer.BlockCopy(cipher, iv.Length, realCipher, 0, realCipher.Length);

            try {
                using var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = keyBytes;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                plain = decryptor.TransformFinalBlock(realCipher, 0, realCipher.Length);

                return true;
            }
            catch {
                return false;
            }
        }
        #endregion

        #region Private - Derive Key
        private static byte[] _DeriveKeyBytes(string pepper) {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(pepper)); // 32 bytes
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * Encrypt
 *  + byte[] 데이터 AES 암호화
 * TryDecrypt
 *  + AES 복호화
 *
 * 사용법 ::
 * var encryptor = new AesEncryptor("pepper");
 * byte[] cipher = encryptor.Encrypt(data);
 *
 * 기타 ::
 * PlayerPrefs 또는 Local Save 데이터 보호용입니다.
 * =========================================================
 */
#endif