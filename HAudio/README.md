# HAudio - 패키지 카드

> 모듈: `HAudio/` · 소스 25파일 · `package.json` v1.0.0 (`com.hohong123.haudio`)
> 구성 어셈블리 2개
> 코드 문서: **[Runtime README](Runtime/README.md)**

---

## 이 패키지가 담는 것

**`string token` 하나로 오디오를 지목하는 재생 계층.** 토큰을 실제 에셋 경로로 번역하고 그
에셋을 누가 붙잡고 있는지 추적하는 일까지가 이 모듈의 범위이고, 실제 로드·캐시·해제는
`HResource` 의 `AssetProvider` 에 위임한다.

| 폴더 | 파일 | 담는 것 |
|---|---|---|
| `Runtime` (루트) | 3 | `AudioManager` + 진단용 partial + 스냅샷 DTO |
| `Runtime/Repository` | 2 | **token → load key 번역**. 이 모듈의 책임 경계 |
| `Runtime/Catalog` | 1 | 활성 카탈로그 집합과 토큰 인덱스, 참조 카운트 |
| `Runtime/Core` | 2 | 카탈로그 SO + 생성 정책 SO |
| `Runtime/AddOn` | 6 | 3D 원샷 풀, 오브젝트 수명 결합 에이전트, 버튼·토글 클릭음 |
| `Runtime/Enum` | 1 | 분류 라벨 (로드 경로에 관여하지 않는다) |
| `Editor` | 9 | 카탈로그 편집·생성·미리듣기·진단 창, enum 생성기, 저작 설정 |
| `Samples/Sound` | 1 | token 기반 재생 샘플. 어느 asmdef 에도 속하지 않는다 |

> [!IMPORTANT]
> 종전 이 문서는 `Runtime/New` 와 `Runtime/Legacy` 로 폴더가 이분돼 있고 구 `SoundManager`
> 가 partial 로 보존된다고 서술했다. **그런 폴더도, 그 클래스도 지금은 없다.**
> 마이그레이션은 끝났다. 다만 **`int uid` 로 지목하는 경로는 남아 있다** - 2026-08-05 에
> 사라진 것은 `*.Uid.cs` 파일들이고(개수는 이 레포에 삭제 이력이 없어 상류에서 확인), `AudioManager` 의 `int` 오버로드와
> `AudioCatalogRegistry` 의 uid 인덱스는 그대로다. `AudioClips` enum 도 이 모듈에 없을 뿐,
> Enum Generator 가 사용처 어셈블리에 생성하며 원소 값이 곧 uid 다.

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 참조 |
|---|---|---|---|
| `HCUP.HAudio` | Runtime | 15 | `UniTask`, `HCUP.HUtil`, `HCUP.HUI`, `HCUP.HCore`, `HCUP.HInspector`, `HCUP.HDiagnosis`, `HCUP.HCollection`, `HCUP.HResource` |
| `HCUP.HAudio.Editor` | Editor | 9 | `HCUP.HAudio`, `HCUP.HUtil`, `HCUP.HUI`, `HCUP.HData`, `HCUP.HDiagnosis`, `HCUP.HCore` |

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다 - 개별 UPM 설치는 현재 동작하지 않는다
([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 이 프로젝트 기준 6000.3.18f1 |
| UniTask | prewarm 의 비동기 흐름 |
| Addressables | `HResource` 경유. Resources 모드만 쓸 때도 패키지는 필요하다 |
| AudioMixer 배선 | 노출 파라미터 이름이 상수로 고정 - `MasterVolume`/`SFXVolume`/`UIVolume`/`BGMVolume` 을 믹서 에셋에 같은 이름으로 Expose 해야 한다 |

`Samples~/Sound` 에 token 기반 재생 샘플이 있다.

---

## 쓰기 전에 알아야 할 계약

1. **`Play*` 는 로드하지 않는다.** 이미 메모리에 있는 클립만 재생하고, 없으면 무음으로 끝난다
   (에디터에서만 경고가 뜬다). 로드는 `Prewarm*` 의 몫이다 - 재생 시점의 프레임 스파이크를
   구조적으로 없애는 대신 호출자에게 prewarm 책임을 지운다.
2. **해제는 소유자 단위다.** `AudioManager` 는 `Awake` 에서 `AssetOwnerId` 를 발급받고
   `OnDestroy` 에서 전부 반납한다. 개별 `ReleaseCatalog` 를 빠뜨려도 누수는 남지 않는다.
3. **Resources 모드는 카탈로그 등록이 필수다.** 미등록 토큰은 `path` 를 알 수 없어 실패한다.
   Addressable 모드에만 "토큰 = 주소" 폴백이 있다.

---

## 어디부터 볼까

1. `Runtime/AudioManager.cs` - 런타임 진입점
2. `Runtime/Repository/AudioClipRepository.cs` - **책임 경계.** 위쪽은 토큰만, 아래쪽은 load key 만 안다
3. `Runtime/Catalog/AudioCatalogRegistry.cs` - 토큰 인덱스와 참조 카운트
4. `Samples~/Sound`

---

## 주의할 점

1. **`GetXVolume01()` 은 믹서가 아니라 PlayerPrefs 를 읽는다.** `SetXVolume(v, save: false)` 로
   바꾸면 게터와 실제 믹서 상태가 어긋난다.
2. **`bgmAltAudio` 는 직렬화만 되고 사용처가 없다.** 크로스페이드용으로 예약된 슬롯으로 보이나
   현재 코드에 그 경로가 없다 (추론).
3. **`AudioCatalogSO.BuildAddressableLoadKey` 는 호출처가 0건이다.** 같은 로직을
   `AudioClipRepository` 가 자체 구현한다 - 둘 중 하나로 모아야 한다.

근거 라인은 [Runtime README](Runtime/README.md) 의 "주의할 점" 절에 있다.
