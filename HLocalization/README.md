# HLocalization — 패키지 카드

> 모듈: `HLocalization/` · 소스 5파일 · **독립 패키지 2개를 담는 우산 폴더**
> 코드 문서: **[HcupLocalization Runtime](HcupLocalization/Runtime/README.md)** · **[HUnityLocalization Editor](HUnityLocalization/Editor/README.md)**

---

## 이 폴더가 담는 것

로컬라이제이션 **두 갈래**가 나란히 있다. 하나를 고르는 것이지 둘을 같이 쓰는 구조가 아니다.

| | `HcupLocalization` | `HUnityLocalization` |
|---|---|---|
| 성격 | 자체 구현 런타임 | Unity Localization **연동 임포터** |
| 범위 | Runtime | **Editor 전용** |
| 소스 | 3 | 2 |
| `package.json` | v1.0.0 `com.hohong123.hcuplocalization` | v1.0.0 `com.hohong123.hunitylocalization` |
| 런타임에 무엇이 도나 | `LocalizationManager` + `HTextLocalizer` 델리게이트 | 없다 — 런타임은 Unity 네이티브 API 가 담당 |
| 조건부 컴파일 | 없음 | `HCUP_UNITY_LOCALIZATION` |

**둘은 완전히 분리돼 있지 않다.** `LocalizationLanguage` enum 의 소유권이 `HcupLocalization`
쪽에 있고 `HUnityLocalization` 이 그것을 참조한다. 즉 Unity Localization 만 쓰더라도
`HcupLocalization` 어셈블리는 프로젝트에 남아 있어야 한다.

### 어느 쪽을 고르나

- **`HcupLocalization`** — Unity Localization 패키지를 들이지 않고 가볍게 끝내고 싶을 때.
  런타임 텍스트 교체가 `HTextLocalizer.GetText` 델리게이트 하나로 끝난다.
- **`HUnityLocalization`** — 이미 Unity Localization 을 쓰고 있고, 엑셀 시트에서 테이블을
  채우고 싶을 때. `HExcel` 을 경유해 임포트한다.

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 참조 |
|---|---|---|---|
| `HCUP.HcupLocalization` | Runtime | 3 | `UniTask(.Addressables)`, `Unity.Addressables`, `Unity.ResourceManager`, `HCUP.HCore`, `HCUP.HUtil`, `HCUP.HCollection`, `HCUP.HDiagnosis`, `HCUP.HUI`, `HCUP.HResource` |
| `HCUP.HUnityLocalization.Editor` | Editor (`HCUP_UNITY_LOCALIZATION`) | 2 | `HCUP.HExcel.Editor`, `HCUP.HcupLocalization`, `HCUP.HDiagnosis`, `Unity.Localization(.Editor)` |

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다 — 개별 UPM 설치는 현재 동작하지 않는다
([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 이 프로젝트 기준 6000.3.18f1 |
| Addressables / UniTask | `HcupLocalization` 의 테이블 로드 |
| Unity Localization | `HUnityLocalization` 만. 없으면 심볼을 정의하지 않으면 된다 |
| NPOI | `HUnityLocalization` 이 `HExcel` 을 경유하므로 간접 필요 |

---

## 주의할 점

1. **`LocalizationManager.InitializeAsync` 를 두 번 부르면 provider 가 샌다.** 가드 없이
   `AssetProviderFactory.CreateAddressable` 을 다시 호출해, 이전 provider 가 점유한 에셋이
   반납 없이 참조를 잃는다.
2. **`OnDestroy` 가 무조건 `HTextLocalizer.GetText = null` 을 실행한다.** `SingletonBehaviour`
   의 중복 인스턴스 파괴 경로에서도 실행되면, 살아 있는 본 인스턴스의 델리게이트가 끊긴다.
   `instance != this` 가드가 필요한 자리다.
3. **`HCUP_UNITY_LOCALIZATION` 을 정의하지 않으면 `HUnityLocalization` 은 존재하지 않는 것과 같다.**
   임포터가 동작하지 않는데 원인을 못 찾는 경우 여기부터 확인한다.

근거 라인은 각 어셈블리 README 의 "정리 대상" 절에 있다.
