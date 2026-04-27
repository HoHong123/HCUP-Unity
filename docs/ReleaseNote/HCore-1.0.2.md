# HCore 1.0.2 릴리즈 노트

----

어셈블리 링크 : https://github.com/HoHong123/HCUP-Unity.git?path=/HCore#HCore-1.0.2

## 개요
공용 기반 어셈블리입니다.
SingletonBehaviour, BaseSceneManager (Scene 카탈로그/전환), TransformExtention, CooldownTimer 등 코어 유틸을 포함합니다.
HData / HDiagnosis / HInspector / HUtil 에 의존하며 UniTask, UniTask.Addressables 를 사용합니다.

## 1.0.2에서 변경된 점

----

- 1.0.1 에서 HUtil 로부터 어셈블리 분리.
- 어셈블리 식별자 prefix 를 HCUP.* 로 통일했습니다 (이전: Hohong123.*).
- SingletonBehaviour Instance 해제 누락 수정 (1.0.1).

## 폴더 맵 (Runtime)

----

- Core: SingletonBehaviour, TransformExtention
- Scene: BaseSceneManager, ISceneControl, SceneCatalogSO, SceneKey, SceneLoader, SceneRef
- Time: CooldownTimer

## 주의사항

----

- HCore 은 별도 UPM 패키지가 아닌 sibling 어셈블리입니다.
- BaseSceneManager 는 Addressables 와 Resources 양쪽 로드 모드를 지원합니다 (HUtil.AssetHandler 경유).
- SingletonBehaviour 는 OnDestroy 시 Instance 를 null 처리하므로 도메인 정리 시 중복 인스턴스가 누적되지 않습니다.

Full Changelog: https://github.com/HoHong123/HCUP-Unity/compare/v1.0.0...HCore-1.0.2
