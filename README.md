# Hong's Custom Utility - Unity (HCUP)

> 업로드된 패키지 묶음을 빠르게 파악할 수 있도록 정리한 README입니다. (업데이트: 2025-11-29, 버전 0.3.0)

---

## 🆕 0.3.0 릴리스 하이라이트
- 세 패키지(HUtil/HGame/HUI) 버전을 **0.3.0**으로 상향하고 중복 스크립트를 정리했습니다.
- 폴더 맵과 구성 요소 수치를 최신 상태로 반영했습니다(샘플/런타임 분리 확인).
- 패키지별 README(릴리즈 노트) 추가로 도입/업그레이드 가이드를 제공합니다.
- 신규 IMGUI 기능을 추가했습니다.
    - 추후 커스텀 에디터로 변경할 예정입니다.

---

## 📘 개요
**목표** 🎯
- 반복 구현을 줄이고 공용 유틸(풀링/사운드/UI/2D/플로우/캐릭터 등)을 모듈화합니다.

**개발자 정보** 🔗
- GitHub: https://github.com/HoHong123

**의존성 패키지** 📦
- [UniTask](https://github.com/Cysharp/UniTask)
- [DoTween](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676)
- [Odin Inspector](https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041)
- TextMeshPro

---

## 🗂️ 패키지/폴더 맵
### HUtil (공용 유틸) — 총 **26개 .cs**
- Runtime/HUtil: Collection(3), Core(2), Data(1), Debug(1), Font(1), Logger(1), Mathx(1), **Pooling(9)**, Primitives(2), Scene(1), Web(3)
- Runtime/Tween: 트윈 보조(DoTween 연동)
- Editor: Odin/Inspector 유틸 1개

### HGame (게임 로직) — 총 **53개 .cs**
- Runtime/HGame: 2D(9), Camera(6), Character(3), GameModule(5), Player(6), Skill(8), World(7)
- Samples~: 샘플/데모 코드 9개

### HUI (UI 레이어) — 총 **49개 .cs**
- Runtime/HUI: Button(6), DropDown(6), Entity(4), Panel(4), Popup(7), Scrollview(8), Spinner(1), Toggle(6), 공용 기반 1개
- Samples~: UI 샘플 6개

---

## ⚠️ 주의 사항
- **풀 반환 필수**: 풀에서 Get한 객체는 사용 후 반드시 **Return** 호출(누수/스파이크 방지).
- **초기화 순서**: `UnityPoolMaster`/`SoundManager` 등 정적/싱글톤 의존 코드는 **씬 로드 순서**에 유의.
- **에디터/런타임 분리**: `Editor/` 하위 스크립트는 빌드 포함 시 오류 가능 — asmdef/폴더로 **분리** 권장.
- **외부 의존성**: 일부 코드가 **UniTask** 등 외부 패키지에 의존할 수 있음. 컴파일 에러 시 선 설치 확인.
- **UI 비용**: 빈번한 바/효과 갱신은 **Canvas Rebuild/Repaint** 비용을 유발 — Profiler로 확인.

---

## 🧪 테스트 시나리오
1) **풀링**: 초당 수백 회 스폰/리턴 루프에서 **할당/GC** 스파이크 체크(Profiler/Memory).
2) **사운드**: BGM 전환/교차 페이드, 동시 SFX 50+ 재생 시 **클리핑/채널 관리** 검증.
3) **UI**: 게이지/버튼 이펙트의 프레임별 업데이트로 **Rebuild/Repaint** 비용 측정.
4) **2D/카메라**: 팔로우/패럴랙스에서 타임스케일/해상도 변화 시 안정성 테스트.
5) **Flow/모듈**: 씬/단계 전환 경계 케이스(중복 호출/취소/딜레이) 테스트.
6) **Character/Skill**: 입력·쿨타임·물리 충돌 타이밍 충돌 테스트.

---

## 🏷️ 네이밍/버전 가이드
- **네임스페이스**: `Util.*`, `HGame.*`, `HUI.*` (도메인에 맞춰 세분화)
- **클래스/메서드**: PascalCase, 인터페이스는 `I` 접두
- **필드**: `_camelCase`(private), `camelCase`(local), 상수는 PascalCase 또는 SCREAMING_SNAKE_CASE
- **SemVer** 권장**:** `MAJOR.MINOR.PATCH`, UPM 경로 `?path=/Packages/<name>#vX.Y.Z` 활용

---

## 🙋 문의
- 이슈/개선 제안: https://github.com/HoHong123
- 특정 **폴더/파일**을 지목하면, 해당 API(공개 메서드/이벤트/사용 예시)를 **자세한 문서**로 확장해 드립니다.
