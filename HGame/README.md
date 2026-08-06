# HGame — 패키지 카드

> 모듈: `HGame/` · 소스 44파일 · `package.json` v1.0.3 (`com.hohong123.hgame`)
> 구성 어셈블리 1개 (Runtime 전용 — Editor·Odin asmdef 는 오디오 분리 때 삭제됨)
> 코드 문서: **[Runtime README](Runtime/README.md)**

---

## 이 패키지가 담는 것

게임 페이즈 초기화, 플레이어 스탯, 스킬 스택, 2D 맵·미니맵, 카메라 경계, 월드 이벤트.
런타임 44파일이 6개 시스템으로 나뉘며 각 문서는 Runtime README 에서 링크한다.

오디오 도메인은 1.0.3 에서 `HAudio` 로 분리됐다.

> [!IMPORTANT]
> 종전 이 문서의 폴더맵은 실제 코드와 어긋나 있었다. 바로잡은 내용:
> **World 는 스폰·웨이브가 아니라** 이벤트 포인트/액션 브로드캐스트다.
> **Character 에 입력·상태 제어 코드는 없다** — 인터페이스 2개와 필드 전용 SO 1개뿐이다.
> **Player 에도 제어·입력·인터랙션이 없다** — 스탯 상태 소유가 전부다.
> **쿨타임 처리는 Skill 이 아니라 Player** 의 `PlayerStatView` 에 있다.
> 그리고 실제 폴더는 `H2D`(8) / `H3D`(2) / `Camera`(3) / `Map`(2) 로 나뉜다.

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 참조 |
|---|---|---|---|
| `HCUP.HGame` | Runtime | 44 | `UniTask`, `HCUP.HUtil`, `HCUP.HUI`, `HCUP.HDiagnosis`, `HCUP.HInspector`, `HCUP.HCollection`, `HCUP.HCore` |

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다 — 개별 UPM 설치는 현재 동작하지 않는다
([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 이 프로젝트 기준 6000.3.18f1 |
| UniTask | 페이즈 전환의 비동기 흐름 |
| 의존 모듈 | `HUtil`, `HUI`, `HDiagnosis`, `HInspector`, `HCollection`, `HCore` — 종전 문서는 이 중 2개만 적고 있었다 |

`Samples~`: `InitModule`, `Player`, `Skill`, `World2D` (+ `02_Resources`).

---

## 어디부터 볼까

1. `Runtime/HGame/InitModule/InitManager.cs` — 페이즈 전환 상태머신. 이 패키지의 중심이다
2. [`docs/InitModule.md`](docs/InitModule.md) — 위 코드의 계약과 롤백 규약
3. `Samples~/InitModule` — 실제 배선 예

---

## 주의할 점

1. **장르 불문 범용 엔진이 아니다.** 반복 사용된 게임 제작 패턴을 정리한 것이라, 구조가 일반론보다
   "이 팀이 관리하기 쉬운 쪽"으로 치우쳐 있다.
2. **페이즈 전환은 직렬화해야 한다.** 비동기 흐름이 중복 호출되면 두 상태머신이 동시에 돈다 —
   `InitManager` 의 in-flight 가드와 CTS 규약을 [`docs/InitModule.md`](docs/InitModule.md) 에서 먼저 읽을 것.
3. **알려진 동작 결함이 남아 있다** (무한 루프 진입 경로, `Random.Range` 배타 경계, 맵이 뷰포트보다
   작을 때의 Clamp 역전 등). 목록과 근거는 [Runtime README](Runtime/README.md) 의 "정리 대상" 절에 있다.
