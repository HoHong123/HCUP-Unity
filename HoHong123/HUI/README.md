# HUI 시스템 요약

----

`HUI`는 UI 로직 계층을 담당하는 패키지입니다.

공용 유틸리티 위에 버튼, 토글, 드롭다운, 팝업, 패널, 재활용 스크롤뷰, 디버그 콘솔 같은 UI 구성요소를 얹는 구조로 되어 있습니다. 이 저장소에서는 범용 UI 컴포넌트와 샘플 프리팹/씬을 함께 확인하기 위한 핵심 패키지입니다.

## 1.0.0에서 변경된 점

- 버튼, 토글, 드롭다운, 팝업, 패널, 스크롤뷰 계층을 전반적으로 확장했습니다.
- `HLogConsole` 기반 디버그 콘솔 UI를 추가해 런타임 로그 표시 흐름을 보강했습니다.
- `BaseRecycleView`, `GridRecycleView`, `SpanningGridRecycleView`, `VlgRecycleView` 등 재활용 스크롤뷰 구성을 추가했습니다.
- `HImage`와 전용 에디터(`HImageEditor`)를 추가해 UI 이미지 제어와 에디터 편의성을 보강했습니다.
- 버튼/토글 계열에 상태 변화용 이펙트 컴포넌트를 추가했습니다.
- `Button`, `Console`, `Dropdown`, `Popup`, `Scrollview` 샘플을 추가해 사용 예제를 정리했습니다.

## 디렉토리 구성

### `Runtime`
- 게임 실행 중 직접 사용하는 UI 런타임 로직이 들어 있습니다.
- `Button`, `Toggle`, `DropDown`, `Popup`, `Panel`, `Scrollview`, `DebugConsole`, `Image`, `Entity`, `Graphic`, `Spinner` 영역으로 나뉩니다.

### `Editor`
- `HImage` 관련 인스펙터 확장처럼 UI 편집을 보조하는 기능이 들어 있습니다.

### `Samples~`
- `Button`, `Console`, `Dropdown`, `Popup`, `Scrollview` 샘플이 포함되어 있습니다.
- 실제 프리팹 구성과 사용 예제를 씬 단위로 확인할 수 있습니다.

## 중점적으로 봐야 할 부분

- `BaseRecycleView` 기반 재활용 스크롤 구조
- `PopupManager` 중심의 팝업 표시/관리 흐름
- `BaseDropDown`과 `HDropDown` 기반 옵션 선택 구조
- `HLogConsole` 중심의 런타임 로그 표시 방식
- 버튼/토글 이펙트 컴포넌트를 통한 상태 표현 방식

## 추천 확인 순서

1. `Runtime/HUI/Scrollview/BaseRecycleView.cs`
2. `Runtime/HUI/Scrollview/SpanningGridRecycleView.cs`
3. `Runtime/HUI/Popup/PopupManager.cs`
4. `Runtime/HUI/DropDown/BaseDropDown.cs`
5. `Runtime/HUI/DebugConsole/HLogConsole.cs`
6. `Samples~/Scrollview`
7. `Samples~/Popup`

## 기술 전제

- Unity 2021.3+
- UniTask
- TextMeshPro
- DOTween.Modules
- `HUtil` 패키지 의존

## 주의점

- `HUI`는 단순 위젯 모음이 아니라, 실제 프로젝트에서 반복 사용된 UI 패턴을 재사용하기 쉽게 정리한 패키지입니다.
- 빈번한 UI 갱신은 `Canvas Rebuild/Repaint` 비용을 유발하므로, 리스트/팝업/상태 이펙트는 배치와 갱신 빈도를 통제해야 합니다.
- 샘플은 참고용 구조이며 실제 프로젝트에는 네임스페이스, 입력 연결, 프리팹 구조를 프로젝트 규칙에 맞게 다시 정리해서 넣어야 합니다.


# 패키지 구성

----

## 폴더 맵 (Runtime)

- Button(8): 버튼 입력, Delegate 처리, OnPress 이펙트 처리.
- DebugConsole(6): 런타임 로그 셀, 콘솔 뷰, 재활용 로그 리스트.
- DropDown(6): 드롭다운 본체, 데이터/유닛 인터페이스, 방향 처리.
- Entity(6): 색상, 이동, 스케일, 활성화 기반 UI 엔티티 처리.
- Graphic(1): 스프라이트 유틸리티.
- Image(1): `HImage` UI 이미지 확장.
- Panel(3): 패널 표시와 프록시 처리.
- Popup(7): 텍스트/이미지/비디오 팝업과 매니저.
- Scrollview(10): 기본/가로/세로/그리드/가변그리드 재활용 스크롤.
- Spinner(2): 로딩 스피너와 매니저.
- Toggle(6): 선택 상태와 이펙트 토글.

## 업그레이드/사용 가이드

- 재활용 스크롤뷰는 데이터 변경 빈도와 셀 생성 비용을 먼저 고려한 뒤 적용하십시오.
- 팝업/패널은 중첩 호출과 닫힘 타이밍 충돌을 방치하면 상태가 쉽게 꼬입니다. 매니저를 통해 진입점을 통일하는 편이 안전합니다.
- 버튼/토글 이펙트는 단순해 보여도 레이아웃 갱신 비용을 유발할 수 있으므로, 다량 배치 화면에서는 프로파일링이 먼저입니다.
- 샘플 코드는 참고용입니다. 실제 빌드에서는 `Samples~`를 제외하거나 별도 패키지로 분리하십시오.

## UPM 설치 경로 (HCUP-Unity)
- `https://github.com/HoHong123/HCUP-Unity.git?path=/HoHong123/HUI`
- `https://github.com/HoHong123/HCUP-Unity.git?path=/HoHong123/HUI#v{목표버전}`
