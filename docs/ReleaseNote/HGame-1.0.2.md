# HGame 1.0.2 릴리즈 노트

----

패키지 링크 : https://github.com/HoHong123/HCUP-Unity.git?path=/HGame#HGame-1.0.2

## 개요
게임 로직 계층 패키지입니다.
GameModule(Phase Stack), Sound(카탈로그 기반), Player, Skill, World(Event), Camera(Boundry), 2D, Character 등 게임 전용 모듈을 포함합니다.
Unity 2021.3+ 환경을 대상으로 하며 HUtil/HUI/HCollection/HInspector/HDiagnosis 와 UniTask 에 의존합니다.

## 1.0.2에서 변경된 점

----

- 어셈블리 식별자 prefix 를 HCUP.* 로 통일했습니다 (이전: Hohong123.*).
- 패키지 디렉토리 구조를 평탄화했습니다.
- Custom Inspector 치환 — Sound/Skill/Camera/World/Game 계층 33+ 클래스 Odin → HInspector 일괄 치환.
- 치환 대상: SfxAgent, SfxView, BaseSfxAddon, Sound/SoundManager(HRequired 7종 치환), WorldEventManager, BaseEventPoint(@표현식 ShowIf), ColorOnPressButton, BaseSkillSO, SkillCatalogSO, SkillManager, SkillRarityStack, SkillStats, PlayerConfig, BaseCharacterConfig, BaseCameraBoundry, CameraBoundry/2D/Perspective/TopDown3D, CameraManager, MapManager, MinimapTracker, ParallexLayer, Box2DBoundSource, CompositeBoundSource, SpriteRendererBoundSource, TilemapBoundSource.
- HGame.Odin 어셈블리를 Runtime/Odin 에서 HGame/Odin 으로 이동 (Runtime 계층에서 분리).
- ColorUiEntity / EnableUiEntity 는 POCO 계층 브릿지 검증 대기로 Odin 원복.

## 폴더 맵 (Runtime)

----

- 2D: 2D 게임 보조 (Box2D/Composite/SpriteRenderer/Tilemap BoundSource, ParallexLayer)
- Audio: 오디오 보조
- Camera: CameraManager + Boundry (2D / 3D / Perspective / TopDown3D)
- Character: 베이스 캐릭터 + Config
- GameModule: Phase Stack 기반 게임 흐름 관리
- Player: PlayerConfig + Manager
- Skill: 스킬 카탈로그 SO + 스택/레어리티 + Stats 관리
- Sound: 카탈로그 기반 사운드 매니저 + Sfx Agent/View/Addon
- World: WorldEventManager + EventPoint + MapManager + MinimapTracker

## 주의사항

----

- HGame 은 HUI/HUtil/HCollection/HInspector/HDiagnosis 의존성이 필수입니다.
- Custom Inspector 치환으로 Odin 미설치 환경에서도 인스펙터가 정상 표시됩니다 (Odin 은 보조 옵션).
- 도메인 SO 는 BaseNode 상속 + AssetReference 간접 참조 패턴을 권장합니다.

Full Changelog: https://github.com/HoHong123/HCUP-Unity/compare/HGame-1.0.0...HGame-1.0.2
