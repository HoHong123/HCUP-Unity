#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * provider 입구 key 정규화가 확장자 변형을 캐시 한 칸 · 로더 기록 하나로 합치는지 확인하는 테스트.
 *
 * 주요 기능 ::
 * 확장자만 다른 두 key 로 같은 에셋을 얻고, 어느 표기로 반납해도 같은 칸을 찾으며 마지막 반납에서만 칸이 지워지는지 본다.
 *
 * 사용법 ::
 * Test Runner 의 EditMode 탭. 에셋은 같은 폴더의 Resources/HResourceTests/KeyProbe.txt 하나다.
 * Editor 폴더 아래 Resources 라 에디터에서만 로드되고 빌드에는 들어가지 않는다 (2026-09-21 정식 빌드로 확인).
 *
 * 주의 ::
 * 2026-09-21 이전에는 이 파일이 결함(캐시 2칸 · 로더 1칸)을 고정하는 KnownIssue 테스트였다. 이력은 Dev Log.
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
        public IEnumerator ExtensionVariantsShareOneCacheEntryUntilTheLastRelease() => UniTask.ToCoroutine(async () => {
            var loader = new ResourcesAssetLoader<TextAsset>();
            IAssetSource<string, TextAsset> provider = AssetProviderFactory.Create(
                new IAssetLoader<string, TextAsset>[] { loader },
                keyNormalizer: new ResourcesKeyNormalizer(string.Empty));
            var ownerA = new GameObject("KeyProbeOwnerA");
            var ownerB = new GameObject("KeyProbeOwnerB");

            try {
                TextAsset first = await provider.GetAsync(ownerA.transform, PROBE_KEY, AssetLoadMode.Resources);
                TextAsset second = await provider.GetAsync(ownerB.transform, PROBE_KEY_WITH_EXTENSION, AssetLoadMode.Resources);

                Assert.IsNotNull(first, "The probe asset did not load. Check Resources/HResourceTests/KeyProbe.txt.");
                Assert.AreSame(first, second);

                // A 는 확장자 없이 얻었지만 확장자 있는 표기로 반납한다. Release 의 반환값은 "칸을 지웠는가" 라
                // B 가 남아 있는 지금은 false 다. 표기가 달라 칸을 못 찾았어도 false 이므로, 구분은 아래 두 줄이 한다.
                Assert.IsFalse(provider.Release(ownerA.transform, PROBE_KEY_WITH_EXTENSION));
                Assert.IsTrue(provider.TryGet(PROBE_KEY, out _));

                // 마지막 점유가 빠지면 칸이 지워지고, 반납 연쇄가 로더 기록을 소비한다.
                // 위에서 A 의 반납이 칸을 못 찾았다면 A 가 여전히 점유 중이라 여기서 칸이 남는다.
                Assert.IsTrue(provider.Release(ownerB.transform, PROBE_KEY));
                Assert.IsFalse(provider.TryGet(PROBE_KEY_WITH_EXTENSION, out _));
                Assert.IsFalse(loader.Release(PROBE_KEY));
            }
            finally {
                provider.Dispose();
                Object.DestroyImmediate(ownerA);
                Object.DestroyImmediate(ownerB);
            }
        });

        [UnityTest]
        public IEnumerator CreateResourcesJoinsExtensionVariants() => UniTask.ToCoroutine(async () => {
            IAssetSource<string, TextAsset> provider = AssetProviderFactory.CreateResources<TextAsset>(string.Empty);
            var owner = new GameObject("KeyProbeOwner");

            try {
                TextAsset asset = await provider.GetAsync(owner.transform, PROBE_KEY_WITH_EXTENSION, AssetLoadMode.Resources);

                Assert.IsNotNull(asset, "The probe asset did not load. Check Resources/HResourceTests/KeyProbe.txt.");
                Assert.IsTrue(provider.TryGet(PROBE_KEY, out var viaOtherSpelling));
                Assert.AreSame(asset, viaOtherSpelling);
            }
            finally {
                provider.Dispose();
                Object.DestroyImmediate(owner);
            }
        });
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-21 (수정 2) :: provider 입구 정규화로 결함을 고치고 테스트를 정상 동작 검증으로 뒤집음
 *
 * 변경 ::
 * KnownIssue_ExtensionVariantsShareOneLoaderEntry 를 ExtensionVariantsShareOneCacheEntryUntilTheLastRelease 로 바꾸고
 * KnownIssue 분류를 뗐다. 단정은 "캐시 한 칸 · 표기와 무관한 반납 · 마지막 반납에서만 칸 삭제 · 로더 기록 소비" 다.
 * 실제 사용 경로 CreateResources 를 확인하는 CreateResourcesJoinsExtensionVariants 를 추가했다.
 *
 * 이유 ::
 * 사용자가 (a1) provider 단위 정규화기를 택했다. 확장자 변형이 입구에서 같은 key 가 되어 결함이 사라졌다.
 *
 * 결과 ::
 * HCUP.HResource.Tests 28/28 통과 (2026-09-21 14:58 UTC, 게이트 10 · 정규화 통합 2 · 규칙 단위 16).
 * 돌연변이 확인: AssetProvider.Release 입구에서만 정규화를 빼자 이 테스트가 59 행(B 의 반납이 칸을 지워야 함)에서
 * 실패했다. 확장자 표기로 한 A 의 반납이 칸을 못 찾아 A 가 남았기 때문이다. 되돌린 뒤 28/28.
 *
 * 주의 ::
 * Release 의 반환값은 "칸을 지웠는가" 다. 다른 소유자가 남은 반납은 정상이어도 false 라, 처음에 true 로 단정해
 * 한 번 실패했다. 정규화 성공 여부는 반환값이 아니라 마지막 반납 뒤의 칸 상태로 판정한다.
 *
 * =========================================================
 * 2026-09-21 (수정) :: 결함 고정 테스트를 KnownIssue 로 분류
 *
 * 변경 ::
 * 테스트 이름에 KnownIssue_ 접두사를 붙이고 [Category("KnownIssue")] 를 달았다.
 *
 * 이유 ::
 * 리뷰 권고. 결함이 있어야 통과하는 테스트가 중립적인 이름으로 "11/11 통과" 에 섞여 결함이 없는 것처럼 읽혔다.
 * HResource 테스트 집계는 "정상 동작 10개 통과 + 알려진 결함 재현 1개" 가 정확하다.
 *
 * 결과 ::
 * Test Runner 에서 Category 로 걸러 볼 수 있다. 정규화를 고치면 접두사와 Category 를 떼고 단정을 뒤집는다.
 *
 * 주의 ::
 * KeyProbe.txt 의 빌드 제외를 실제 빌드로 확인했다. DesktopForest 정식 빌드(BuildOptions.None, 145 MB)의 산출물 전체에서
 * 프로브 내용 · "KeyProbe" · "HResourceTests" 검색이 0 건이었다. 같은 검색이 대조 문자열 "TMP Settings" 는
 * resources.assets 에서 찾아 검색 자체는 유효하다. Managed 에는 HCUP.HResource.dll 만 있고 테스트 어셈블리 · NUnit 은 없다.
 *
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
