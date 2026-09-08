# HResource - 패키지 카드

> 모듈: `HResource/` · 소스 36파일 · `package.json` 없음 (저장소 통째 사용)
> 구성 어셈블리 2개
> 코드 문서: **[Runtime README](Runtime/README.md)** · [Editor README](Editor/README.md)

---

## 이 패키지가 담는 것

에셋의 **로드·캐시·소유권**을 한 곳에 모은 계층이다. Addressables 와 Resources 양쪽을 같은
인터페이스 뒤로 감추고, 누가 그 에셋을 붙잡고 있는지를 `AssetOwnerId` 로 추적한다.

이 저장소에서 계층이 갈리는 지점이 여기다. `HUI`·`HAudio`·`HDialogue`·`HcupLocalization` 은
"무엇을 쓸지"만 알고, 언제 로드되고 언제 풀리는지는 전부 이 모듈이 소유한다.

| 시스템                                            | 파일  | 문서                                           |
| ---------------------------------------------- | --- | -------------------------------------------- |
| Load - 로더 3종 + 중복 요청 병합 게이트                    | 8   | [docs/Load.md](docs/Load.md)                 |
| Cache - 메모리 캐시 + owner별 점유 집합                   | 10  | [docs/Cache.md](docs/Cache.md)               |
| Provider - 진입점 + 팩토리 (+ Store·Validation·Data) | 3+6 | [docs/Provider.md](docs/Provider.md)         |
| Subscription - `AssetOwnerId` 발급·해제, Lease     | 6   | [docs/Subscription.md](docs/Subscription.md) |

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 참조 |
|---|---|---|---|
| `HCUP.HResource` | Runtime | 33 | `Unity.Addressables`, `Unity.ResourceManager`, `UniTask`, `UniTask.Addressables`, `HCUP.HDiagnosis` |
| `HCUP.HResource.Editor` | Editor | 3 | `HCUP.HResource` |

Editor 어셈블리는 진단 창 하나다 (메뉴 `HCUP/Resource/Owner Watcher`). 탭 2개로
**소유자의 수명**(Owner Tracker)과 **리소스의 점유**(Resource Ownership)를 함께 본다.
점유 자료는 `Runtime/Cache` 의 `#if UNITY_EDITOR` 진단 표면에서 온다.

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다 - 이 모듈에는 `package.json` 이 없어 개별 UPM 설치 대상이 아니다
([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 이 프로젝트 기준 6000.3.18f1 |
| Addressables / ResourceManager | 로더의 한 갈래. Resources 만 쓰더라도 asmdef 참조 때문에 패키지는 필요하다 |
| UniTask | 비동기 로드 전 구간 |

---

## 쓰기 전에 알아야 할 계약

1. **소유는 유무다.** 같은 owner 가 같은 key 를 몇 번 얻든 점유는 하나이고, 한 번의 반납으로
   끝난다. 소유자가 자기 획득 횟수를 기억할 필요가 없다.
2. **소유자 단위로 반납한다.** `ReleaseOwner(this)` 한 번이면 그 소유자가 잡은 전부가
   회수된다. Component 소유자는 그것마저 빠뜨려도 파괴 프로브가 같은 회수를 수행한다.
3. **`Release` 의 `false` 는 실패가 아니다.** "테이블에서 제거되지 않았다"는 뜻이고, 여기에는
   "정상 감소했지만 점유가 남음"이 포함된다. false 를 실패로 보고 재시도하면 이중 해제가 된다.

---

## 주의할 점

1. **`IAssetStore` 구현체가 0개다.** `LocalStoreFirst`/`LocalStoreOnly` fetch 모드와
   `ClearStoreAsync`/`DeleteAsync` 는 전부 도달 불가 경로다.
2. ~~사용처 0건 축이 둘 있다 - `AddressableLabelLoader`(315행)와 `AssetLeaseManager`(260행).~~
   -> 2026-09-04 절반 해소. `AssetLeaseManager` 는 삭제하고 `AssetLeashManager` 로 대체했다.
   새 계층은 옵트인이 아니라 provider 의 상주 객체라 모든 획득이 그곳을 지난다.
   `AddressableLabelLoader` 는 여전히 사용처 0건이고 `IAssetLoader` 를 구현하지 않아
   provider 에 등록할 수도 없다.
3. ~~`AssetProvider.Dispose()` 를 부르는 곳이 없고, `IAssetProvider` 는 `IDisposable` 을 상속하지
   않는다.~~ -> 2026-09-04 정정. 두 서술 모두 현재 코드와 다르다. 공개 계약은
   `IAssetSource<TKey, TAsset>` 로 교체됐고 `IDisposable` 을 상속한다.
   `Dispose()` 호출처도 `AudioClipRepository` / `CharacterStageDirector` /
   `LocalizationManager` 로 존재한다.
4. ~~`int → AssetOwnerId` 암시 변환이 생성기를 우회한다.~~ → 2026-08-06 해소. `int → AssetOwnerId`
   방향의 implicit 변환을 제거해 임의 정수가 owner 로 통과하는 경로를 컴파일 타임에 차단.
5. ~~Editor 어셈블리의 네임스페이스가 `HUtil.Editor.Subscription` 으로 남아 있다~~ → 2026-08-06
   `HResource.Editor.Subscription` 으로 정정, 메뉴 경로도 `HCUP/Resource/Owner Watcher` 로 통일.
6. **에디터 진단 표면이 `Runtime/Cache` 에 추가됐다** (2026-09-04). `IAssetCacheDiagnostics` /
   `AssetCacheDiagnosticsRegistry` / `AssetOccupancySnapshot` / `AssetOwnerOccupancy` 4파일이며
   전부 `#if UNITY_EDITOR` 라 빌드 공개 표면은 늘지 않는다. 레지스트리는 약한 참조를 쓴다.
7. **자동 회수는 GameObject 파괴에만 걸린다** (2026-09-04). `OwnerLeashProbe` 는 소유자의
   GameObject 에 붙으므로 `Destroy(component)` 로 컴포넌트만 지우면 통지가 오지 않는다.
   순수 C# 소유자는 붙일 GameObject 가 없어 `ICSharpAssetLeash` 의 `using` 이 유일한 보증이다.
   ~~두 경우 모두 Owner Watcher 의 진단이 최종 방어선이다.~~ → 2026-09-07 보완.
   진단에서 그치지 않고 회수까지 간다. `IAssetSource.ReclaimOrphans()` 를 부르면 소유자가
   죽은 항목을 약한 표 대조로 찾아 걷어내고, 에디터에서는 워처 툴바의 `Orphan Clean` 이
   같은 일을 한다. 자동 감지는 여전히 없다 - 부르는 시점은 호출자가 정한다.

근거 라인은 [Runtime README](Runtime/README.md) 의 "정리 대상" 절에 있다.
