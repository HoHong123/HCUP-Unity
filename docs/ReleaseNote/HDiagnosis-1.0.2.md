# HDiagnosis 1.0.2 릴리즈 노트

----

어셈블리 링크 : https://github.com/HoHong123/HCUP-Unity.git?path=/HDiagnosis#HDiagnosis-1.0.2

## 개요
진단/로깅 어셈블리입니다.
HLogger, HDebug, ComponentActivationWatcher 등 디버깅과 런타임 진단 도구를 제공합니다.
독립 어셈블리로 외부 의존성이 없습니다.

## 1.0.2에서 변경된 점

----

- 1.0.1 에서 HUtil 로부터 어셈블리 분리.
- 어셈블리 식별자 prefix 를 HCUP.* 로 통일했습니다 (이전: Hohong123.*).

## 폴더 맵 (Runtime)

----

- Debug: HDebug, ComponentActivationWatcher (컴포넌트 활성화 감시)
- Logger: HLogger, LogLevel

## 주의사항

----

- HDiagnosis 는 별도 UPM 패키지가 아닌 sibling 어셈블리입니다.
- HLogger.Throw 는 HUtil.AssetHandler 의 Assertion 가드로도 사용됩니다 (런타임 에러 승격 경로).
- Editor 전용 기능과 Runtime 진단을 분리해 사용하는 것을 권장합니다.

Full Changelog: https://github.com/HoHong123/HCUP-Unity/compare/v1.0.0...HDiagnosis-1.0.2
