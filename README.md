# Custom Unity Utility Mono Repo (📦 com.hohong123.util)

> 이 문서는 업로드한 개발 편이를 위해 모아둔 커스텀 패키지를 분석해 정리한 README입니다. (생성일: 2025-09-18)

---

## 📘 1. 개요
**용도** ✨  
- 개발의 윤탁함을 위해(반복 구현을 줄이고 공통 유틸을 모듈화)

**목표** 🎯  
- 개발 중 필요했던 기능들을 모아둠(풀링/사운드/UI/2D/플로우/캐릭터 등)

**개발자 정보** 🔗  
- GitHub: https://github.com/HoHong123

**의존성 패키지** 📦
- [UniTask](https://github.com/Cysharp/UniTask)
- [DoTween](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676)
- [Odin Inspector](https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041?srsltid=AfmBOoqpAKZfrIeE2HHgI2EcZ7e8fQvO7Y1UW_eQMWURL2An3zK2aMiT)
- TextMeshPro

---

## 🗂️ 2. 폴더 맵
- **HGame** — 총 19개 .cs
  - `HGame/2D`: 11개
  - `HGame/Flow`: 6개
  - `HGame/Character`: 2개
- **Util** — 총 79개 .cs
  - `Util/UI`: 49개
  - `Util/Pooling`: 12개
  - `Util/Sound`: 7개
  - `Util/Collection`: 2개
  - `Util/Core`: 2개
  - `Util/Formattable`: 2개
  - `Util/Debug`: 1개
  - `Util/Editor`: 1개
  - `Util/Font`: 1개
  - `Util/Logger`: 1개
  - `Util/Scene`: 1개

---

## 🧩 3. 각 폴더별 용도
- **HGame/2D**: 게임별 2D 전용(카메라, 맵/미니맵 등)
- **HGame/Character**: 게임 캐릭터 로직(컨트롤/상태)
- **HGame/Flow**: 게임 플로우/상태 관리(씬 전환/절차)
- **Util/Collection**: 보조 자료구조(CircularList 등)
- **Util/Core**: 공통 상수/확장/헬퍼(코어)
- **Util/Debug**: 모듈 목적 미정 (코드 참조)
- **Util/Editor**: 에디터 확장(Inspector/메뉴)
- **Util/Font**: 텍스트/폰트 유틸
- **Util/Formattable**: 숫자/문자열 포맷
- **Util/Logger**: 로깅/스택트레이스 헬퍼
- **Util/Pooling**: 오브젝트 풀(할당/GC 최소화, 재사용)
- **Util/Scene**: 씬 전환/로딩 유틸
- **Util/Sound**: BGM/SFX 재생과 컨테이너 관리
- **Util/UI**: UI 컴포넌트(버튼/바/효과 등)

---

## 🧭 4. 네임스페이스 가이드
- **HGame**
  - `HGame._2D.Cam` (2)
  - `HGame._2D.Map` (6)
  - `HGame._2D.View` (1)
  - `HGame.Game.Flow` (4)
  - `HGame.Game.Character` (2)
- **Util**
  - `Util.Pooling` (8)
  - `Util.Sound` (5)
  - `Util.UI.ButtonUI` (5)
  - `Util.UI.Drop` (3)
  - `Util.UI.Entity` (3)
  - `Util.UI.Popup` (6)
  - `Util.UI.ScrollView` (8)
  - `Util.UI.ToggleUI` (5)

---

## ⚠️ 5. 주의 사항
- **풀 반환 필수**: 풀에서 Get한 객체는 사용 후 반드시 **Return** 호출(누수/스파이크 방지).
- **초기화 순서**: `UnityPoolMaster`/`SoundManager` 등 정적/싱글톤 의존 코드는 **씬 로드 순서**에 유의.
- **에디터/런타임 분리**: `Editor/` 하위 스크립트는 빌드 포함 시 오류 가능 — asmdef/폴더로 **분리** 권장.
- **외부 의존성**: 일부 코드가 **UniTask** 등 외부 패키지에 의존할 수 있음. 컴파일 에러 시 선 설치 확인.
- **UI 비용**: 빈번한 바/효과 갱신은 **Canvas Rebuild/Repaint** 비용을 유발 — Profiler로 확인.

---

## 🧪 6. 테스트 시나리오
1) **풀링**: 초당 수백 회 스폰/리턴 루프에서 **할당/GC** 스파이크 체크(Profiler/Memory).  
2) **사운드**: BGM 전환/교차 페이드, 동시 SFX 50+ 재생 시 **클리핑/채널 관리** 검증.  
3) **UI**: 게이지/버튼 이펙트의 프레임별 업데이트로 **Rebuild/Repaint** 비용 측정.  
4) **2D**: 카메라 팔로우/패럴랙스에서 타임스케일/해상도 변화 시 안정성 테스트.  
5) **Flow**: 플로우(씬 전환/상태머신) 경계 케이스(중복 호출/취소/딜레이) 테스트.  
6) **Character**: 입력/물리 충돌/상태 전이(Idle/Move/Attack 등) 타이밍 충돌 테스트.

---

## 🏷️ 7. 네이밍 컨벤션
- **네임스페이스**: `Util.*`, `HGame.*` (도메인에 따라 세분화: `Util.Pooling`, `HGame.Game.Flow` 등)  
- **클래스/메서드**: PascalCase  
- **인터페이스**: `I` 접두(PascalCase)  
- **필드**: `_camelCase`(private) / `camelCase`(local)  
- **상수**: `SCREAMING_SNAKE_CASE` 또는 PascalCase(팀 규칙에 맞춤)

---

## 🔖 8. 버전 힌트
- **SemVer** 권장: `MAJOR.MINOR.PATCH`  
- 다프로젝트 공유 시 태그 고정 활용(UPM Git 또는 서브모듈 포인터)  
- **권장 패키지 분할(UPM 이름 예)**  
  - `com.hohong123.util` → 공용 유틸(Pooling/Sound/UI/2D/Collection/Logger/Scene/Core/Editor/…)  
  - `com.hohong123.hgame` → 게임 전용 로직(HGame/Flow/Character/2D 등)  
- 모노레포 사용 시: `?path=/Packages/com.hohong123.util#vX.Y.Z` 형태로 하위 패키지 참조

---

## 🙋 9. 문의
- 이슈/개선 제안: https://github.com/HoHong123  
- 특정 **폴더/파일**을 지목하면, 해당 API(공개 메서드/이벤트/사용 예시)를 **자세한 문서**로 확장해 드립니다.
