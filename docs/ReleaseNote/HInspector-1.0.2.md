# HInspector 1.0.2 릴리즈 노트

----

어셈블리 링크 : https://github.com/HoHong123/HCUP-Unity.git?path=/HInspector#HInspector-1.0.2

## 개요
커스텀 인스펙터 IMGUI 속성 시스템 어셈블리입니다.
Odin Inspector 없이도 동등한 인스펙터 표현력을 제공하기 위한 attribute 군과 베이스 클래스를 포함합니다.
HTitle / HShowIf / HHideIf / HEnableIf / HListDrawer / HRequired / HButton / HOnValueChanged / HBoxGroup / HHorizontalGroup / HVerticalGroup / HMin/Max / HMinMaxSlider / HReadOnly / HHideLabel / HLabelText / HShowInInspector 등을 제공합니다.
독립 어셈블리로 외부 의존성이 없으며, Odin 환경에서는 HInspectorToOdinBridge 가 Odin 측에 attribute 를 노출합니다.

## 1.0.2에서 변경된 점

----

- 1.0.1 에서 HUtil 로부터 어셈블리 분리 — HCUP.HInspector / HCUP.HInspector.Editor / HCUP.HInspector.Odin.Editor.
- 어셈블리 식별자 prefix 를 HCUP.* 로 통일했습니다 (이전: Hohong123.*).
- HVerticalGroupAttribute 신설.

## 폴더 맵 (Runtime/Inspector)

----

- 비교 헬퍼: CompareType
- 그룹 attribute: HBoxGroup, HHorizontalGroup, HVerticalGroup
- 표시 제어: HShowIf, HHideIf, HEnableIf, HReadOnly, HHideLabel, HLabelText, HShowInInspector
- 값 검증/제한: HMin, HMax, HMinMaxSlider, HRequired, HOnValueChanged
- 리스트/버튼: HListDrawer, HButton
- 베이스: HInspector(추상), HInspectorBehaviour (MonoBehaviour 베이스), HInspectorScriptableObject (SO 베이스)

## 주의사항

----

- HInspector 는 별도 UPM 패키지가 아닌 sibling 어셈블리입니다.
- HInspector attribute 는 IMGUI 기반이며 UIElements 는 지원하지 않습니다.
- Odin Inspector 환경에서는 HCUP.HInspector.Odin.Editor 어셈블리가 자동으로 attribute 를 Odin 으로 브릿지합니다 (defineConstraints: ODIN_INSPECTOR).
- HInspectorBehaviour / HInspectorScriptableObject 베이스를 상속하면 attribute 가 자동 적용됩니다.

Full Changelog: https://github.com/HoHong123/HCUP-Unity/compare/v1.0.0...HInspector-1.0.2
