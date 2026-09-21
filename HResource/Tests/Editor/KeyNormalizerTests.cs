#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * ResourcesKeyNormalizer / TrimKeyNormalizer 의 규칙을 고정하는 EditMode 테스트.
 *
 * 주요 기능 ::
 * 확장자 · 선행 슬래시 · 역슬래시 · rootPath 결합 · 경로 경계 · 공백 처리를 표 형태로 확인한다.
 *
 * 사용법 ::
 * Test Runner 의 EditMode 탭. 에셋이 필요 없다.
 *
 * 주의 ::
 * ResourcesKeyNormalizerIsNotIdempotent 는 "두 번 정규화하면 다른 에셋이 된다" 를 고정한다.
 * 로더에 정규화를 다시 넣으면 안 되는 이유다. 이 테스트를 지우지 말 것.
 * =========================================================
 */
#endif

using NUnit.Framework;
using HResource.Load;

namespace HResource.Tests {
    public sealed class KeyNormalizerTests {
        #region Tests - Resources
        [TestCase("", "", "")]
        [TestCase("   ", "", "")]
        [TestCase("Icon/A", "", "Icon/A")]
        [TestCase("Icon/A.png", "", "Icon/A")]
        [TestCase("/Icon/A.png", "", "Icon/A")]
        [TestCase("Icon\\A.png", "", "Icon/A")]
        [TestCase("Icon/A", "Root", "Root/Icon/A")]
        [TestCase("Root/Icon/A.png", "Root", "Root/Icon/A")]
        [TestCase("A", "/Root/", "Root/A")]
        [TestCase("IconSet/A", "Icon", "Icon/IconSet/A")]
        [TestCase("Icon", "Icon", "Icon")]
        public void ResourcesKeyNormalizerMapsSpellingsToOneKey(string key, string rootPath, string expected) {
            var normalizer = new ResourcesKeyNormalizer(rootPath);

            Assert.AreEqual(expected, normalizer.Normalize(key));
        }

        [Test]
        public void ResourcesKeyNormalizerIsNotIdempotent() {
            var normalizer = new ResourcesKeyNormalizer(string.Empty);

            string once = normalizer.Normalize("Icon/foo.v2.png");
            string twice = normalizer.Normalize(once);

            Assert.AreEqual("Icon/foo.v2", once);
            Assert.AreEqual("Icon/foo", twice, "If this ever holds again the loader may normalize too; until then it must not.");
        }
        #endregion

        #region Tests - Trim
        [TestCase("", "")]
        [TestCase("   ", "")]
        [TestCase(" Sprites/A.png ", "Sprites/A.png")]
        [TestCase("Assets/Sprites/A.png", "Assets/Sprites/A.png")]
        public void TrimKeyNormalizerKeepsExtensionsAndPaths(string key, string expected) {
            var normalizer = new TrimKeyNormalizer();

            Assert.AreEqual(expected, normalizer.Normalize(key));
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-21 (최초 설계) :: key 정규화 규칙 테스트
 *
 * 변경 ::
 * ResourcesKeyNormalizer 11 케이스 + 비멱등 1 건, TrimKeyNormalizer 4 케이스.
 *
 * 이유 ::
 * key 정규화를 로더에서 provider 입구로 옮기며 규칙이 별도 클래스가 됐다. 옮긴 규칙이 그대로인지와,
 * rootPath 경계 검사(2026-08-06 교정)가 살아 있는지를 고정한다.
 * =========================================================
 */
#endif
