# HUtil 시스템 요약

----

`HUtil`은 공용 유틸리티 계층을 담당하는 패키지입니다.

에셋 로드, 캐시, 데이터 처리, 풀링, 씬, 로거, 디버그, 인스펙터, 시간, 문자열/수학 유틸 같은 기반 기능을 모아 둔 구조입니다. 이 저장소에서는 다른 패키지(`HGame`, `HUI`)가 기대고 있는 하부 공용 계층을 확인하기 위한 핵심 패키지입니다.

## 1.0.0에서 변경된 점

- Addressables/Resources 기반 에셋 로드 계층을 크게 확장했습니다.
- `AssetHandler`를 추가해 로더, 캐시, Provider, Load Gate, Validator, Lease 관리 구조를 정리했습니다.
- `Data` 계층을 확장해 Load, Save, Cache, Sequence, Subscription 흐름을 세분화했습니다.
- Owner 기반 추적과 Lease 관리 구조를 추가해 에셋/데이터 수명 주기 제어를 보강했습니다.
- `HInspector` 관련 런타임/에디터 유틸을 추가해 커스텀 인스펙터 확장 기반을 마련했습니다.
- `Animation`, `Scene`, `Time`, `Logger`, `Collection`, `Pooling`, `Encode`, `Encrypt`, `Web` 영역을 현재 구조에 맞게 정리했습니다.
- `AddressableSequence`, `OwnerTracking`, `SceneUtil` 샘플을 추가해 사용 흐름을 검증할 수 있도록 보강했습니다.

## 디렉토리 구성

### `Runtime`
- 다른 패키지와 게임 코드가 직접 의존하는 공용 런타임 기능이 들어 있습니다.
- `AssetHandler`, `Data`, `Pooling`, `Scene`, `Inspector`, `Logger`, `Collection`, `Animation`, `Time` 등 기반 영역으로 나뉩니다.

### `Editor`
- Addressables 이름 정리, Owner 추적 확인, 커스텀 인스펙터 처리 같은 에디터 보조 기능이 들어 있습니다.

### `Samples~`
- `AddressableSequence`, `OwnerTracking`, `SceneUtil` 샘플이 포함되어 있습니다.
- 로드 시퀀스, 소유권 추적, 씬 전환 흐름을 예제로 확인할 수 있습니다.

## 중점적으로 봐야 할 부분

- `AssetProvider` 중심의 에셋 요청/캐시/해제 흐름
- `MemoryAssetCache`와 Load Gate를 통한 중복 로드 제어
- `Data` 계층의 Sequence, Cache, Subscription 분리 구조
- `HLogger`와 `HDebug` 기반 공용 디버깅 보조
- `HInspector` 관련 런타임/에디터 확장 구조

## 추천 확인 순서

1. `Runtime/HUtil/AssetHandler/Provider/AssetProvider.cs`
2. `Runtime/HUtil/AssetHandler/Cache/MemoryAssetCache.cs`
3. `Runtime/HUtil/AssetHandler/Load/AddressableLabelLoader.cs`
4. `Runtime/HUtil/Data/Sequence/AddressableLoadSequence.cs`
5. `Runtime/HUtil/Logger/HLogger.cs`
6. `Editor/Runtime/Inspector/HInspectorPropertyDrawer.cs`
7. `Samples~/OwnerTracking`

## 기술 전제

- Unity 2021.3+
- Unity Addressables
- Unity ResourceManager
- UniTask
- UniTask.Addressables
- 선택 의존: Odin Inspector, DoTween

## 주의점

- `HUtil`은 단순 헬퍼 묶음이 아니라, 로드/캐시/구독/소유권 관리를 분리하려는 의도가 강한 기반 패키지입니다.
- 따라서 편의상 바로 호출하는 방식으로 우회하면 구조 장점이 사라집니다. 특히 에셋 로드와 해제는 Provider와 Lease 흐름을 무시하면 다시 꼬입니다.
- 풀링 객체와 로드된 에셋은 사용 후 정리 책임이 따라옵니다. `Get`만 있고 `Return`이나 해제가 없으면 누수와 상태 오염이 발생합니다.
- 에디터 전용 기능은 런타임과 분리되어야 합니다. asmdef와 폴더 구분을 유지하지 않으면 빌드 오류를 스스로 만드는 셈입니다.


# 패키지 구성

----

## 폴더 맵 (Runtime)

- Animation(15): Animator 상태 라우터와 핸들러 인터페이스.
- AssetHandler(28): 에셋 로더, 캐시, Provider, Lease, Validator, Store.
- Collection(4): 순환 리스트, 직렬화 딕셔너리, Enum 배열 유틸.
- Core(2): 싱글톤과 Transform 확장.
- Data(26): Load, Save, Cache, Sequence, Provider, Subscription.
- Debug(2): 디버그 보조와 활성 상태 감시.
- Encode(2): 텍스트 인코딩 보조.
- Encrypt(2): 암호화 유틸리티.
- Font(1): 폰트/외곽선 보조.
- Inspector(12): 조건부 표시, 읽기 전용, 타이틀 등 인스펙터 속성.
- Logger(2): 공용 로거와 로그 레벨 정의.
- Mathx(1): 벡터 유틸리티.
- Pooling(9): 클래스, 컴포넌트, 게임오브젝트, 파티클 풀링.
- Primitives(4): 문자열, 숫자, 열거형, JSON 토큰 유틸.
- Scene(7): 씬 키, 카탈로그, 로더, 베이스 매니저.
- Time(3): 쿨다운, 날짜 체크, 시간 유틸.
- Web(3): 외부 수신과 웹 연결 보조.

## 업그레이드/사용 가이드

- Addressables와 Resources를 섞어 쓸 수는 있지만, 호출 경로를 섞어버리면 캐시/해제 책임이 흐려집니다. Provider 진입점을 통일하십시오.
- 에셋/데이터 로드 시스템은 중복 요청, 취소, 해제 타이밍을 먼저 정리하고 붙여야 합니다. 이 순서를 무시하면 디버깅 비용만 커집니다.
- `HInspector` 속성은 편의 기능이지만 프로젝트 전반에 퍼지면 의존성이 강해집니다. 런타임/에디터 경계를 의식해서 사용하십시오.
- 샘플 코드는 참고용입니다. 실제 빌드에서는 `Samples~`를 제외하거나 별도 패키지로 분리하십시오.

## UPM 설치 경로 (HCUP-Unity)
- `https://github.com/HoHong123/HCUP-Unity.git?path=/HoHong123/HUtil`
- `https://github.com/HoHong123/HCUP-Unity.git?path=/HoHong123/HUtil#v{목표버전}`
