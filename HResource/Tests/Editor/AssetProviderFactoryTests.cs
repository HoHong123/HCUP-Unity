#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetProviderFactory.Create 가 규칙 없이 불렸을 때 로더에서 key 규칙을 알아내는지 확인하는 테스트.
 *
 * 주요 기능 ::
 * Resources 로더만 넘기면 Resources 규칙이 들어가 확장자 변형이 로드되고 한 칸을 공유한다.
 * 두 소스를 섞으면 조립 시점에 ArgumentException 이 난다.
 *
 * 사용법 ::
 * Test Runner 의 EditMode 탭. 에셋은 Resources/HResourceTests/KeyProbe.txt 하나다.
 *
 * 주의 ::
 * 혼합 거부는 HLogger.Throw 가 던지기 전에 Debug.LogError 를 남긴다. LogAssert.Expect 를 지우면 올바른 동작도 실패로 잡힌다.
 * =========================================================
 */
#endif

using System;
using System.Collections;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HResource.Data;
using HResource.Load;
using HResource.Provider;
using Object = UnityEngine.Object;

namespace HResource.Tests {
    public sealed class AssetProviderFactoryTests {
        #region Fields
        const string PROBE_KEY = "HResourceTests/KeyProbe";
        const string PROBE_KEY_WITH_EXTENSION = "HResourceTests/KeyProbe.txt";
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator CreateWithOnlyResourcesLoaderInfersTheResourcesRule() => UniTask.ToCoroutine(async () => {
            IAssetSource<string, TextAsset> provider = AssetProviderFactory.Create(
                new IAssetLoader<string, TextAsset>[] { new ResourcesAssetLoader<TextAsset>() });
            var owner = new GameObject("FactoryProbeOwner");

            try {
                // 규칙이 Trim 이었다면 확장자째 Resources.LoadAsync 로 들어가 null 이 된다.
                TextAsset asset = await provider.GetAsync(owner.transform, PROBE_KEY_WITH_EXTENSION, AssetLoadMode.Resources);

                Assert.IsNotNull(asset, "Create with only a Resources loader did not strip the extension.");
                Assert.IsTrue(provider.TryGet(PROBE_KEY, out var viaOtherSpelling));
                Assert.AreSame(asset, viaOtherSpelling);
            }
            finally {
                provider.Dispose();
                Object.DestroyImmediate(owner);
            }
        });

        [Test]
        public void CreateRejectsMixedSourcesWithoutAnExplicitRule() {
            LogAssert.Expect(LogType.Error, new Regex("cannot share one provider"));

            Assert.Throws<ArgumentException>(() => AssetProviderFactory.Create(new IAssetLoader<string, TextAsset>[] {
                new ResourcesAssetLoader<TextAsset>(),
                new AddressableAssetLoader<TextAsset>(),
            }));
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-22 (최초 설계) :: Create 의 key 규칙 추론 테스트
 *
 * 변경 ::
 * 규칙 없는 Create(Resources 로더) 의 확장자 로드 · 한 칸 공유, 두 소스 혼합 거부 두 건.
 *
 * 이유 ::
 * 리뷰 지적 (P1 두 건). 2026-09-21 변경 뒤 규칙 없는 Create 가 Trim 을 받아 Resources 확장자 요청이 실패하는
 * 회귀가 있었고, 혼합 provider 는 어떤 규칙으로도 한쪽이 틀린데 막는 장치가 없었다.
 * =========================================================
 */
#endif
