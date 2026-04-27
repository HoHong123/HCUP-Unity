# HUtil Changelog

## [1.0.0] - 2026-03-31

### 주요 변경
- Addressables/Resources 기반 에셋 로드 계층을 크게 확장했습니다.
- `AssetHandler`를 추가해 로더, 캐시, Provider, Load Gate, Validator, Lease 관리 구조를 정리했습니다.
- `Data` 계층을 확장해 Load, Save, Cache, Sequence, Subscription 흐름을 세분화했습니다.
- Owner 기반 추적과 Lease 관리 구조를 추가해 에셋/데이터 수명 주기 제어를 보강했습니다.
- `HInspector` 관련 런타임/에디터 유틸을 추가해 커스텀 인스펙터 확장 기반을 마련했습니다.
- `Animation`, `Scene`, `Time`, `Logger`, `Collection`, `Pooling`, `Encode`, `Encrypt`, `Web` 영역을 현재 구조에 맞게 정리했습니다.
- 샘플 콘텐츠를 확장했습니다. `AddressableSequence`, `OwnerTracking`, `SceneUtil` 샘플을 추가해 사용 흐름을 검증할 수 있도록 보강했습니다.

### 마이그레이션/주의
- Addressables와 Resources를 혼용할 수는 있지만, 호출 경로를 섞어버리면 캐시와 해제 책임이 흐려집니다. Provider 진입점을 통일하십시오.
- 에셋/데이터 로드 시스템은 중복 요청, 취소, 해제 타이밍을 먼저 정리하고 적용해야 합니다. 이 순서를 무시하면 구조만 복잡해지고 디버깅 비용만 커집니다.
- 풀링 객체와 로드된 에셋은 사용 후 정리 책임이 따라옵니다. `Get`만 있고 `Return`이나 해제가 없으면 누수와 상태 오염이 발생합니다.
- 에디터 전용 기능은 런타임과 분리되어야 합니다. asmdef와 폴더 구분을 유지하지 않으면 빌드 오류를 스스로 만드는 셈입니다.

### 검증 체크리스트
- AssetHandler: 중복 요청, 프리로드, 캐시 적중, 해제, Lease 종료 흐름이 정상인지 점검하십시오.
- Addressables/Resources: 로더별 예외 전파, 취소 처리, 라벨 로드 결과, 메모리 해제 타이밍을 검증하십시오.
- Data: Cache, Sequence, Subscription 경로에서 소유권 추적과 재진입 안정성을 확인하십시오.
- Logger/Inspector: 빌드 시 에디터 전용 코드가 누출되지 않는지와 로그/인스펙터 동작이 정상인지 확인하십시오.

## [0.2.1] - 2025-11-27

### 주요 변경
- 리포지토리 명칭을 **HCUP-Unity**로 통일하고 UPM 경로를 `HoHong123/HUtil`로 안내했습니다.
- README에 신규 UPM 설치 URL을 추가해 최신 디렉터리 구조와 일치시켰습니다.

### 마이그레이션/주의
- UPM 설치 시 새로운 Git 경로(`HCUP-Unity`)와 디렉터리(`HoHong123/HUtil`)를 사용하십시오.

## [0.2.0] - 2025-09-18

### 주요 변경
- 중복된 런타임 스크립트를 제거하고 총 26개 구성으로 슬림화했습니다.
- HGame/HUI와 버전 정렬을 위해 패키지 버전을 0.2.0으로 상향했습니다.
- 폴더 맵과 의존성, 업그레이드 가이드를 README에 보강했습니다.

### 마이그레이션/주의
- 풀링 객체는 사용 후 반드시 `Return` 호출이 필요합니다.
- Editor 스크립트가 빌드에 포함되지 않도록 asmdef/폴더 분리를 유지하십시오.
- 외부 의존성(UniTask/DoTween/Odin/TMP) 설치를 선행하십시오.

### 검증 체크리스트
- Pooling: 대량 스폰/리턴 루프에서 GC 스파이크 여부.
- Scene/Web: 비동기 호출 실패 시 예외/취소 토큰 전파 확인.
- Logger/Debug: 빌드 시 에디터 전용 코드 포함 여부 점검.
