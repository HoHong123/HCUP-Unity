# HGame - 패키지 카드

> 모듈: `HGame/` · 소스 44파일 · `package.json` v1.0.3 (`com.hohong123.hgame`)
> 구성 어셈블리 1개 (Runtime 전용)
> 코드 문서: **[Runtime README](Runtime/README.md)**

---

## 이 패키지가 담는 것

게임 페이즈 초기화, 플레이어 스탯, 스킬 스택, 2D 맵·미니맵, 카메라 경계, 월드 이벤트.
런타임 44파일이 6개 시스템으로 나뉘며 각 문서는 Runtime README 에서 링크한다.

폴더 구성과 각 시스템의 범위는 다음과 같다.

- **World** 는 스폰·웨이브가 아니라 이벤트 포인트/액션 브로드캐스트다.
- **Character** 는 인터페이스 2개와 필드 전용 SO 1개뿐이다. 입력·상태 제어 코드는 없다.
- **Player** 는 스탯 상태 소유가 전부다. 제어·입력·인터랙션은 없다.
- **쿨타임 처리는 Skill 이 아니라 Player** 의 `PlayerStatView` 에 있다.
- 차원별 폴더는 `H2D`(8) / `H3D`(2) 이고, 차원 공통은 `Camera`(3) / `Map`(2) 이다.

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 참조 |
|---|---|---|---|
| `HCUP.HGame` | Runtime | 44 | `UniTask`, `HCUP.HUtil`, `HCUP.HUI`, `HCUP.HDiagnosis`, `HCUP.HInspector`, `HCUP.HCollection`, `HCUP.HCore` |

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다. 개별 UPM 설치는 현재 동작하지 않는다
([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 최소 2021.3 (`package.json` 의 `unity` 필드) |
| UniTask | 페이즈 전환의 비동기 흐름 |
| 의존 모듈 | `HUtil`, `HUI`, `HDiagnosis`, `HInspector`, `HCollection`, `HCore` |

`Samples~` 폴더: `InitModule`, `Player`, `Skill`, `World2D` (+ `02_Resources`). 이 중 `package.json` 의 `samples` 에 등록된 것은 `InitModule` 과 `Skill` 2개뿐이다.

---

## 어디부터 볼까

1. `Runtime/HGame/InitModule/InitManager.cs` - 페이즈 전환 상태머신. 이 패키지의 중심이다
2. [`docs/InitModule.md`](docs/InitModule.md) - 위 코드의 계약과 롤백 규약
3. `Samples~/InitModule` - 실제 배선 예

---

## 주의할 점

1. **장르 불문 범용 엔진이 아니다.** 반복 사용된 게임 제작 패턴을 정리한 것이라, 구조가 일반론보다
   "이 팀이 관리하기 쉬운 쪽"으로 치우쳐 있다.
2. **페이즈 전환은 직렬화해야 한다.** 새 전환은 이전 전환의 CTS 를 취소하고 모듈 사이마다 취소를 검사하며, 훅 안에서의 재진입은 깊이 8 에서 거부한다 (`InitManager.cs:101-140`). 규약은 [`docs/InitModule.md`](docs/InitModule.md) 에서 먼저 읽을 것.
3. **시스템 사이 배선은 자동이 아니다.** `InitModule` 은 다른 5개 시스템을 모른다. 계약과 정리 대상은 [Runtime README](Runtime/README.md) 의 "주의할 점" 절에 있다.

---

## 히스토리

### 2026-08-07 :: 알려진 동작 결함 3건 수정

- 이전: "주의할 점" 에 무한 루프 진입 경로(`PlayerStatus.GainExp`), `Random.Range` 배타 경계(`PlayerConfig.RollBaseDamage`), 맵이 뷰포트보다 작을 때의 Clamp 역전이 남은 결함으로 적혀 있었다.
- 현재: `ExpToNext > 0f` 가드, `maxDamage + 1` 상한, `min > max` 일 때 중앙 고정으로 세 곳 모두 수정됐다.

### 2026-08-06 :: 폴더맵 정정

- 이전: 이 문서는 `World(7): 월드/스폰/웨이브 관리`, `Character(3): 캐릭터 입력·상태 제어`, `2D(9)` 같은 분류를 제시했고, 의존 모듈을 2개만 적고 있었다.
- 현재: 실제 폴더(`H2D` / `H3D` / `Camera` / `Map`)와 asmdef 참조 6개 모듈 기준으로 고쳤다.

### 2026-08-04 :: `GameModule/` 을 `InitModule/` 로 개칭

- 이전: 폴더 `GameModule/`, 타입 `GameManager<TSelf>` / `BaseGameModule` / `GameContext` / `GamePhaseType` 이었다.
- 현재: `InitModule/` 폴더의 `InitManager<TSelf>` / `BaseInitModule` / `InitContext` / `InitPhaseType` 이다. 페이즈 전환 API 이름(`GamePrepareAsync` 등)은 그대로 유지했다.

### 2026-05-05 :: 오디오 도메인을 `HAudio` 로 분리 (v1.0.3)

- 이전: `HCUP.HGame.Editor` / `HCUP.HGame.Odin` asmdef 와 오디오 샘플(`Sound`)이 이 패키지에 있었다.
- 현재: 오디오 코드와 샘플은 `HAudio` 로 옮겼고, 두 asmdef 는 삭제해 Runtime 어셈블리 1개만 남았다.
