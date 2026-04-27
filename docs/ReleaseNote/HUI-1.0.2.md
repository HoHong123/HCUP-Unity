# HUI 1.0.2 릴리즈 노트

----

패키지 링크 : https://github.com/HoHong123/HCUP-Unity.git?path=/HUI#HUI-1.0.2

## 개요
Unity UI 로직 패키지입니다.
Button(다양한 인터랙션), Toggle, DropDown, Popup(Manager + Image/Text/Video), Panel, Scrollview(가상화 재활용 기반), DebugConsole 등 UI 컴포넌트를 포함합니다.
Unity 2021.3+ 환경을 대상으로 하며 HUtil/HCore/HInspector/HDiagnosis 와 UniTask/Unity.TextMeshPro 에 의존합니다.

## 1.0.2에서 변경된 점

----

- 어셈블리 식별자 prefix 를 HCUP.* 로 통일했습니다 (이전: Hohong123.*).
- 패키지 디렉토리 구조를 평탄화했습니다.
- Scrollview §11 헤더 형틀 정리 — BaseRecycleCellData/View, IGridSpanData, IRecycleView, BaseRecycleView, SpanningGridRecycleView 6 클래스.
- IGridSpanData 인코딩 cp949 → UTF-8 정정.
- BaseRecycleView Region 추가.
- Popup 생명주기 누수 수정 — BasePopupUi/PopupManager/ImagePopup/VideoPopup 의 버튼 리스너 + 이벤트 구독 해제 누락 수정.

## 폴더 맵 (Runtime)

----

- Button: 다양한 인터랙션 버튼 (ColorOnPress/ScaleOnPress/MoveOnPress/EnableOnPress/DelegateButton)
- DebugConsole: 런타임 로그 콘솔 (HLogConsole, HLogRecycleView, HLogCellData/View)
- DropDown: HDropdown 커스텀 드롭다운
- Entity: UI 엔티티 추상 (Color/Enable UiEntity)
- Graphic: UI 그래픽 보조
- Image: 이미지 컴포넌트
- Panel: 패널 베이스
- Popup: 팝업 매니저 + 이미지/텍스트/비디오 팝업
- Scrollview: 가상화된 재활용 스크롤뷰 (BaseRecycleView, SpanningGridRecycleView)
- Spinner: 로딩 스피너
- Toggle: 커스텀 토글

## 주의사항

----

- HUI 는 HUtil/HCore/HInspector/HDiagnosis 의존성이 필수입니다.
- Scrollview 는 가상화(virtualization) 기반이므로 셀 수명 처리에 주의하십시오.
- Popup/RecycleView 의 이벤트 구독 해제는 OnDestroy 시점에 자동으로 동작합니다 (1.0.2 에서 누수 수정).

Full Changelog: https://github.com/HoHong123/HCUP-Unity/compare/HUI-1.0.0...HUI-1.0.2
