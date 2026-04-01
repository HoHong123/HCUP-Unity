# 개인 Unity 패키지 모음

이 디렉토리는 프로젝트에서 재사용하기 위해 분리한 개인 Unity 패키지 모음입니다.

완성 게임 로직이 아니라, 공용 기반 기능과 재사용 가능한 시스템을 `HUtil`, `HGame`, `HUI`로 나누어 관리하는 구조입니다. 이 저장소에서 공통 설계 방향을 보려면 가장 먼저 확인해야 하는 디렉토리입니다.

## 구성

### `HUtil`
- 공용 유틸리티 계층입니다.
- AssetHandler, Data Load/Save, Pooling, Scene, Logger, Inspector Attribute 등 기반 시스템을 포함합니다.

### `HGame`
- 게임 로직 계층입니다.
- GameModule, Sound, Skill, Player, Camera, World 관련 기능을 포함합니다.

### `HUI`
- UI 로직 계층입니다.
- Popup, Scrollview, Dropdown, Button, Spinner, DebugConsole 등 재사용 UI 시스템을 포함합니다.

## 추천 확인 순서

1. `HUtil`
2. `HGame`
3. `HUI`

## 중점적으로 봐야 할 부분

- 공용 기능을 패키지 단위로 분리한 구조
- `asmdef` 기반 의존성 경계
- Runtime / Editor / Samples 분리
- 하나의 프로젝트 전용 코드가 아니라 재사용 가능한 형태로 추상화하려는 방향

## 주의점

- 이 디렉토리의 코드는 독립 패키지 관점으로 정리되어 있지만, 일부 예제는 원본 프로젝트 의존성을 그대로 가질 수 있습니다.
- 따라서 문서를 볼 때는 빌드 완결성보다 책임 분리와 구조적 의도를 중심으로 확인하는 편이 맞습니다.
