# HData - 패키지 카드

> 모듈: `HData/` · 소스 9파일 · `package.json` v1.0.0 (`com.hohong123.hdata`)
> 구성 어셈블리 1개 - **참조 0** (기반 계층)
> 코드 문서: **[Runtime README](Runtime/README.md)**

---

## 이 패키지가 담는 것

의존 없는 순수 유틸 묶음. 네 갈래이며 서로 참조하지 않는다.

| 폴더 | 파일 | 담는 것 |
|---|---|---|
| `Primitives` | 4 | 문자열·숫자·열거형·JSON 토큰 보조 |
| `Encrypt` | 2 | AES 암·복호화 |
| `Encode` | 2 | Base64 등 텍스트 인코딩 |
| `Mathx` | 1 | 벡터 보조 |

`HCUP.HData` 는 **아무것도 참조하지 않는다.** 이 저장소 안에서는 `HCUP.HCore` 와 `HCUP.HAudio.Editor` 가 이걸 참조한다.

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 참조 |
|---|---|---|---|
| `HCUP.HData` | Runtime | 9 | 없음 (`references: []`) |

동반 Editor 어셈블리는 없다.

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다 - 개별 UPM 설치는 현재 동작하지 않는다
([루트 README 의 설치 절](../README.md#설치) 참조).

Unity 최소 2021.3 (`package.json` 의 `unity` 필드). Unity 외 외부 의존이 없다. **단, `JTokenUtil.cs` 만 예외다** - 아래 참조.

---

## 주의할 점

1. **`Primitives/JTokenUtil.cs` 는 파일 전체가 죽어 있다.** `#if Newtonsoft` 로 감싸져 있는데
   이 심볼을 정의하는 곳이 저장소 어디에도 없다 (`versionDefines` 전부 비어 있고
   `defineConstraints` 에도 없다). 게다가 asmdef 가 Newtonsoft 어셈블리를 참조하지 않으므로
   **심볼을 켜는 순간 컴파일 에러가 난다.** 삭제하거나 `versionDefines` 를 붙여야 한다.
2. **폴더와 네임스페이스가 어긋난 파일이 있다** - `Primitives/StringUtil.cs` 의 네임스페이스는
   `HData.Formattable` 이다.

근거 라인은 [Runtime README](Runtime/README.md) 의 "정리 대상" 절에 있다.

---

## 히스토리

### 2026-08-07 :: `VectorUtil.GetCanvasPosition` 을 `GetScreenPosition` 으로 개명

- 이전: 이름은 캔버스 좌표를 뜻했지만 본문은 `Camera.WorldToScreenPoint` 였고, 파라미터명이 `_target` / `_camera` 였다.
- 현재: 이름을 `GetScreenPosition` 으로 바꿔 동작과 맞췄고 파라미터명을 `target` / `camera` 로 정정했다.

### 2026-08-07 :: `StringUtil.NumToAlpha` 의 K 단위 경계 정정

- 이전: K 단위 분기 조건이 `num >= 10_000` 이라 1,000 ~ 9,999 가 축약되지 않았다.
- 현재: `num >= 1_000` 이다. 다른 단위(M/B/T)와 동일하게 "그 단위의 기준값 이상" 이면 축약한다.
