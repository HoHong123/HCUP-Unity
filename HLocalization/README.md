# HLocalization - 패키지 카드

> 모듈: `HLocalization/` · 소스 7파일 · **독립 패키지 2개를 담는 우산 폴더**
> 코드 문서: **[HcupLocalization Runtime](HcupLocalization/Runtime/README.md)** · **[HUnityLocalization Editor](HUnityLocalization/Editor/README.md)** · **[HUnityLocalization Runtime](HUnityLocalization/Runtime/README.md)**

---

## 이 폴더가 담는 것

로컬라이제이션 **두 갈래**가 나란히 있다. 하나를 고르는 것이지 둘을 같이 쓰는 구조가 아니다.

| | `HcupLocalization` | `HUnityLocalization` |
|---|---|---|
| 성격 | 자체 구현 런타임 | Unity Localization **연동 임포터 + 런타임 전환** |
| 범위 | Runtime | **Editor + Runtime** |
| 소스 | 3 | 4 |
| `package.json` | v1.0.0 `com.hohong123.hcuplocalization` | v1.0.0 `com.hohong123.hunitylocalization` |
| 런타임에 무엇이 도나 | `LocalizationManager` + `HTextLocalizer` 델리게이트 | `UnityLocalizationManager` (언어 전환 + 토큰 조회). UI 텍스트는 Unity 네이티브 `LocalizeStringEvent` |
| 조건부 컴파일 | 없음 | `HCUP_UNITY_LOCALIZATION` |

**둘은 완전히 분리돼 있지 않다.** `LocalizationLanguage` enum 의 소유권이 `HcupLocalization`
쪽에 있고 `HUnityLocalization` 이 그것을 참조한다. 즉 Unity Localization 만 쓰더라도
`HcupLocalization` 어셈블리는 프로젝트에 남아 있어야 한다.

### 어느 쪽을 고르나

- **`HcupLocalization`** - Unity Localization 패키지를 들이지 않고 가볍게 끝내고 싶을 때.
  런타임 텍스트 교체가 `HTextLocalizer.GetText` 델리게이트 하나로 끝난다.
- **`HUnityLocalization`** - 이미 Unity Localization 을 쓰고 있고, 엑셀 시트에서 테이블을
  채우고 싶을 때. `HExcel` 을 경유해 임포트하고, 런타임 전환과 코드용 토큰 조회는
  `UnityLocalizationManager` 가 담당한다.

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 참조 |
|---|---|---|---|
| `HCUP.HcupLocalization` | Runtime | 3 | `UniTask(.Addressables)`, `Unity.Addressables`, `Unity.ResourceManager`, `HCUP.HCore`, `HCUP.HUtil`, `HCUP.HCollection`, `HCUP.HDiagnosis`, `HCUP.HUI`, `HCUP.HResource` |
| `HCUP.HUnityLocalization` | Runtime (`HCUP_UNITY_LOCALIZATION`) | 2 | `UniTask`, `Unity.Localization`, `Unity.ResourceManager`, `HCUP.HCore`, `HCUP.HDiagnosis`, `HCUP.HInspector`, `HCUP.HcupLocalization` |
| `HCUP.HUnityLocalization.Editor` | Editor (`HCUP_UNITY_LOCALIZATION`) | 2 | `HCUP.HExcel.Editor`, `HCUP.HUnityLocalization`, `HCUP.HcupLocalization`, `HCUP.HDiagnosis`, `Unity.Localization(.Editor)`, `Unity.TextMeshPro` |

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다 - 개별 UPM 설치는 현재 동작하지 않는다
([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 최소 2021.3 (두 `package.json` 의 `unity` 필드). 6000.3 에서 개발·검증 |
| Addressables / UniTask | `HcupLocalization` 의 테이블 로드 |
| Unity Localization | `HUnityLocalization` 두 어셈블리 모두. 없으면 심볼이 정의되지 않아 양쪽이 컴파일 대상에서 빠진다 |
| NPOI | `HUnityLocalization.Editor` 가 `HExcel` 을 경유하므로 간접 필요 |

---

## 주의할 점

1. **`HCUP_UNITY_LOCALIZATION` 을 정의하지 않으면 `HUnityLocalization` 은 존재하지 않는 것과 같다.**
   임포터가 동작하지 않는데 원인을 못 찾는 경우 여기부터 확인한다.
2. **두 매니저는 PlayerPrefs 키가 다르다.** `LocalizationManager` 는 `LocalizationManager.Language`, `UnityLocalizationManager` 는 `UnityLocalizationManager.Language` 에 저장한다. 갈래를 바꾸면 저장된 언어 선택이 이어지지 않는다.

근거 라인은 각 어셈블리 README 의 "주의할 점" 절에 있다.

---

## 히스토리

### 2026-08-07 :: `LocalizationManager` 재호출 누수·중복 인스턴스 파괴 오염 수정

- 이전: 이 카드의 "주의할 점" 에 두 결함이 있었다. `InitializeAsync` 를 두 번 부르면 이전 provider 가 점유한 에셋이 반납 없이 참조를 잃었고, `OnDestroy` 가 중복 인스턴스 파괴 경로에서도 `HTextLocalizer.GetText = null` 을 실행해 본 인스턴스의 델리게이트를 끊었다.
- 현재: `InitializeAsync` 는 provider 재생성 전에 기존 provider 를 `Dispose` 하고, `OnDestroy` 는 `instance == this` 인 소유자일 때만 델리게이트와 provider 를 정리한다.
