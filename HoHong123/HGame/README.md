# HGame 시스템 요약

----

`HGame`은 게임 로직 계층을 담당하는 패키지입니다.

공용 유틸리티 위에 바로 게임 진행 흐름, 사운드 관리, 플레이어/스킬/월드 관련 기능을 얹는 구조로 되어 있습니다. 이 저장소에서는 `GameModule`과 `Audio/Sound Management System`을 함께 확인하기 위한 핵심 패키지입니다.

## 1.0.0에서 변경된 점

- 사운드 시스템을 사실상 새 축으로 확장했습니다.
- SoundManager, AudioClipProvider, SoundCatalogSO, SoundCatalogRegistry, Repository 계층을 추가했습니다.
- 레거시 사운드 API와 신규 카탈로그 구조를 함께 유지해 전환 여지를 남겼습니다.
- 사운드 카탈로그 생성/편집/디버깅용 에디터 윈도우를 추가했습니다.
- 2D 맵 경계, 미니맵, 카메라 바운더리, 월드 이벤트 포인트 구조를 보강했습니다.
- GameModule, Player, Skill 도메인 구조를 보강하고 샘플 연동을 확장했습니다.
- Player, Skill, Sound, World2D, GameModule 샘플 씬과 에셋을 대폭 추가했습니다.

## 디렉토리 구성

### `Runtime`
- 게임 실행 흐름과 직접 연결되는 로직이 들어 있습니다.
- `GameModule`, `Sound`, `Player`, `Skill`, `Camera`, `World`, `2D` 영역으로 나뉩니다.

### `Editor`
- 사운드 카탈로그 편집과 미리보기 같은 에디터 보조 기능이 들어 있습니다.

### `Samples~`
- `GameModule`, `Skill`, `Sound`, `Player`, `World2D` 샘플이 포함되어 있습니다.
- 기능별 사용 예제를 확인할 수 있습니다.

## 중점적으로 봐야 할 부분

- `GameManager`와 `BaseGameModule` 기반 단계 전환 구조
- 기능별 모듈 분리를 통한 흐름 제어
- `SoundManager` 중심의 사운드 로딩, 재생, 볼륨 제어 구조
- 샘플을 통한 실제 패키지 사용 방식

## 추천 확인 순서

1. `Runtime/HGame/GameModule/GameManager.cs`
2. `Runtime/HGame/GameModule/BaseGameModule.cs`
3. `Runtime/HGame/Sound/SoundManager.cs`
4. `Editor/Sound/SoundCatalogEditorWindow.cs`
5. `Samples~/GameModule`
6. `Samples~/Sound`

## 기술 전제

- Unity 2021.3+
- UniTask
- `HUtil`, `HUI` 패키지 의존

## 주의점

- `HGame`은 장르 불문 범용 엔진이 아니라, 반복 사용된 게임 제작 패턴을 정리한 패키지입니다.
- 따라서 구조는 일반론보다 실제 프로젝트에서 관리하기 쉬운 쪽으로 치우쳐 있습니다.


# 패키지 구성

----

## 폴더 맵 (Runtime)

- 2D(9): 패럴랙스, 맵/미니맵, 2D 유틸.
- Camera(6): 카메라 추적/영역 제어.
- Character(3): 캐릭터 입력·상태 제어.
- GameModule(5): 씬/게임 단계 관리 베이스 모듈.
- Player(6): 플레이어 제어/입력/인터랙션.
- Skill(8): 스킬/쿨타임/효과 처리.
- World(7): 월드/스폰/웨이브 관리.

## 업그레이드/사용 가이드

- 씬/상태 전환 시 비동기 흐름이 중복 호출되지 않도록 직렬화하거나 토큰을 관리하십시오.
- 샘플 코드를 참고하되, 실제 빌드에서는 `Samples~`를 제외하거나 별도 패키지로 분리합니다.
- 캐릭터/스킬/플레이어 입력은 프로젝트 입력 시스템에 맞춰 래핑해 사용합니다.

## UPM 설치 경로 (HCUP-Unity)
- `https://github.com/HoHong123/HCUP-Unity.git?path=/HoHong123/HGame`
- `https://github.com/HoHong123/HCUP-Unity.git?path=/HoHong123/HGame#v{목표버전}`
