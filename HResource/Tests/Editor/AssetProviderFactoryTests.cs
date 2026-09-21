#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetProviderFactory.Create 가 규칙 없이 불렸을 때 로더에서 key 규칙을 알아내는지 확인하는 테스트.
 *
 * 주요 기능 ::
 * Resources 로더만 넘기면 Resources 규칙이 들어가 확장자 변형이 로드되고 한 칸을 공유한다.
 * Addressable 로더만 넘기면 Trim 규칙, 규칙을 직접 넘기면 추론을 건너뛰고 혼합도 조립된다.
 * 두 소스를 규칙 없이 섞으면 조립 시점에 ArgumentException 이 난다.
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
        const string PADDED_ADDRESS = " Sprites/A.png ";
        const string TRIMMED_ADDRESS = "Sprites/A.png";
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

        [Test]
        public void CreateWithOnlyAddressableLoaderInfersTheTrimRule() {
            var loader = new RecordingLoader(AssetLoadMode.Addressable);

            string received = _LoadThrough(AssetProviderFactory.Create(new IAssetLoader<string, TextAsset>[] { loader }),
                PADDED_ADDRESS, AssetLoadMode.Addressable, loader);

            // Trim 은 공백만 지우고 확장자는 남긴다. Resources 규칙이었다면 확장자가 사라진다.
            Assert.AreEqual(TRIMMED_ADDRESS, received);
        }

        [Test]
        public void CreateWithAnExplicitRuleSkipsInference() {
            var loader = new RecordingLoader(AssetLoadMode.Resources);

            string received = _LoadThrough(
                AssetProviderFactory.Create(new IAssetLoader<string, TextAsset>[] { loader }, keyNormalizer: new FixedKeyNormalizer()),
                PADDED_ADDRESS, AssetLoadMode.Resources, loader);

            Assert.AreEqual(FixedKeyNormalizer.RESULT, received);
        }

        [Test]
        public void CreateWithAnExplicitRuleAllowsMixedSources() {
            IAssetSource<string, TextAsset> provider = null;

            // 규칙을 직접 넘기면 혼합도 조립된다. 거부 경로를 타면 HLogger.Throw 가 에러 로그를 남겨 이 테스트가 실패한다.
            Assert.DoesNotThrow(() => provider = AssetProviderFactory.Create(new IAssetLoader<string, TextAsset>[] {
                new RecordingLoader(AssetLoadMode.Resources),
                new RecordingLoader(AssetLoadMode.Addressable),
            }, keyNormalizer: new FixedKeyNormalizer()));

            provider.Dispose();
        }
        #endregion

        #region Private - Helpers
        // 가짜 로더는 즉시 완료되고 게이트는 합류자를 동기로 깨우므로 GetAsync 가 이 호출 안에서 끝난다.
        static string _LoadThrough(IAssetSource<string, TextAsset> provider, string key, AssetLoadMode loadMode, RecordingLoader loader) {
            var owner = new GameObject("FactoryRuleOwner");
            try {
                provider.GetAsync(owner.transform, key, loadMode).GetAwaiter().GetResult();
                return loader.LastKey;
            }
            finally {
                provider.Dispose();
                Object.DestroyImmediate(owner);
            }
        }

        // provider 가 로더에 넘긴 key 를 기록한다. 에셋은 돌려주지 않아 캐시에 아무것도 남지 않는다.
        sealed class RecordingLoader : IAssetLoader<string, TextAsset> {
            public RecordingLoader(AssetLoadMode loadMode) {
                LoadMode = loadMode;
            }

            public AssetLoadMode LoadMode { get; }
            public string LastKey { get; private set; }

            public UniTask<TextAsset> LoadAsync(string key) {
                LastKey = key;
                return UniTask.FromResult<TextAsset>(null);
            }
        }

        // 어떤 key 든 고정값으로 바꾼다. 로더가 이 값을 받으면 추론이 아니라 이 규칙이 쓰인 것이다.
        sealed class FixedKeyNormalizer : IAssetKeyNormalizer<string> {
            public const string RESULT = "explicit-rule-result";

            public string Normalize(string key) => RESULT;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-22 (수정) :: 추론 규칙의 나머지 절반 테스트 3건
 *
 * 변경 ::
 * CreateWithOnlyAddressableLoaderInfersTheTrimRule / CreateWithAnExplicitRuleSkipsInference /
 * CreateWithAnExplicitRuleAllowsMixedSources 추가. 받은 key 를 기록하는 RecordingLoader 와 고정값을 돌려주는
 * FixedKeyNormalizer 로 provider 가 로더에 넘긴 key 를 직접 본다. 에셋이 필요 없다.
 *
 * 이유 ::
 * 리뷰 지적. Resources 만 넘긴 경우와 혼합 거부만 테스트가 있었고, Addressable 만 넘긴 경우의 Trim 선택과
 * 규칙을 직접 넘겼을 때의 추론 생략(혼합 허용 포함)은 검증이 없었다.
 *
 * 결과 ::
 * HCUP.HResource.Tests 33/33 통과 (2026-09-21 15:44 UTC). 돌연변이 두 개를 함께 넣자(Addressable 만일 때 Resources 규칙 반환,
 * 직접 넘긴 규칙 무시) 새 3건이 모두 실패했다. Trim 테스트는 "Sprites/A.png" 대신 " Sprites/A", 직접 규칙 테스트는
 * "explicit-rule-result" 대신 " Sprites/A", 혼합 허용 테스트는 ArgumentException. 되돌린 뒤 팩토리 코드는 master 와 같다.
 *
 * =========================================================
 * 2026-09-22 (검증) :: 최초 두 건의 실행 결과와 돌연변이 확인 기록
 *
 * 변경 ::
 * 코드 변경 없음. 아래 (최초 설계) 항목을 만든 브랜치 dev/hong/fix/hresource-normalizer-inference 에서의 실행 기록이다.
 *
 * 결과 ::
 * 1) HCUP.HResource.Tests 30/30 통과 (2026-09-21 15:31 UTC, Unity 6000.3.18f1 batchmode -runTests). 기존 28 + 이 파일 2.
 * 2) 돌연변이 확인. Create 의 기본 규칙을 _InferKeyNormalizer 대신 new TrimKeyNormalizer() 로 바꾸자 이 파일 두 건이
 *    모두 실패했다. CreateWithOnlyResourcesLoaderInfersTheResourcesRule 은 "Expected: not null But was: null",
 *    CreateRejectsMixedSourcesWithoutAnExplicitRule 은 "Expected: <System.ArgumentException> But was: null". 되돌린 뒤 30/30.
 *
 * 주의 ::
 * 리뷰 지적. 이 결과를 병합 시점에 채팅으로만 보고하고 여기 적지 않았다. 2026-09-21 게이트 테스트와 같은 누락이다.
 *
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
