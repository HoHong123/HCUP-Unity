# HDiagnosis - 패키지 카드

> 모듈: `HDiagnosis/` · 소스 4파일 · `package.json` 없음 (저장소 통째 사용)
> 구성 어셈블리 1개 - **참조 0** (기반 계층)
> 코드 문서: **[Runtime README](Runtime/README.md)**

---

## 이 패키지가 담는 것

이 저장소의 로깅 진입점. 다른 모듈이 `UnityEngine.Debug` 대신 이쪽을 쓴다.

| 파일 | 역할 |
|---|---|
| `Logger/HLogger.cs` | 정적 로깅 진입점 + `OnLogPublished` 이벤트 (인게임 콘솔이 여기 붙는다) |
| `Logger/LogLevel.cs` | 로그 레벨 정의 |
| `Debug/HDebug.cs` | 에디터 전용 진단 (스택 트레이스 강조 등) |
| `Debug/ComponentActivationWatcher.cs` | 컴포넌트 활성 상태 감시 |

`HCUP.HDiagnosis` 는 **아무것도 참조하지 않는다.** 그래서 어느 모듈에서든 순환 참조 걱정 없이 쓸 수 있고, 이 저장소 안에서 17개 어셈블리가 이걸 참조한다.

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 참조 |
|---|---|---|---|
| `HCUP.HDiagnosis` | Runtime | 4 | 없음 (`references: []`) |

동반 Editor 어셈블리는 없다.

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다 - 이 모듈에는 `package.json` 이 없어 개별 UPM 설치 대상이 아니다
([루트 README 의 설치 절](../README.md#설치) 참조).

Unity 6000.3 에서 개발·검증했다. Unity 외 외부 의존이 없다.

---

## 릴리즈 빌드에서 무엇이 사라지나

이 모듈을 쓸 때 가장 자주 어긋나는 지점이다.

| 대상 | 에디터 | 개발 빌드 | 릴리즈 빌드 |
|---|---|---|---|
| `HLogger.*` | 동작 | 동작 | **본체는 살아 있고 콘솔 출력만 빠진다** |
| `HDebug.*` | 동작 | **사라짐** | **사라짐** |
| `ComponentActivationWatcher` | 동작 | **사라짐** | **사라짐** |

`HDebug` 계열은 `[Conditional("UNITY_EDITOR")]` 라 **개발 빌드에서도 통째로 제거된다.**
빌드에서도 남아야 하는 진단은 `HLogger.Error` 로 적어야 한다.

---

## 주의할 점

1. **`logQue` 는 쓰기 전용이다.** 이 큐를 읽을 예정이던 `SendLogsToServer()` 가 빈 스텁이라, 플레이어 빌드에서 최대 1000건의 로그 엔트리를 붙잡기만 한다.
2. **네임스페이스 `HDiagnosis.HDebug` 와 클래스 `HDebug` 가 동명이다.** 외부에서 `using HDiagnosis;` 만 하면 이름이 네임스페이스로 해석되어 실패한다.
3. **`HLogger` 는 `LogLevel` 의 `Debug`/`Fatal`/`Assert` 를 만들지 않는다.** `HUI` 의 로그 콘솔은 이 셋을 분기 처리하고 있어 `HLogger` 경로에서는 도달 불가 분기다.

근거 라인은 [Runtime README](Runtime/README.md) 의 "정리 대상" 절에 있다.

---

## 히스토리

### 2026-08-07 :: `HLogger` static 봉인과 UTC 시각 교정

- 이전: `HLogger` 가 `public class` 라 `new HLogger()` 가 가능했고, `_UtcNow` 가 `DateTimeOffset.Now`(로컬 시각)를 반환했다.
- 현재: `public static class` 이고 `_UtcNow` 는 `DateTimeOffset.UtcNow` 다.

### 2026-08-07 :: `_ResetStatics` 를 `#if !UNITY_EDITOR` 밖으로 이동

- 이전: `_ResetStatics` 가 `#if !UNITY_EDITOR` 블록 안에 있어, Domain Reload 비활성(에디터 전용 기능) 환경에서 정확히 빠졌고 `OnLogPublished` 구독자가 플레이 세션을 넘어 잔존했다.
- 현재: 훅은 항상 컴파일되고 `OnLogPublished = null` 을 수행한다. `logQue.Clear()` 만 `#if !UNITY_EDITOR` 안에 남아 있다.
