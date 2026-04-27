# HUtil 1.0.2 릴리즈 노트

----

패키지 링크 : https://github.com/HoHong123/HCUP-Unity.git?path=/HUtil#HUtil-1.0.2

## 개요
공용 기반 유틸리티 패키지입니다.
AssetHandler(Addressables/Resources 추상화), Animation 라우터, Pooling, Font 보조 기능을 포함합니다.
Unity 2021.3+ 환경을 대상으로 하며 UniTask, Addressables, DOTween(선택), Odin Inspector(선택), TextMeshPro 에 의존합니다.

## 1.0.2에서 변경된 점

----

- 어셈블리 식별자 prefix 를 HCUP.* 로 통일했습니다 (이전: Hohong123.*).
- 패키지 디렉토리 구조를 평탄화했습니다 (HoHong123/HUtil → HUtil).
- AssetHandler §11 헤더 형틀 정리 — Cache(IAssetCache, IAssetReader/Writer/Releaser, MemoryAssetCache), Data(AssetFetchMode, AssetLoadMode, AssetRequest), Load(IAssetLoader/LoadGate/Releasable, ResourcesAssetLoader, SharedAssetLoadGate), Provider(AssetProvider/Factory, IAssetProvider), Store(IAssetStore), Subscription(AssetLeaseManager, AssetOwnerId/Generator, IAssetLease/LeaseManager/Owner), Validation(IAssetValidator, DefaultAssetValidator) — 25 클래스 일관 적용.
- AssetProvider Provider/Lease 역할 경계 Dev Log 보강 + Assertion 체크를 HLogger.Throw 기반 런타임 가드로 승격.
- ImagePopup 문자열 로드 경로를 AssetHandler 기반 비동기 API 로 재구현.
- MemoryAssetCache Save 경로 재등록/충돌 재정의.

## 폴더 맵 (Runtime)

----

- Animation: AnimatorState 라우터 (Enter/Exit/Move/IK/Update/MachineEnter/MachineExit) + 핸들러 인터페이스 군
- AssetHandler: Cache/Data/Load/Provider/Store/Subscription/Validation 7개 서브폴더 (Addressables/Resources 추상화)
- Data: 데이터 모듈
- Font: BetterOutline 등 폰트 보조
- Pooling: 객체/컴포넌트/파티클 풀링 시스템 (BasePool, ClassPool, ComponentPool, GameObjectPool, ParticlePoolingSystem + IPool* 인터페이스)
- Odin: Odin Inspector 연동 보조 (선택 의존, ODIN_INSPECTOR define gate)
- Tween: DOTween 연동 보조 (선택 의존, USE_DOTWEEN define gate)

## 주의사항

----

- HUtil 은 HCollection / HCore / HData / HDiagnosis / HInspector 어셈블리들의 sibling 입니다 — 같은 sub-tree 안에 함께 가져갑니다.
- 어셈블리 prefix 변경(Hohong123 → HCUP)에 따라 consumer 측 .asmdef references 와 prefab/scene 의 m_EditorClassIdentifier 가 갱신되어야 합니다.
- Odin / DOTween 은 선택 의존이며 defineConstraints 로 분리 컴파일됩니다.
- AssetHandler 사용 시 Provider/Lease 두 역할의 경계를 인식하고, Lease 측에서 reference counting 을 책임지도록 설계되어 있습니다.

Full Changelog: https://github.com/HoHong123/HCUP-Unity/compare/HUtil-1.0.0...HUtil-1.0.2
