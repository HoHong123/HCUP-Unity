# HResource - 패키지 카드

> 모듈: `HResource/` · 소스 35파일 · `package.json` 없음 (저장소 통째 사용)
> 구성 어셈블리 2개
> 코드 문서: **[Runtime README](Runtime/README.md)** · [Editor README](Editor/README.md)

---

## 이 패키지가 담는 것

에셋의 **로드·캐시·소유권**을 한 곳에 모은 계층이다. Addressables 와 Resources 양쪽을 같은 인터페이스 뒤로 감추고, 누가 그 에셋을 붙잡고 있는지를 `AssetOwnerId` 로 추적한다.

이 저장소에서 계층이 갈리는 지점이 여기다. `HUI`·`HAudio`·`HDialogue`·`HcupLocalization` 은 "무엇을 쓸지"만 알고, 언제 로드되고 언제 풀리는지는 전부 이 모듈이 소유한다.

| 시스템                                            | 파일  | 문서                                           |
| ---------------------------------------------- | --- | -------------------------------------------- |
| Load - 로더 3종 + 중복 요청 병합 게이트                    | 8   | [docs/Load.md](docs/Load.md)                 |
| Cache - 메모리 캐시 + owner별 점유 집합 + 에디터 진단         | 10  | [docs/Cache.md](docs/Cache.md)               |
| Provider - 진입점 + 팩토리 (+ Store·Validation·Data) | 3+6 | [docs/Provider.md](docs/Provider.md)         |
| Subscription - `AssetOwnerId` 발급·해제, leash     | 5   | [docs/Subscription.md](docs/Subscription.md) |

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 참조 |
|---|---|---|---|
| `HCUP.HResource` | Runtime | 32 | `Unity.Addressables`, `Unity.ResourceManager`, `UniTask`, `UniTask.Addressables`, `HCUP.HDiagnosis` |
| `HCUP.HResource.Editor` | Editor | 3 | `HCUP.HResource`, `HCUP.HDiagnosis` |

Editor 어셈블리는 진단 도구 두 개다. 메뉴 `HCUP/Resource/Owner Watcher` 창은 탭 2개로 **소유자의 수명**(Owner Tracker)과 **리소스의 점유**(Resource Ownership)를 함께 본다. `AssetCacheLeakReporter` 는 플레이 종료 시 회수되지 않은 점유를 콘솔로 보고한다. 점유 자료는 `Runtime/Cache` 의 `#if UNITY_EDITOR` 진단 표면에서 온다.

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다 - 이 모듈에는 `package.json` 이 없어 개별 UPM 설치 대상이 아니다 ([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 6000.3 에서 개발·검증 |
| Addressables / ResourceManager | 로더의 한 갈래. Resources 만 쓰더라도 asmdef 참조 때문에 패키지는 필요하다 |
| UniTask | 비동기 로드 전 구간 |

---

## 쓰기 전에 알아야 할 계약

1. **소유는 유무다.** 같은 owner 가 같은 key 를 몇 번 얻든 점유는 하나이고, 한 번의 반납으로 끝난다. 소유자가 자기 획득 횟수를 기억할 필요가 없다.
2. **소유자 단위로 반납한다.** `ReleaseOwner(this)` 한 번이면 그 소유자가 잡은 전부가 회수된다. Component 소유자는 그것마저 빠뜨려도 파괴 프로브가 같은 회수를 수행한다.
3. **`Release` 의 `false` 는 실패가 아니다.** "테이블에서 제거되지 않았다"는 뜻이고, 여기에는 "정상 감소했지만 점유가 남음"이 포함된다. false 를 실패로 보고 재시도하면 이중 해제가 된다.
4. **provider 는 만든 쪽이 폐기한다.** `IAssetSource` 는 `IDisposable` 이다. 폐기하지 않고 버린 provider 의 로더 핸들은 어떤 API 로도 되돌릴 수 없다.

---

## 주의할 점

1. **`IAssetStore` 는 기본 구현을 제공하지 않는다.** `LocalStoreFirst`/`LocalStoreOnly` fetch 모드와 `ClearStoreAsync` 는 store 를 직접 구현해 팩토리에 넘길 때만 의미를 갖는다. store 없이 `LocalStore*` 를 요청하면 예외다.
2. **`AddressableLabelLoader` 는 provider 축과 별개다.** `IAssetLoader` 를 구현하지 않아 provider 에 등록할 수 없고, 캐시·소유권·게이트가 적용되지 않는다. 쓰려면 핸들 해제(`Release*ByLabel` / `ReleaseAll`)를 호출자가 직접 한다.
3. **에디터 진단 표면은 `Runtime/Cache` 에 있다.** `IAssetCacheDiagnostics` / `AssetCacheDiagnosticsRegistry` / `AssetCacheDiagnosticsHandle` / `AssetOccupancySnapshot` / `AssetOwnerOccupancy` 5파일이며 전부 `#if UNITY_EDITOR` 라 빌드 공개 표면은 늘지 않는다. 레지스트리는 약한 참조를 쓴다.
4. **자동 회수는 GameObject 파괴에만 걸린다.** `OwnerLeashProbe` 는 소유자의 GameObject 에 붙으므로 `Destroy(component)` 로 컴포넌트만 지우면 통지가 오지 않는다. 그 점유는 `IAssetSource.ReclaimOrphans()` 를 부르거나 워처 툴바의 `Orphan Clean` 을 누를 때 걷힌다. 부르는 시점은 호출자가 정한다.
5. **순수 C# 소유자는 `ICSharpAssetLeash` 반납이 의무다.** 붙일 GameObject 가 없어 anchor 파괴가 유일한 자동 상한이고, GC 된 순수 소유자는 `ReclaimOrphans()` 로도 걷히지 않는다.

근거 라인은 [Runtime README](Runtime/README.md) 의 "주의할 점" 절에 있다.

---

## 히스토리

### 2026-09-07 :: 죽은 소유자 점유의 회수 수단 추가

- 이전: `Destroy(component)` 와 순수 C# 소유자의 점유는 Owner Watcher 의 진단으로만 드러났다.
- 현재: `IAssetSource.ReclaimOrphans()` 와 워처의 `Orphan Clean` 이 그 점유를 걷는다. 자동 감지는 없다.

### 2026-09-04 :: 소유권 계층 개편

- 이전: `AssetLeaseManager` / `IAssetLeaseManager` / `IAssetLease` / `IAssetOwner` 가 옵트인 lease 계층이었다. 공개 계약은 `IAssetProvider` 였고 `IDisposable` 을 상속하지 않았다.
- 현재: lease 계층을 삭제하고 provider 상주 객체 `AssetLeashManager` 로 대체했다. 모든 획득이 지문 발급을 지난다. 공개 계약은 `IAssetSource<TKey, TAsset>` 이고 `IDisposable` 을 상속한다.
- 같은 날 에디터 진단 표면이 `Runtime/Cache` 에 추가됐다.

### 2026-08-06 :: 식별자 위조 경로 차단과 에디터 네임스페이스 정정

- 이전: `int → AssetOwnerId` implicit 변환이 발급기를 우회했다. Editor 어셈블리 네임스페이스가 `HUtil.Editor.Subscription` 이었다.
- 현재: 역방향 변환을 제거했다. 네임스페이스는 `HResource.Editor.Subscription`, 메뉴 경로는 `HCUP/Resource/Owner Watcher` 다.
