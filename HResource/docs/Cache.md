# Cache - 점유와 소유권

> 대상: `Runtime/Cache/*.cs` (`IAssetReader` / `IAssetWriter` / `IAssetReleaser` / `IAssetCache` / `MemoryAssetCache`)
> 상위 문서: [Runtime/README.md](../Runtime/README.md)

---

## 요약

캐시는 HResource 에서 **점유를 실제로 보유하는 유일한 지점**이다. provider·leash·owner 는
전부 이 점유를 조작하는 경로일 뿐이고, 에셋이 살아 있는지 여부는 `MemoryAssetCache` 의 항목
하나가 결정한다. 그 항목이 지워지는 순간에만 `OnAssetRemoved` 가 발화하고, 그 신호가 소스
핸들 해제로 이어진다.

---

## 계약 3분할

```mermaid
flowchart TD
    R["IAssetReader&lt;TKey, TAsset&gt;<br/>TryLoad ×2 / TryGet"]
    W["IAssetWriter&lt;TKey, TAsset&gt;<br/>Save ×2"]
    D["IAssetReleaser&lt;TKey&gt;<br/>Release ×2 / ReleaseOwner / ReleaseAll / Clear"]
    C["IAssetCache&lt;TKey, TAsset&gt;<br/>+ event OnAssetRemoved"]
    M["MemoryAssetCache&lt;TKey, TAsset&gt;"]

    R --> C
    W --> C
    D --> C
    C --> M
```

세 인터페이스를 나눠 둔 이유는 **부분 계약으로 주입할 여지**를 남기기 위해서다. 다만 현재
패키지 안에서 `IAssetReader` / `IAssetWriter` / `IAssetReleaser` 를 단독 타입으로 받는 코드는
없다 - 전부 `IAssetCache` 로만 다룬다(`Provider/AssetProvider.cs:49`).

---

## 데이터 모델 - 소유자 집합 + 양방향 인덱스

```csharp
// Cache/MemoryAssetCache.cs - 2026-09-05 기준
sealed class Item {
    public TAsset Asset;
    public HashSet<AssetOwnerId> Owners = new();   // 이 key 를 잡고 있는 소유자. 횟수가 아니라 유무
}

readonly Dictionary<TKey, Item> table = new();                        // key  → 항목
readonly Dictionary<AssetOwnerId, HashSet<TKey>> ownerTable = new();  // owner → 잡은 key 집합
```

`Owners` 가 `Dictionary<_, int>` 가 아니라 `HashSet` 인 것이 중요하다. 소유는 불린 관계다.
"이 소유자가 이 key 를 살려둬야 하나" 는 예/아니오이고 "두 번 살려둬야 한다" 는 뜻이 없다.

2026-09-05 이전에는 횟수를 셌다. 그 모델은 소유자가 자기 획득 횟수를 기억할 것을 요구하는데,
그 기록을 들 자리가 없는 소비자가 실제로 있어 두 곳에서 누수가 났다. 자세한 것은
`RumbleJasonDocs/02_Code/02_Refactoring/02_26.09.05_HResource 소유권 강제.md` 에 있다.

`ownerTable` 은 역인덱스다. `ReleaseOwner` 가 전체 `table` 을 순회하지 않고 **그 owner 가 잡은
key 만** 도는 데 쓰인다(`ReleaseOwner`).

### 제거 조건

```mermaid
flowchart TD
    A["Release 계열 호출"] --> B["그 소유자를 Owners 에서 제거"]
    B --> C["_TryRemoveItem"]
    C --> F{"Owners.Count &gt; 0"}
    F -->|예| E["유지 - false 반환"]
    F -->|아니오| G["table.Remove"]
    G --> H["OnAssetRemoved 발화"]
    H --> I["AssetProvider._ReleaseAssetLoaders - 소스 핸들 해제"]
```

**`Owners` 가 비면 제거된다.** 2026-09-04 이전에는 `AnonymousDependency` 와 둘 다 비어야 했고,
익명으로 얻고 owner 로 반납하면 항목이 영구 잔류하는 함정이 있었다. 익명 축을 제거해 그 함정이
구조적으로 사라졌다.

`Owners` 는 2026-09-05 부터 `HashSet<AssetOwnerId>` 다. 점유는 횟수가 아니라 유무다.
같은 소유자가 같은 key 를 여러 번 요청해도 상태는 바뀌지 않고, 한 번의 반납으로 끝난다.

---

## 네 개의 해제 경로

| 메서드 | 대상 | 감소 방식 | 반환 |
|---|---|---|---|
| `Release(key, ownerId)` | 그 owner 의 점유 | **제거**. 세트라 한 번이면 끝난다 | 항목까지 제거됐으면 `true` |
| `ReleaseOwner(ownerId)` | 그 owner 의 전 key | **제거**. 단건 Release 와 같은 의미 | 내려놓은 key 수 |
| `ReleaseAll()` | 전체 | 소유자를 가리지 않고 전량 제거 | - |
| `Clear()` (`:180-182`) | 전체 | `ReleaseAll` 과 동일 구현 | - |

`Release(key, ownerId)` 가 `false` 를 반환하는 경우가 두 가지라는 점에 주의한다 - **해당 owner
가 그 key 를 안 잡고 있을 때**(경고를 남긴다)와 **내려놓았지만 다른 owner 가 남아 항목이
유지될 때**가 같은 `false` 다. 후자는 정상 동작이다.

~~`ReleaseOwner` 가 카운트를 무시하는 것은 명시적 의도다.~~ -> 2026-09-05 해소. 점유가
횟수에서 유무로 바뀌어 `Release` / `ReleaseOwner` / 파괴 프로브 세 경로의 의미가 일치한다.
종전에는 같은 논리 조작이 어느 API 를 쓰느냐에 따라 다르게 동작했다.

---

## 흐름 - 두 소유자가 같은 key 를 공유

```mermaid
sequenceDiagram
    participant A as OwnerA
    participant B as OwnerB
    participant P as AssetProvider
    participant M as MemoryAssetCache
    participant L as AddressableAssetLoader

    A->>P: GetAsync(key, ownerId=A)
    P->>L: LoadAsync(key)
    L-->>P: asset - handleTable[key] 등록
    P->>M: Save(key, asset, A)
    Note over M: Owners = {A:1}, ownerTable[A] = {key}

    B->>P: GetAsync(key, ownerId=B)
    Note over P,M: 캐시 히트 - 로더 호출 없음
    P->>M: Save(key, asset, B)
    Note over M: Owners = {A:1, B:1}

    A->>P: ReleaseOwner(A)
    P->>M: ReleaseOwner(A)
    Note over M: Owners = {B:1} - 아직 제거 안 됨
    M-->>P: 1

    B->>P: Release(key, B)
    Note over M: Owners = {} → 제거
    M->>P: OnAssetRemoved(key, asset)
    P->>L: Release(key) - Addressables.Release
```

---

## `Save` 의 세 갈래

```csharp
// Cache/MemoryAssetCache.cs - Save
if (!ownerId.IsValid) { HLogger.Error(...); return false; } // ① 무효 id → 거부. 익명 점유는 만들지 않는다
if (table.TryGetValue(key, out var item)) {
    if (ReferenceEquals(item.Asset, asset))               // ② 같은 asset → 소유자 등록만
        { _AddOwnerDependency(item, ownerId, key); return true; }
    HLogger.Error(...);                                   // ③ 다른 asset → 거부 + 에러
    return false;
}
```

③ 이 provider 의 롤백 경로와 짝을 이룬다 - 거부되면 provider 가 방금 로드한 핸들을 직접
해제한다(`Provider/AssetProvider.cs:172-177`). silent overwrite 를 하지 않는 대신 호출자에게
`default` 를 돌려준다.

**`Save` 는 소유자 단위로 멱등하다.** 같은 소유자가 같은 asset 을 다시 넣어도 상태가 바뀌지
않는다. provider 가 캐시 히트에도 `Save` 를 부르는 이유는 **새 소유자를 등록**하기 위해서다.

---

## `Clear` 의 재진입 방어

```csharp
// Cache/MemoryAssetCache.cs:238-261
const int MAX_CLEAR_PASSES = 8;
for (int pass = 0; pass < MAX_CLEAR_PASSES; pass++) {
    if (table.Count < 1) return;
    var removeItems = new List<KeyValuePair<TKey, Item>>(table);
    table.Clear();
    ownerTable.Clear();
    foreach (var pair in removeItems) _NotifyRemoved(pair.Key, pair.Value.Asset);
}
if (table.Count > 0) HLogger.Error("[AssetCache] Clear did not converge: ...");
```

`OnAssetRemoved` 구독자가 알림 도중 `Save` 를 다시 호출하면 그 항목이 `Clear` 를 통과해
살아남는다. 그래서 잔존이 없어질 때까지 반복하고 8회로 끊는다. **수렴 실패는 에러 로그로만
드러나고 항목은 남는다** - 예외를 던지지 않는다.

---

## 에디터 진단 표면

`#if UNITY_EDITOR` 안에서만 존재하는 읽기 전용 축이다. 빌드에는 코드가 남지 않고 런타임
공개 표면도 늘지 않는다.

`MemoryAssetCache` 가 `IAssetCacheDiagnostics` 를 구현하고, 생성자에서
`AssetCacheDiagnosticsRegistry` 에 자신을 **약한 참조**로 등록한다. 캐시는 provider 마다
`new` 로 만들어져 어디에도 등록되지 않으므로, 에디터 창이 살아 있는 캐시에 닿으려면
캐시가 스스로 손을 드는 수밖에 없다.

| 멤버 | 내용 |
| --- | --- |
| `CacheLabel` | 등록 시 발급된 표시용 문자열. 같은 제네릭 조합이 여러 개여도 순번으로 구분된다 |
| `EntryCount` | `table.Count` |
| `CaptureOccupancy(buffer)` | 버퍼를 비우고 key 마다 `AssetOccupancySnapshot` 을 채운다 |

스냅샷은 **key 중심 한 가지 모양**이다. 점유가 유무이므로 `TotalCount` 는 곧 그 key 를 잡고
있는 소유자 수다. owner 기준 뷰는 에디터가 이것을 뒤집어 만든다.
런타임 표면을 하나만 늘리고, 두 뷰가 같은 원본에서 나와 서로 어긋날 수 없게 하려는 선택이다.

반환값이 아니라 호출자의 버퍼를 채운다. 진단 창이 0.25초마다 다시 그리므로 매번 리스트를
새로 만들면 에디터에 불필요한 GC 압력이 된다.

약한 참조를 쓰는 이유는 단순하다. 강한 참조면 캐시가 영원히 GC 되지 않아, 누수를 잡으려고
만든 도구가 누수를 만든다. 죽은 참조는 `Collect` 시점에 정리한다.

## 주의할 점

1. **`TryLoad` 두 오버로드는 호출처가 0건이다**(`:54-76`, 전역 `grep "\.TryLoad("` 0건).
   `TryGet` + `Save` 조합이 provider 에서 그 역할을 대신한다. 남겨두면 "조회하면서 점유가
   오르는" 두 번째 규약이 계약(`Cache/IAssetReader.cs:26-27`)에 남는다.
2. **`ReleaseAll()` 과 `Clear()` 는 동일 구현이다**(`:176-182`). 계약상 두 이름이 있으나 의미
   차이가 없다.
3. ~~`ReleaseOwner` 는 익명 카운터를 건드리지 않는다. 익명·owner 혼용 시 항목이 남는다.~~
   -> 2026-09-04 해소. 익명 축을 제거해 카운터가 하나가 됐다. 모든 점유는 소유자를 갖는다.
4. **스레드 안전하지 않다.** `Dictionary` 를 잠금 없이 쓴다. `AssetOwnerIdGenerator` 만
   `Interlocked` 로 보호되어 있어(`Subscription/AssetOwnerIdGenerator.cs:56`) id 발급은
   스레드 안전하지만 캐시 조작은 메인 스레드 전제다.
5. **`OnAssetRemoved` 구독 해제 경로가 얇다.** 구독자는 `AssetProvider` 하나이고, 그 해제는
   `AssetProvider.Dispose()` 뿐인데 호출처가 0건이다(`Provider/AssetProvider.cs:145-147`).
6. **진단 표면도 잠금이 없다.** `CaptureOccupancy` 는 4번과 같은 메인 스레드 전제를 따른다.
   캐시 조작 중에 다른 스레드에서 캡처하면 열거 도중 수정 예외가 난다.
