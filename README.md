# Hong's Custom Utility - Unity (HCUP)

> 공용 유틸리티, UI, 게임 로직 패키지를 함께 관리하는 Unity 패키지 저장소입니다. (업데이트: 2026-04-01, 문서 기준 버전 1.0.0)

---

## 1.0.0 릴리스 하이라이트

- `HUtil`은 Addressables/Resources 기반 `AssetHandler`, 데이터 로드/캐시/구독, 커스텀 인스펙터 보조 계층까지 포함하는 공용 기반 패키지로 확장됐습니다.
- `HGame`은 카탈로그 기반 사운드 시스템, `GameModule`, `Player`, `Skill`, `World`, `Camera`, `2D` 계층을 중심으로 게임 로직 패키지 구조를 강화했습니다.
- `HUI`는 버튼, 토글, 드롭다운, 팝업, 패널, 재활용 스크롤뷰, 디버그 콘솔까지 포함하는 UI 패키지로 확장됐습니다.
- 각 패키지는 샘플 씬과 프리팹을 함께 제공하며, 실제 사용 흐름을 `Samples~` 폴더 기준으로 확인할 수 있습니다.
- 패키지별 README와 CHANGELOG를 최신 구조에 맞게 정리해 도입 순서와 주의점을 바로 파악할 수 있도록 갱신했습니다.

---

## 개요

이 저장소는 반복 구현되던 Unity 기능을 세 계층으로 분리해 재사용하기 쉽게 정리한 패키지 묶음입니다.

- `HUtil`: 공용 기반 계층
- `HUI`: UI 계층
- `HGame`: 게임 로직 계층

구조상 `HGame`과 `HUI`는 `HUtil`을 기반으로 사용합니다. 범용 엔진을 지향한 저장소가 아니라, 실제 프로젝트에서 반복 사용된 구조를 유지보수하기 쉽게 모듈화한 저장소에 가깝습니다.

---

## 패키지 구성

### HUtil
- 역할: 에셋 로드, 캐시, 데이터, 풀링, 씬, 로거, 인스펙터, 시간, 문자열/수학 유틸 같은 공용 기반 기능을 담당합니다.
- 런타임 코드 수: `123`개 `.cs`
- 샘플 코드 수: `4`개 `.cs`
- 핵심 축: `AssetHandler`, `Data`, `Pooling`, `Scene`, `Inspector`, `Logger`
- 문서: `HoHong123/HUtil/README.md`, `HoHong123/HUtil/docs/CHANGELOG.md`

### HUI
- 역할: 버튼, 토글, 드롭다운, 팝업, 패널, 재활용 스크롤뷰, 디버그 콘솔 등 UI 계층을 담당합니다.
- 런타임 코드 수: `57`개 `.cs`
- 샘플 코드 수: `8`개 `.cs`
- 핵심 축: `Scrollview`, `Popup`, `DropDown`, `DebugConsole`, `Button`, `Toggle`
- 문서: `HoHong123/HUI/README.md`, `HoHong123/HUI/docs/CHANGELOG.md`

### HGame
- 역할: 게임 진행 흐름, 사운드, 플레이어, 스킬, 월드, 카메라, 2D 관련 게임 로직 계층을 담당합니다.
- 런타임 코드 수: `75`개 `.cs`
- 샘플 코드 수: `15`개 `.cs`
- 핵심 축: `Sound`, `GameModule`, `Player`, `Skill`, `World`, `Camera`, `2D`
- 문서: `HoHong123/HGame/README.md`, `HoHong123/HGame/doc/CHANGELOG.md`

---

## 디렉토리 맵

### `HoHong123/HUtil`
- `Runtime/HUtil`
  - `Animation(15)`, `AssetHandler(28)`, `Collection(4)`, `Core(2)`, `Data(26)`, `Debug(2)`, `Encode(2)`, `Encrypt(2)`, `Font(1)`, `Inspector(12)`, `Logger(2)`, `Mathx(1)`, `Pooling(9)`, `Primitives(4)`, `Scene(7)`, `Time(3)`, `Web(3)`
- `Editor`
  - Addressables 이름 정리, Owner 추적, 커스텀 인스펙터 보조 기능
- `Samples~`
  - `AddressableSequence`, `OwnerTracking`, `SceneUtil`

### `HoHong123/HUI`
- `Runtime/HUI`
  - `Button(8)`, `DebugConsole(6)`, `DropDown(6)`, `Entity(6)`, `Graphic(1)`, `Image(1)`, `Panel(3)`, `Popup(7)`, `Scrollview(10)`, `Spinner(2)`, `Toggle(6)`
- `Editor`
  - `HImage` 인스펙터 확장
- `Samples~`
  - `Button`, `Console`, `Dropdown`, `Popup`, `Scrollview`

### `HoHong123/HGame`
- `Runtime/HGame`
  - `2D(9)`, `Audio(10)`, `Camera(6)`, `Character(3)`, `GameModule(6)`, `Player(6)`, `Skill(8)`, `Sound(20)`, `World(7)`
- `Editor`
  - 사운드 카탈로그 생성, 편집, 미리보기, 디버깅 도구
- `Samples~`
  - `GameModule`, `Player`, `Skill`, `Sound`, `World2D`

---

## 기술 전제

- Unity `2021.3+`
- `UniTask`
- `Unity Addressables`
- `Unity ResourceManager`
- `TextMeshPro`
- `DOTween.Modules`
- 선택 의존: `Odin Inspector`, `DoTween`

패키지별 실제 의존성은 각 asmdef와 package 문서를 기준으로 다시 확인하는 편이 안전합니다.

---

## 사용 순서

1. 공용 기반이 필요한 경우 `HUtil`부터 확인하십시오.
2. UI 작업이 목적이면 `HUI`를 확인하고, `Scrollview`와 `Popup` 샘플부터 보는 편이 빠릅니다.
3. 게임 진행 흐름이나 사운드 구조가 목적이면 `HGame`의 `GameModule`과 `Sound`부터 확인하십시오.
4. 샘플은 참고용입니다. 실제 프로젝트에는 그대로 복사하지 말고 입력 체계, 네임스페이스, 초기화 순서에 맞게 다시 감싸서 넣으십시오.

---

## 주의 사항

- 이 저장소는 편의상 직접 호출하는 구조보다, 계층을 나눠 책임을 분리하는 쪽에 무게가 실려 있습니다. 구조를 무시하고 바로 접근하면 장점이 사라집니다.
- 에셋 로드와 해제는 `HUtil`의 Provider/Lease 흐름을 무시하면 다시 꼬입니다.
- UI 계층은 빈번한 갱신 시 `Canvas Rebuild/Repaint` 비용이 커집니다. 성능 문제는 감으로 보지 말고 프로파일링해야 합니다.
- 게임 계층은 사운드, 상태 전환, 샘플 흐름이 서로 얽혀 있으므로 초기화 순서를 무시하면 바로 불안정해집니다.
- `Samples~`는 참고용입니다. 실제 배포 빌드에는 포함하지 않거나 별도 패키지로 분리하는 편이 맞습니다.

---

## UPM 설치 경로

- `HUtil`
  - `https://github.com/HoHong123/HCUP-Unity.git?path=/HoHong123/HUtil`
  - `https://github.com/HoHong123/HCUP-Unity.git?path=/HoHong123/HUtil#v{목표버전}`
- `HUI`
  - `https://github.com/HoHong123/HCUP-Unity.git?path=/HoHong123/HUI`
  - `https://github.com/HoHong123/HCUP-Unity.git?path=/HoHong123/HUI#v{목표버전}`
- `HGame`
  - `https://github.com/HoHong123/HCUP-Unity.git?path=/HoHong123/HGame`
  - `https://github.com/HoHong123/HCUP-Unity.git?path=/HoHong123/HGame#v{목표버전}`

---

## 참고 문서

- `HoHong123/HUtil/README.md`
- `HoHong123/HUI/README.md`
- `HoHong123/HGame/README.md`
- `HoHong123/HUtil/docs/CHANGELOG.md`
- `HoHong123/HUI/docs/CHANGELOG.md`
- `HoHong123/HGame/doc/CHANGELOG.md`

---

## 문의

- GitHub: `https://github.com/HoHong123`
- 특정 패키지나 폴더를 지정하면, 해당 영역 기준으로 더 세부적인 문서 정리나 사용 예시 정리는 바로 이어서 할 수 있습니다.
