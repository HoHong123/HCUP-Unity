# HResource — 패키지 카드

> 모듈: `HResource/` · 소스 30파일 · `package.json` 없음 (저장소 통째 사용)
> 구성 어셈블리 2개
> 코드 문서: **[Runtime README](Runtime/README.md)** · [Editor README](Editor/README.md)

---

## 이 패키지가 담는 것

에셋의 **로드·캐시·소유권**을 한 곳에 모은 계층이다. Addressables 와 Resources 양쪽을 같은
인터페이스 뒤로 감추고, 누가 그 에셋을 붙잡고 있는지를 `AssetOwnerId` 로 추적한다.

이 저장소에서 계층이 갈리는 지점이 여기다. `HUI`·`HAudio`·`HDialogue`·`HcupLocalization` 은
"무엇을 쓸지"만 알고, 언제 로드되고 언제 풀리는지는 전부 이 모듈이 소유한다.

| 시스템 | 파일 | 문서 |
|---|---|---|
| Load — 로더 3종 + 중복 요청 병합 게이트 | 8 | [docs/Load.md](docs/Load.md) |
| Cache — 메모리 캐시 + owner별 점유 카운트 | 5 | [docs/Cache.md](docs/Cache.md) |
| Provider — 진입점 + 팩토리 (+ Store·Validation·Data) | 3+6 | [docs/Provider.md](docs/Provider.md) |
| Subscription — `AssetOwnerId` 발급·해제, Lease | 6 | [docs/Subscription.md](docs/Subscription.md) |

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 참조 |
|---|---|---|---|
| `HCUP.HResource` | Runtime | 28 | `Unity.Addressables`, `Unity.ResourceManager`, `UniTask`, `UniTask.Addressables`, `HCUP.HDiagnosis` |
| `HCUP.HResource.Editor` | Editor | 2 | `HCUP.HResource` |

Editor 어셈블리는 owner 점유 현황을 보는 진단 창 하나다 (메뉴 `HCUP/Data/Owner Watcher`).

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다 — 이 모듈에는 `package.json` 이 없어 개별 UPM 설치 대상이 아니다
([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 이 프로젝트 기준 6000.3.18f1 |
| Addressables / ResourceManager | 로더의 한 갈래. Resources 만 쓰더라도 asmdef 참조 때문에 패키지는 필요하다 |
| UniTask | 비동기 로드 전 구간 |

---

## 쓰기 전에 알아야 할 계약

1. **획득과 해제는 1:1 이다.** 같은 owner 가 같은 key 를 두 번 얻었으면 두 번 반납해야 한다.
   HashSet 이 아니라 횟수로 센다.
2. **owner 를 지정했으면 owner 로 반납한다.** `OnDestroy` 에서 `ReleaseOwner(ownerId)` 한 번이면
   그 owner 가 잡은 전부가 회수되므로, 개별 `Release` 를 빠뜨려도 누수는 남지 않는다.
3. **`Release` 의 `false` 는 실패가 아니다.** "테이블에서 제거되지 않았다"는 뜻이고, 여기에는
   "정상 감소했지만 점유가 남음"이 포함된다. false 를 실패로 보고 재시도하면 이중 해제가 된다.

---

## 주의할 점

1. **`IAssetStore` 구현체가 0개다.** `LocalStoreFirst`/`LocalStoreOnly` fetch 모드와
   `ClearStoreAsync`/`DeleteAsync` 는 전부 도달 불가 경로다.
2. **사용처 0건 축이 둘 있다** — `AddressableLabelLoader`(315행)와 `AssetLeaseManager`(260행).
   전자는 `IAssetLoader` 를 구현하지 않아 provider 에 등록할 수도 없다.
3. **`AssetProvider.Dispose()` 를 부르는 곳이 없고, `IAssetProvider` 는 `IDisposable` 을 상속하지
   않는다.** 인터페이스로 보유하는 소비자는 `OnAssetRemoved` 구독을 해제할 수단이 없다.
4. **`int → AssetOwnerId` 암시 변환이 생성기를 우회한다.** 임의 정수가 owner 로 통과한다.
5. Editor 어셈블리의 네임스페이스가 `HUtil.Editor.Subscription` 으로 남아 있다 (HUtil 분리 잔재).

근거 라인은 [Runtime README](Runtime/README.md) 의 "정리 대상" 절에 있다.
