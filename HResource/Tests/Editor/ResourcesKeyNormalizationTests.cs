#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Resources key 정규화가 provider 한 개 안에서 캐시 · 게이트와 로더 표를 어긋나게 하는지 재현하는 테스트.
 *
 * 주요 기능 ::
 * 확장자만 다른 두 key 로 같은 에셋을 얻고, 한쪽 반납 뒤 로더 표에 다른 쪽 항목이 남는지 본다.
 *
 * 사용법 ::
 * Test Runner 의 EditMode 탭. 에셋은 같은 폴더의 Resources/HResourceTests/KeyProbe.txt 하나다.
 * Editor 폴더 아래 Resources 라 에디터에서만 로드되고 빌드에는 들어가지 않는다.
 *
 * 주의 ::
 * 현재 동작(결함)을 고정한다. 캐시 · 게이트는 원본 key, 로더 표는 정규화 key 를 쓴다.
 * 정규화 위임이나 참조 카운트로 고치면 마지막 단정이 뒤집히므로 그때 이 테스트를 함께 고친다.
 * =========================================================
 */
#endif

using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HResource.Data;
using HResource.Load;
using HResource.Provider;

namespace HResource.Tests {
    public sealed class ResourcesKeyNormalizationTests {
        #region Fields
        const string PROBE_KEY = "HResourceTests/KeyProbe";
        const string PROBE_KEY_WITH_EXTENSION = "HResourceTests/KeyProbe.txt";
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator ExtensionVariantsShareOneLoaderEntry() => UniTask.ToCoroutine(async () => {
            var loader = new ResourcesAssetLoader<TextAsset>();
            IAssetSource<string, TextAsset> provider = AssetProviderFactory.Create(new IAssetLoader<string, TextAsset>[] { loader });
            var ownerA = new GameObject("KeyProbeOwnerA");
            var ownerB = new GameObject("KeyProbeOwnerB");

            try {
                TextAsset first = await provider.GetAsync(ownerA.transform, PROBE_KEY, AssetLoadMode.Resources);
                TextAsset second = await provider.GetAsync(ownerB.transform, PROBE_KEY_WITH_EXTENSION, AssetLoadMode.Resources);

                // 같은 에셋인데 캐시 항목은 원본 key 마다 하나씩 생긴다.
                Assert.IsNotNull(first, "The probe asset did not load. Check Resources/HResourceTests/KeyProbe.txt.");
                Assert.AreSame(first, second);
                Assert.IsTrue(provider.TryGet(PROBE_KEY, out _));
                Assert.IsTrue(provider.TryGet(PROBE_KEY_WITH_EXTENSION, out _));

                Assert.IsTrue(provider.Release(ownerA.transform, PROBE_KEY));

                // B 의 캐시 항목은 살아 있는데, 로더 표의 유일한 항목은 A 의 반납이 이미 가져갔다.
                // B 가 반납될 때 로더는 되돌릴 것이 없어 언로드가 일어나지 않는다.
                Assert.IsTrue(provider.TryGet(PROBE_KEY_WITH_EXTENSION, out _));
                Assert.IsFalse(loader.Release(PROBE_KEY_WITH_EXTENSION));
            }
            finally {
                provider.Dispose();
                Object.DestroyImmediate(ownerA);
                Object.DestroyImmediate(ownerB);
            }
        });
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-21 (최초 설계) :: Resources key 정규화 불일치 재현
 *
 * 변경 ::
 * ExtensionVariantsShareOneLoaderEntry 테스트와 KeyProbe.txt 에셋 추가.
 *
 * 이유 ::
 * 리뷰 지적(추론). ResourcesAssetLoader 의 loadedTable 은 정규화 key, provider 의 캐시와 게이트는 원본 key 를 쓴다.
 * "Icon/A" 와 "Icon/A.png" 는 캐시 항목 2개, 로더 표 1개가 된다. 추론을 실행으로 확인하기 위해 만들었다.
 *
 * 결과 ::
 * 재현됐다 (2026-09-21, master 566eca6 기준 batchmode EditMode). 같은 에셋 인스턴스에 캐시 항목 2개가 생기고,
 * A 반납 뒤 B 의 캐시 항목은 남았는데 loader.Release(B) 가 false 다. 로더 표의 유일한 항목을 A 가 가져갔다.
 * 이 테스트는 현재 동작을 고정한 채 통과한다. 수정 방향(정규화 위임 / 참조 카운트)은 미정.
 *
 * 주의 ::
 * UnloadAsset 뒤에도 참조되는 에셋은 Unity 가 디스크에서 다시 읽으므로 피해는 깨진 에셋이 아니라
 * 불필요한 언로드 · 재로드와, 두 번째 반납 때 언로드가 빠지는 것이다.
 * =========================================================
 */
#endif
