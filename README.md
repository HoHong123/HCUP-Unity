# Hong's Custom Utility - Unity (HCUP)

> 공용 유틸리티, UI, 게임 로직 패키지를 함께 관리하는 Unity 패키지 저장소입니다. (업데이트: 2026-04-27, 문서 기준 버전 1.0.2)

---

## 1.0.2 릴리스 하이라이트

- 어셈블리 식별자 prefix 를 `HCUP.*` 로 통일했습니다 (이전: `Hohong123.*`).
- 패키지 디렉토리 구조를 평탄화했습니다 (`HoHong123/X` → `X`). HoHong123 컨테이너 폴더가 제거되었습니다.
- 1.0.1 어셈블리 분리 — HUtil 에 묶여 있던 `HCollection`, `HCore`, `HData`, `HDiagnosis`, `HInspector` 5개 도메인이 별도 어셈블리로 분리되었습니다.
- 1.0.2 Custom Inspector 치환 — Sound/Skill/Camera/World/Game 계층 33+ 클래스를 Odin → HInspector 로 치환했습니다.
- HCollection.Odin.Editor 어셈블리 신설 — HDictionary 의 커스텀 드로어가 Odin 환경에서 강제됩니다.
- AssetHandler / HUI.Scrollview §11 헤더 형틀 정리 — Cache/Data/Load/Provider/Store/Subscription/Validation + Scrollview 핵심 클래스 31개 일관 적용.
- Popup/SoundManager 등 생명주기 누수 4건 수정.
- HUtil/HUI/HGame 메인 패키지 1.0.0 → 1.0.2.

---

## 개요

이 저장소는 반복 구현되던 Unity 기능을 세 메인 패키지 + 다섯 어셈블리로 분리해 재사용하기 쉽게 정리한 묶음입니다.

### 메인 UPM 패키지 (3개)
- `HUtil`: 공용 기반 계층 (AssetHandler / Pooling / Animation / Font)
- `HUI`: UI 계층 (Button / Toggle / DropDown / Popup / Panel / Scrollview / DebugConsole)
- `HGame`: 게임 로직 계층 (GameModule / Sound / Player / Skill / World / Camera / 2D / Character)

### 분리 어셈블리 (5개, sibling)
- `HCollection`: HDictionary / IHDictionary / EnumArray / CircularList
- `HCore`: SingletonBehaviour / Scene 관리 / TransformExtention / CooldownTimer
- `HData`: Encode / Encrypt / Mathx / Primitives 보조
- `HDiagnosis`: HLogger / HDebug / ComponentActivationWatcher
- `HInspector`: 커스텀 IMGUI 인스펙터 attribute 군 (HTitle / HShowIf / HListDrawer / HRequired / HButton 등 20+)

구조상 `HGame`과 `HUI`는 `HUtil` 및 위 어셈블리들을 기반으로 사용합니다. 범용 엔진을 지향한 저장소가 아니라, 실제 프로젝트에서 반복 사용된 구조를 유지보수하기 쉽게 모듈화한 저장소에 가깝습니다.

---

## 패키지 구성

### HUtil
- 역할: 에셋 로드, 캐시, 풀링, 애니메이션 라우터, 폰트 보조 등 공용 기반 기능을 담당합니다.
- 핵심 축: `AssetHandler`, `Animation`, `Pooling`, `Font`, (선택) `Odin`, (선택) `Tween`
- 문서: `HUtil/README.md`, `HUtil/docs/CHANGELOG.md`

### HUI
- 역할: 버튼, 토글, 드롭다운, 팝업, 패널, 재활용 스크롤뷰, 디버그 콘솔 등 UI 계층을 담당합니다.
- 핵심 축: `Scrollview`, `Popup`, `DropDown`, `DebugConsole`, `Button`, `Toggle`
- 문서: `HUI/README.md`, `HUI/docs/CHANGELOG.md`

### HGame
- 역할: 게임 진행 흐름, 사운드, 플레이어, 스킬, 월드, 카메라, 2D 관련 게임 로직 계층을 담당합니다.
- 핵심 축: `Sound`, `GameModule`, `Player`, `Skill`, `World`, `Camera`, `2D`
- 문서: `HGame/README.md`, `HGame/doc/CHANGELOG.md`

---

## 디렉토리 맵

### `HUtil` (UPM 패키지)
- `Runtime/HUtil`
  - `Animation(15)`, `AssetHandler(28)`, `Data(26)`, `Font(1)`, `Pooling(9)`
- `Runtime/Odin`, `Runtime/Tween` (선택 의존, defineConstraints gate)
- `Editor`: Addressables 이름 정리, Owner 추적, 커스텀 인스펙터 보조
- `Samples~`: `AddressableSequence`, `OwnerTracking`, `SceneUtil`

### `HUI` (UPM 패키지)
- `Runtime/HUI`
  - `Button(8)`, `DebugConsole(6)`, `DropDown(6)`, `Entity(6)`, `Graphic(1)`, `Image(1)`, `Panel(3)`, `Popup(7)`, `Scrollview(10)`, `Spinner(2)`, `Toggle(6)`
- `Editor`: `HImage` 인스펙터 확장
- `Samples~`: `Button`, `Console`, `Dropdown`, `Popup`, `Scrollview`

### `HGame` (UPM 패키지)
- `Runtime/HGame`
  - `2D(9)`, `Audio(10)`, `Camera(6)`, `Character(3)`, `GameModule(6)`, `Player(6)`, `Skill(8)`, `Sound(20)`, `World(7)`
- `Editor`: 사운드 카탈로그 생성/편집/미리보기/디버깅 도구
- `Samples~`: `GameModule`, `Player`, `Skill`, `Sound`, `World2D`

### `HCollection` / `HCore` / `HData` / `HDiagnosis` / `HInspector` (sibling 어셈블리)
- 별도 UPM 패키지가 아니라 같은 repo 안의 sibling 폴더입니다.
- 메인 패키지를 import 시 같은 sub-tree 안에 함께 가져갑니다.
- 자세한 폴더 구성은 각 폴더의 .asmdef 와 README/doc 을 참조하십시오.

---

## 기술 전제

- Unity `2021.3+`
- `UniTask`, `UniTask.Addressables`
- `Unity Addressables`, `Unity ResourceManager`
- `TextMeshPro`
- 선택 의존: `Odin Inspector` (defineConstraints `ODIN_INSPECTOR`), `DOTween` (defineConstraints `USE_DOTWEEN`)

패키지별 실제 의존성은 각 asmdef 와 package 문서를 기준으로 다시 확인하는 편이 안전합니다.

---

## 사용 순서

1. 공용 기반이 필요한 경우 `HUtil` 부터 확인하십시오.
2. UI 작업이 목적이면 `HUI` 를 확인하고, `Scrollview` 와 `Popup` 샘플부터 보는 편이 빠릅니다.
3. 게임 진행 흐름이나 사운드 구조가 목적이면 `HGame` 의 `GameModule` 과 `Sound` 부터 확인하십시오.
4. 샘플은 참고용입니다. 실제 프로젝트에는 그대로 복사하지 말고 입력 체계, 네임스페이스, 초기화 순서에 맞게 다시 감싸서 넣으십시오.

---

## 1.0.0 / 1.0.1 → 1.0.2 마이그레이션

- consumer 측 `.asmdef` 의 `references` 필드에서 `Hohong123.X` 를 `HCUP.X` 로 일괄 치환하십시오.
- prefab/scene/asset 의 yaml 안에 박힌 `Hohong123.X::...` 또는 `..., Hohong123.X]]` 패턴도 함께 갱신해야 type lookup 이 동작합니다.
- import 경로가 `Assets/.../HoHong123/X/...` 였다면 `Assets/.../X/...` 로 갱신해야 합니다.
- `package.json` 의 `name`(com.hohong123.*) 과 `displayName` 은 UPM 식별자 호환을 위해 유지되었습니다.

---

## 주의 사항

- 이 저장소는 편의상 직접 호출하는 구조보다, 계층을 나눠 책임을 분리하는 쪽에 무게가 실려 있습니다. 구조를 무시하고 바로 접근하면 장점이 사라집니다.
- 에셋 로드와 해제는 `HUtil` 의 Provider/Lease 흐름을 무시하면 다시 꼬입니다.
- UI 계층은 빈번한 갱신 시 `Canvas Rebuild/Repaint` 비용이 커집니다. 성능 문제는 감으로 보지 말고 프로파일링해야 합니다.
- 게임 계층은 사운드, 상태 전환, 샘플 흐름이 서로 얽혀 있으므로 초기화 순서를 무시하면 바로 불안정해집니다.
- `Samples~` 는 참고용입니다. 실제 배포 빌드에는 포함하지 않거나 별도 패키지로 분리하는 편이 맞습니다.
- HWindows 는 1.1.0 에서 도입 예정이며 이번 1.0.2 릴리즈에는 포함되지 않습니다.

---

## UPM 설치 경로

- `HUtil`
  - `https://github.com/HoHong123/HCUP-Unity.git?path=/HUtil`
  - `https://github.com/HoHong123/HCUP-Unity.git?path=/HUtil#HUtil-1.0.2`
- `HUI`
  - `https://github.com/HoHong123/HCUP-Unity.git?path=/HUI`
  - `https://github.com/HoHong123/HCUP-Unity.git?path=/HUI#HUI-1.0.2`
- `HGame`
  - `https://github.com/HoHong123/HCUP-Unity.git?path=/HGame`
  - `https://github.com/HoHong123/HCUP-Unity.git?path=/HGame#HGame-1.0.2`

태그 컨벤션은 `{어셈블리}-{버전}` 형식입니다 (예: `HUtil-1.0.2`, `v1.0.2` umbrella). 분리 어셈블리도 동일 패턴(`HCollection-1.0.2` 등)으로 태그가 부여되어 있습니다.

---

## 참고 문서

- 패키지 README: `HUtil/README.md`, `HUI/README.md`, `HGame/README.md`
- 패키지 CHANGELOG: `HUtil/docs/CHANGELOG.md`, `HUI/docs/CHANGELOG.md`, `HGame/doc/CHANGELOG.md`
- 릴리즈 노트: `docs/ReleaseNote/v1.0.2.md`, `docs/ReleaseNote/HUtil-1.0.2.md` 등 9개 파일
- 릴리즈 워크플로우: `RELEASE_WORKFLOW.md`

---

## 문의

- GitHub: `https://github.com/HoHong123`
- 특정 패키지나 폴더를 지정하면, 해당 영역 기준으로 더 세부적인 문서 정리나 사용 예시 정리는 바로 이어서 할 수 있습니다.
