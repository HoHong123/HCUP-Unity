# HCUP.HAudio

> 어셈블리: `HCUP.HAudio` (`Runtime/HCUP.HAudio.asmdef`, rootNamespace `HAudio`)
> 의존: `UniTask`, `HCUP.HResource`, `HCUP.HCore`, `HCUP.HUtil`, `HCUP.HUI`, `HCUP.HInspector`, `HCUP.HDiagnosis`, `HCUP.HCollection`
> 동반 어셈블리: `HCUP.HAudio.Editor`(카탈로그 편집·생성·진단)

---

## 요약

HAudio 는 **`string token` 하나로 오디오를 지목하는 재생 계층**이다. 토큰이 실제 에셋 경로로
번역되는 과정과, 그 에셋의 수명을 누가 붙잡고 있는지를 추적하는 일은 전부 이 어셈블리 안에서
끝나고, **실제 로드·캐시·해제는 `HCUP.HResource` 의 `AssetProvider<string, AudioClip>` 에 위임**한다.

설계의 중심에 두 가지 규약이 있다.

1. **재생은 로드하지 않는다.** `Play*` 계열은 이미 메모리에 있는 클립만 재생한다. 없으면 조용히
   실패하고(에디터에서는 스택 트레이스 경고) 로드를 시작하지 않는다. 로드는 `Prewarm*` 의 몫이다.
2. **해제는 소유자(owner) 단위다.** `AudioManager` 는 `Awake` 에서 자신에게 묶인 `AssetOwnerId` 를
   발급받고, `OnDestroy` 에서 그 id 로 잡고 있던 전부를 한 번에 반납한다.
3. **식별자는 `string token` 하나다.** int/enum 으로 클립을 지목하는 경로는 존재하지 않는다.
   `AudioMajorCategory` 는 카탈로그를 읽을 때의 분류 라벨일 뿐 로드에 관여하지 않는다.

---

## 파일 지도

| 경로 | 역할 |
|---|---|
| `AudioManager.cs` | 런타임 진입점. prewarm/release/play/volume. `SingletonBehaviour<AudioManager>` |
| `AudioManager.Preview.cs` | 에디터 진단용 스냅샷 생성 + preview 추적 상태 (partial) |
| `AudioClipManagerSnapshot.cs` | 진단 창·Odin 인스펙터에 넘길 DTO |
| `Repository/IAudioClipRepository.cs` | 조회·로드·prewarm·release 계약 |
| `Repository/AudioClipRepository.cs` | **token → load key 번역** + `AssetProvider` 위임 |
| `Catalog/AudioCatalogRegistry.cs` | 활성 카탈로그 집합과 **토큰→Entry 인덱스**, 참조 카운트 |
| `Core/AudioCatalogSO.cs` | 데이터 원본. `Entry` 정의 + load key 빌더 |
| `Core/AudioCatalogPolicySO.cs` | 폴더 → 카테고리 매핑 규칙 (에디터 생성기용) |
| `Enum/AudioMajorCategory.cs` | BGM / SFX / UI / Voice - 카탈로그 분류 라벨 |
| `AddOn/AudioSpatialPool.cs` | 3D 원샷 재생용 `AudioSource` 풀 |
| `AddOn/SfxView.cs` | 카탈로그 묶음 직렬화 컨테이너 + 에디터 토큰 미리보기 |
| `AddOn/SfxAgent.cs` | 오브젝트 수명에 맞춘 prewarm/release + 재생 프록시 |
| `AddOn/BaseSfxAddon.cs` | 클릭 사운드 공통 처리 (기본 클릭 토큰 / 오버라이드) |
| `AddOn/ButtonSfxAddon.cs` | `DelegateButton.OnPointUp` 배선 |
| `AddOn/ToggleSfxAddon.cs` | `Toggle.onValueChanged` 배선 (on 일 때만) |

---

## 계층 구조

```mermaid
flowchart TD
    subgraph 호출측
    A["게임 코드 / SfxAgent / *SfxAddon"]
    end
    subgraph HAudio
    B["AudioManager"]
    C["IAudioClipRepository<br/>AudioClipRepository"]
    D["AudioCatalogRegistry"]
    E["AudioCatalogSO"]
    F["AudioSpatialPool"]
    end
    subgraph HResource
    G["AssetProvider&lt;string, AudioClip&gt;"]
    H["MemoryAssetCache"]
    I["Resources / Addressable Loader"]
    end
    subgraph Unity
    J["AudioSource ×4"]
    K["AudioMixer"]
    end

    A -->|"token"| B
    B -->|"prewarm / release / 조회"| C
    C -->|"token → Entry"| D
    D -->|"등록·참조카운트"| E
    C -->|"load key"| G
    G --> H
    G --> I
    B -->|"AudioClip"| J
    B -->|"AudioClip"| F
    B -->|"dB"| K
```

**책임 경계가 갈리는 지점은 `AudioClipRepository` 하나다.** 위쪽(AudioManager)은 토큰만 알고,
아래쪽(AssetProvider)은 load key 만 안다. 그 사이의 번역이 리포지토리의 존재 이유다.

---

## 데이터 모델

`AudioCatalogSO` 가 유일한 데이터 원본이고, `Entry` 하나가 클립 하나를 서술한다.

```csharp
// Core/AudioCatalogSO.cs - Entry
[Serializable]
public sealed class Entry {
    [SerializeField] AudioMajorCategory major;   // 분류 라벨. 읽기 편하라고 있는 것이지 키가 아니다
    [SerializeField] string token;               // 유일한 키. 시스템 전체의 기준
    [SerializeField] string path;                // Resources 모드에서 폴더 경로
#if UNITY_EDITOR
    [SerializeField] AudioClip editorClip;       // 편집·미리보기 전용. 빌드에는 없다
#endif
}
```

**`token` 이 Entry 의 유일한 식별자다.** `AudioCatalogSO.BuildCache` 도 token 으로 인덱싱하고
중복을 그 기준으로 잡는다. `major` 는 정렬·검색·폴더 추론에만 쓰인다.

### load key 로의 번역

`token` 은 그 자체로 에셋을 찾지 못한다. 모드에 따라 두 갈래로 번역된다.

```csharp
// Core/AudioCatalogSO.cs:96-104
public static string BuildResourcesLoadKey(string path, string token) {
    if (string.IsNullOrWhiteSpace(token)) return string.Empty;
    if (string.IsNullOrWhiteSpace(path)) return token;
    return $"{path.Trim('/')}/{token}";     // 예: "Audio/UI" + "Click" → "Audio/UI/Click"
}
```

```mermaid
flowchart TD
    T["token 입력"] --> N["_NormalizeToken - Trim"]
    N --> Q{"비어 있나"}
    Q -->|예| X["실패 - false"]
    Q -->|아니오| R{"registry 에 Entry 가 있나"}
    R -->|있음| M{"LoadMode"}
    M -->|Resources| RK["path + '/' + token"]
    M -->|Addressable| AK["token 그대로"]
    R -->|"없음"| F{"LoadMode 가 Addressable 인가"}
    F -->|예| AK2["token 을 load key 로 직접 사용"]
    F -->|"아니오 - Resources"| X2["실패 - path 를 알 수 없다"]
```

**Resources 모드에서는 카탈로그에 등록되지 않은 토큰이 절대 로드되지 않는다.** `path` 를 알
방법이 없기 때문이다(`AudioClipRepository.cs:146-159`). Addressable 모드에만 "토큰 = 주소"
폴백이 있다.

---

## 흐름 1 - 초기화

```mermaid
sequenceDiagram
    participant U as Unity
    participant AM as AudioManager
    participant GEN as AssetOwnerIdGenerator
    participant REG as AudioCatalogRegistry
    participant REPO as AudioClipRepository
    participant F as AssetProviderFactory

    U->>AM: Awake
    AM->>AM: base.Awake - 중복 인스턴스면 여기서 종료
    AM->>AM: _BuildRepository
    Note over AM: Mixer·AudioSource·spatialPool null 검사 후 로그
    AM->>REG: new AudioCatalogRegistry
    AM->>REPO: new AudioClipRepository(loadMode, registry, this)
    REPO->>F: CreateResources / CreateAddressable
    F-->>REPO: IAssetSource
    U->>AM: Start
    AM->>AM: _CheckPlayerPrefs - 볼륨 4종 + 기본 클릭 토큰 복원
```

`_BuildRepository` 의 null 검사는 `Assert` 가 아니라 `HLogger.Error` 다. Assert 는 릴리즈
빌드에서 통째로 제거되므로, 인스펙터 배선 누락은 릴리즈에서도 드러나야 한다는 판단이다
(`AudioManager.cs:138`).

---

## 흐름 2 - Prewarm → Play

이 시스템을 이해하는 핵심이다. **두 흐름은 완전히 분리되어 있고, 서로를 기다리지 않는다.**

```mermaid
sequenceDiagram
    participant C as 호출자
    participant AM as AudioManager
    participant REPO as AudioClipRepository
    participant REG as AudioCatalogRegistry
    participant AP as AssetProvider
    participant SRC as AudioSource

    rect rgb(240, 248, 255)
    Note over C,AP: ① Prewarm - 비동기, 로드를 실제로 수행한다
    C->>AM: await PrewarmCatalog(catalog)
    AM->>REPO: PrewarmCatalogAsync(catalog, ownerId)
    REPO->>REG: RegisterCatalog(catalog)
    REG->>REG: BuildCache + Entry 전량 인덱싱, refCount = 1
    loop catalog.Entries
        REPO->>REPO: GetOrLoadAsync(entry.Token, ownerId)
        REPO->>AP: GetAsync(loadKey, loadMode, fetchMode, ownerId)
    end
    AP-->>REPO: AudioClip - 캐시 등록 + owner 점유
    end

    rect rgb(255, 250, 240)
    Note over C,SRC: ② Play - 동기, 조회만 한다
    C->>AM: Play(token)
    AM->>REPO: TryGet(token, out clip)
    REPO->>REG: TryGetEntry → load key
    REPO->>AP: TryGet(loadKey)
    alt 캐시 히트
        AP-->>AM: clip
        AM->>SRC: sfxAudio.PlayOneShot(clip)
    else 미스
        AM-->>C: 무음 - 에디터에서만 StackTraceError
    end
    end
```

`Play` 계열이 로드를 트리거하지 않는 것은 **의도된 제약**이다. 재생 시점의 프레임 스파이크와
"소리가 한 박자 늦게 나는" 문제를 구조적으로 없애는 대신, 호출자에게 prewarm 책임을 지운다.

```csharp
// AudioManager.cs:272-289 - 조회 실패는 조용하다. 에디터에서만 시끄럽다.
private bool _TryGetLoadedClip(string token, out AudioClip clip) {
    if (clipRepository == null) { HLogger.Error("[AudioManager] clipRepository is null."); clip = null; return false; }
    string normalizedToken = _NormalizeToken(token);
    if (clipRepository.TryGet(normalizedToken, out clip) && clip) return true;
#if UNITY_EDITOR
    HDebug.StackTraceError($"[AudioManager] Clip not loaded yet. Prewarm required. token={normalizedToken}", 10);
#endif
    clip = null;
    return false;
}
```

---

## 흐름 3 - 참조 카운트와 해제

해제는 **두 겹의 참조 카운트**를 통과해야 실제로 일어난다. 카탈로그 단위(레지스트리)와
에셋 단위(HResource 캐시의 owner 점유)다.

```mermaid
flowchart TD
    A["ReleaseCatalog(catalog)"] --> B["AudioClipRepository.ReleaseCatalog"]
    B --> C["AudioCatalogRegistry.ReleaseCatalog"]
    C --> D{"catalog refCount"}
    D -->|"1 초과 - 감소만"| E["아무것도 해제하지 않는다"]
    D -->|"1 → 0"| F["catalogRefTable 에서 제거"]
    F --> G["Entry 별 EntrySlot.Release"]
    G --> H{"EntrySlot refCount 가 0 인가"}
    H -->|"아니오 - 다른 카탈로그가 같은 토큰을 공유"| I["인덱스 유지"]
    H -->|예| J["removedEntries 에 수집"]
    J --> K["각 Entry → load key 변환"]
    K --> L["AssetProvider.Release(owner, loadKey)"]
    L --> M["HResource owner 점유 감소 → 0 이면 실제 언로드"]
```

**같은 토큰을 두 카탈로그가 공유하는 경우**를 `EntrySlot` 이 처리한다. 카탈로그 A 를 내려도
카탈로그 B 가 그 토큰을 붙잡고 있으면 인덱스도 에셋도 살아남는다.

```csharp
// Catalog/AudioCatalogRegistry.cs:21-38
sealed class EntrySlot {
    public AudioCatalogSO.Entry Entry { get; private set; }
    public int RefCount { get; private set; }
    public EntrySlot(AudioCatalogSO.Entry entry) { Entry = entry; RefCount = 1; }
    public void Retain() { RefCount++; }
    public int Release() { if (RefCount > 0) RefCount--; return RefCount; }
}
```

### 매니저 수명 종료

```mermaid
sequenceDiagram
    participant U as Unity
    participant AM as AudioManager
    participant REPO as AudioClipRepository
    participant AP as AssetProvider
    participant GEN as AssetOwnerIdGenerator

    U->>AM: OnDestroy
    AM->>REPO: ReleaseAll()
    REPO->>AP: ReleaseOwner(this)
    AP->>AP: 이 매니저가 점유한 전 key 반납
    Note over AP: 마지막 점유였던 key 는 OnAssetRemoved → 로더 핸들 해제로 이어진다
    Note over AP,GEN: 지문 폐기와 NotifyReleased 는 AssetLeashManager 안에서 일어난다
    AM->>AM: base.OnDestroy - static instance 해제
```

카탈로그를 몇 개 붙잡고 있었든 **`OnDestroy` 한 번으로 전부 정리된다.** 개별 `ReleaseCatalog`
호출을 빠뜨려도 매니저 파괴 시점에 누수가 남지 않는 이유다.

---

## 흐름 4 - SfxAgent 의 수명 결합

`SfxAgent` 는 "이 오브젝트가 살아 있는 동안만 이 카탈로그들이 메모리에 있으면 된다" 를 표현한다.

```mermaid
sequenceDiagram
    participant U as Unity
    participant AG as SfxAgent
    participant AM as AudioManager

    U->>AG: Start
    AG->>AM: PrewarmSfxView(preloadCatalogs)
    Note over AG: prewarmTask 에 UniTask 를 보관한다
    U->>AG: OnDestroy
    AG->>AG: await prewarmTask
    Note over AG: ⚠ 완료를 기다리지 않고 해제하면<br/>등록이 뒤늦게 도착해 refCount 가 영구 잔류한다
    AG->>AM: ReleaseSfxView(preloadCatalogs)
```

```csharp
// AddOn/SfxAgent.cs:121-127
private async UniTaskVoid _ReleaseViewsAfterPrewarm() {
    // prewarm 이 in-flight 인 채로 release 가 먼저 실행되면 등록이 뒤늦게 도착해
    // registry refCount 가 영구 잔류한다 - 완료를 기다린 뒤 해제한다.
    await prewarmTask;
    if (!AudioManager.HasInstance) return;
    AudioManager.Instance.ReleaseSfxView(preloadCatalogs);
}
```

이 주석이 가리키는 것은 **fire-and-forget prewarm 과 동기 release 의 경쟁**이다. 짧게 살다
사라지는 오브젝트(팝업, 이펙트)에서 실제로 발생하던 누수라, 같은 패턴을 다른 곳에서 쓸 때도
같은 방어가 필요하다.

---

## 재생 채널

| API | 출력 | 비고 |
|---|---|---|
| `Play(token)` | `sfxAudio.PlayOneShot` | 2D 효과음 |
| `PlayUI(token)` | `uiAudio.PlayOneShot` | UI 효과음 |
| `PlayClick()` | `PlayUI(기본 클릭 uid)` | 토큰이 아니라 uid 다. `SetGlobalClickUid` 로 지정하고 0 이면 무동작 |
| `Play3D(token, Transform)` | `AudioSpatialPool.PlayAt` | 부모에 붙여 따라다닌다 |
| `Play3D(token, Vector3)` | `AudioSpatialPool.PlayAt` | 월드 좌표 고정 |
| `PlayBGM(token, ignoreSameClip)` | `bgmAudio.clip` 교체 후 `Play` | 같은 클립 재생 중이면 기본 무시 |
| `StopBGM(fadeOut)` | 볼륨 램프 후 `Stop` | `Time.unscaledDeltaTime` 기준 |

`AudioSpatialPool` 은 `ComponentPool<AudioSource>` 위에 얹은 원샷 풀이다. 반납 시점을
타이머가 아니라 **`isPlaying` 감시**로 잡기 때문에, 외부에서 `Stop` 하거나 씬이 바뀌어도
소스가 새지 않는다.

```csharp
// AddOn/AudioSpatialPool.cs:118-129
try {
    // 종료 감시 : isPlaying 기준, 강제 Stop/씬 전환에도 안전
    await UniTask.WaitUntil(() => !audio || !audio.isPlaying, PlayerLoopTiming.Update, token);
}
catch { /* 파괴/취소 시 무시 */ }
finally { if (audio) audioPool.Return(audio); }
```

---

## 볼륨

볼륨은 **믹서에 쓰고, PlayerPrefs 에서 읽는다.** 두 저장소가 분리되어 있다.

```mermaid
flowchart LR
    S["SetSfxVolume(v, save)"] --> D["_ToDecibel - log10(v)*20, 하한 -80dB"]
    D --> M["audioMix.SetFloat('SFXVolume', db)"]
    S -->|"save == true 일 때만"| P["PlayerPrefs 'Audio.SFX' = Clamp01(v)"]
    G["GetSfxVolume01()"] --> P2["PlayerPrefs 에서 읽는다"]
    P2 -.->|"⚠ 믹서 실측값이 아니다"| G
```

노출 파라미터 이름(`MasterVolume` / `SFXVolume` / `UIVolume` / `BGMVolume`)은 상수로 고정되어
있으므로, **AudioMixer 애셋에 같은 이름으로 파라미터를 Expose 해 두어야 한다**
(`AudioManager.cs:39-42`).

---

## 식별자 체계

주 키는 `string token` 이지만 **`int uid` 축도 살아 있다.** 2026-08-05 에 제거된 것은
`*.Uid.cs` 파일들뿐이고(개수는 이 레포에 삭제 이력이 없어 상류에서 확인), `AudioManager` 의 `int` 오버로드 / `AudioClipRepository._TryBuildLoadKey(int)`
/ `AudioCatalogRegistry` 의 uid 인덱스는 그대로다. uid 는 `Entry.Uid` 필드에서 오고, 그 값이 0 이면
token 앞머리에서 파싱한다. 둘이 어긋나면 오류를 남기고 **uid 인덱스에만 등록되지 않는다** -
token 축은 이미 등록돼 있어 토큰 조회는 정상 동작한다.
`AudioClips` enum 은 이 모듈에 없다. Enum Generator 가 사용처 어셈블리에 생성하고 원소 값이 곧
uid 라, 생성된 `Play(this AudioManager, AudioClips)` 확장은 `manager.Play((int)id)` 로 이어진다.

```mermaid
flowchart LR
    subgraph 저작["카탈로그 저작 - 에디터"]
    F["AudioClip 에셋"] --> G["AudioCatalogGeneratorPanel"]
    G -->|"파일명"| T["token"]
    G -->|"폴더 추론"| M["AudioMajorCategory"]
    G -->|"폴더 경로"| P2["path"]
    end
    subgraph 런타임
    T --> R["AudioClipRepository"]
    P2 --> R
    R --> L["load key"]
    M -.->|"관여하지 않는다"| L
    end
```

`AudioMajorCategory` 는 남아 있지만 **로드 경로에 들어가지 않는다.** 카탈로그 창의 정렬·검색,
생성기의 폴더 추론, 드롭다운 라벨(`AudioClipDropdownSource`), 레지스트리의 등가성 검사에 쓰인다.

## 에디터 도구 (`HCUP.HAudio.Editor`)

| 창 | 메뉴 | 용도 |
|---|---|---|
| `SoundToolsWindow` | HCUP/Audio/Sound Catalog Editor · Generator · Sound Clip Enum Generator | 아래 3개 패널을 탭으로 묶은 호스트 창 |
| ├ `AudioCatalogEditorPanel` | (Sound Catalogs 탭) | 기존 카탈로그 Entry 표시. 직접 편집은 `clip` 뿐이고 token/path 는 버튼으로 역산, major 는 읽기 전용, uid 는 미노출. **생성은 못 한다** |
| ├ `AudioCatalogGeneratorPanel` | (Catalog Generator 탭) | 폴더 스캔 → token·path·카테고리 자동 생성 |
| └ `AudioClipEnumPanel` | (Enum Generator 탭) | 카탈로그 → AudioClips enum + 재생 확장 메서드 생성 |
| `AudioClipDiagnosticsWindow` | HCUP/Audio/Sound Data Diagnostics | **Play Mode 전용.** 토큰별 로드 여부 실시간 확인 |
| `EditorAudioPreview` | (내부) | `UnityEditor.AudioUtil` 리플렉션 래퍼 - 에디터 미리듣기 |

진단 창은 `AudioManager.CreateSnapshot()`(`AudioManager.Preview.cs`)이 만든
`AudioClipManagerSnapshot` 을 표시한다. 토큰을 올리는 경로는 `string` 오버로드와 카탈로그 단위
API(`_TrackPreviewCatalog` 가 Entry 토큰을 전부 등록) 두 갈래다. `int uid` 오버로드는 아무것도
남기지 않으므로 `PlayClick()` 과 생성된 enum 확장은 목록에 나타나지 않는다.

---

## 사용 예

```csharp
// 1) 씬 진입 시 - 카탈로그 단위 preload
await AudioManager.Instance.PrewarmCatalog(uiCatalog);

// 2) 재생 - 조회만 한다. prewarm 이 끝나 있어야 한다
AudioManager.Instance.PlayUI("Click");
AudioManager.Instance.Play3D("Explosion", transform);
AudioManager.Instance.PlayBGM("Dungeon");

// 3) 씬 이탈 시 - 카탈로그 반납 (생략해도 매니저 파괴 시 회수된다)
AudioManager.Instance.ReleaseCatalog(uiCatalog);
```

오브젝트 수명에 묶고 싶으면 `SfxAgent` 를 붙이고 `SfxView` 에 카탈로그를 넣는 편이 낫다 -
`Start`/`OnDestroy` 배선과 in-flight 경쟁 방어가 이미 되어 있다.

---

## 주의할 점

읽으면서 확인한 사실들이다. 대부분은 설계 의도지만, 몇 개는 정리 대상이다.

### 계약

1. **`Play*` 는 로드하지 않는다.** prewarm 없이 호출하면 무음이고, 릴리즈 빌드에서는 로그조차
   남지 않는다 (`AudioManager._TryGetLoadedClip` 의 진단이 `#if UNITY_EDITOR`).
2. **Resources 모드는 카탈로그 등록이 필수다.** 미등록 토큰은 레지스트리 조회에 실패하고, 그 뒤를
   받아 줄 폴백이 없어 그대로 끝난다 (`AudioClipRepository._TryBuildLoadKey(string)`).
   Addressable 모드에만 토큰 직접 사용 폴백이 있다. `path` 가 비어도 등록만 되어 있으면 키는 만들어진다.
3. **`Release(token)` 은 레지스트리에 Entry 가 남아 있어야 동작한다.** 카탈로그를 먼저 내리면
   토큰→load key 번역이 실패해 `false` 를 반환한다 (`AudioClipRepository.Release(string)`).
   Addressable 모드는 토큰 폴백이 있어 카탈로그를 내린 뒤에도 성공한다.
4. **`GetXVolume01()` 은 믹서가 아니라 PlayerPrefs 를 읽는다** (`AudioManager._GetLocalMixerVolume01`).
   `SetXVolume(v, save: false)` 로 바꾸면 게터와 실제 믹서 상태가 어긋난다. 시작 시
   `_CheckPlayerPrefs` 가 prefs 값으로 믹서를 맞추므로 초기 상태는 일치한다.

### 정리 대상

5. **`bgmAltAudio` 는 직렬화만 되고 사용처가 없다** (`AudioManager` 필드 선언, 전역 grep 0건).
   크로스페이드용으로 예약된 슬롯으로 보이나(추론) 현재 코드에는 그 경로가 없다.
6. **`AudioCatalogSO.BuildAddressableLoadKey` 는 호출처가 없다.**
   `AudioClipRepository._ResolveAddressableLoadKey` 가 같은 로직을 자체 구현한다 - 둘 중 하나로 모아야 한다.
7. ~~상위 폴더 `HAudio/README.md` 는 낡았다.~~ -> 2026-09-07 해소. 그 문서의 IMPORTANT 블록이
   현행 구조를 서술하도록 갱신됐다.

### 진단이 릴리즈에서 사라지는 지점

8. `AudioCatalogRegistry` 는 `UnityEngine.Assertions.Assert` 를 광범위하게 쓴다.
   Assert 는 릴리즈에서 제거되므로, 그 뒤의 런타임 가드
   (`if (!catalog) return 0;`)가 실제 방어선이다. 다만 **`_RegisterEntry` 의 "빈 토큰" 검사에는
   런타임 가드가 없어**, 토큰이 비어 있으면 빈 문자열 키로 인덱스에 들어간다.
   조회 측(`TryGetEntry`)이 빈 토큰을 거르므로 실피해는 없고 쓰레기 항목만 남는다.

### 2026-08-05 `*.Uid.cs` 제거로 바뀐 동작

> 아래 번호는 위 "진단이 릴리즈에서 사라지는 지점" 절의 8번 다음을 잇는다.

9. **생성기의 파일명 제약이 사라졌다.** 종전에는 `{uid}_{이름}.wav` 형식만 발굴 대상이었다
   (`_TryParseUid` 가 실패하면 스킵). 이제 루트 아래 **모든 `AudioClip`** 이 대상이 된다.
   token 규칙(`파일명에서 확장자 제거`)은 그대로이므로 기존 파일의 token 값은 변하지 않는다.
10. `AudioCatalogPolicySO` 의 `UidRange` / `TryGetUidRange` / `OnValidate` 가 제거됐다.
    기존 정책 에셋의 `uidRanges` YAML 블록은 읽히지 않고 남는다 - 에셋을 한 번 저장하면 정리된다.

---

## 확장 지점

| 하고 싶은 것 | 손댈 곳 |
|---|---|
| 로더 교체 (테스트 목킹 포함) | `AudioClipRepository` 생성자의 `assetProvider` 파라미터 - 기본값 대신 주입 |
| 새 재생 채널 (예: Voice) | `AudioManager` 에 `AudioSource` 추가 + 믹서 노출 파라미터 상수 추가 |
| 카탈로그 자동 생성 규칙 변경 | `AudioCatalogPolicySO` 의 `FolderMidMapping` + 생성기의 `_InferMajor` |
| 3D 재생 기본값 (감쇠·거리) | `AudioSpatialPool` 의 `[HTitle("3D Audio Settings")]` 필드군 |
| 클릭음 정책 | `BaseSfxAddon` 상속 후 `_HandleClick` 오버라이드 |
