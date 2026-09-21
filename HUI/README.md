# HUI - 패키지 카드

> 모듈: `HUI/` · 소스 72파일 · `package.json` v1.0.2 (`com.hohong123.hui`)
> 구성 어셈블리 2개
> 코드 문서: **[Runtime README](Runtime/README.md)** · [Editor README](Editor/README.md)

---

## 이 패키지가 담는 것

버튼·토글·드롭다운·팝업·스피너·패널·재활용 스크롤뷰·디버그 콘솔·이미지 확장. UI 표현 계층 전부다.
런타임 67파일이 8개 시스템으로 나뉘며, 각 시스템 문서는 Runtime README 의 목록 표에서 링크한다.

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 참조 |
|---|---|---|---|
| `HCUP.HUI` | Runtime | 67 | `UniTask`, `Unity.TextMeshPro`, `DOTween.Modules`, `HCUP.HUtil`, `HCUP.HDiagnosis`, `HCUP.HInspector`, `HCUP.HCore`, `HCUP.HResource` |
| `HCUP.HUI.Editor` | Editor | 5 | `HCUP.HUtil`, `HCUP.HUI`, `HCUP.HCore`, `Unity.TextMeshPro(.Editor)` |

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다. 개별 UPM 설치는 현재 동작하지 않는다
([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 최소 2021.3 (`package.json` 의 `unity` 필드) |
| UniTask | 팝업·스피너의 비동기 흐름 |
| TextMeshPro | 텍스트 계열 전반 |
| **DOTween** | **선택이 아니라 필수다.** asmdef 가 `DOTween.Modules` 를 직접 참조한다 |

> [!IMPORTANT]
> `DOTWEEN_PRO` 심볼은 설치 요건이 아니다. `#if DOTWEEN_PRO` 가드가 걸린 파일은 `Entity/ColorUiEntity.cs` 하나뿐이고 `MovingUiEntity`·`ScalingUiEntity`·`TransitUiEntity`·`HDropDown` 은 무조건 `using DG.Tweening` 한다. asmdef 의 `defineConstraints` 도 `[]` 다. **DOTween 패키지 자체가 없으면 심볼과 무관하게 컴파일이 실패한다.**

`Samples~`: `Button`, `Console`, `Dropdown`, `Popup`, `Scrollview` (+ `02_Resources`).
Unity 가 컴파일하지 않는 영역이라 실제 빌드에는 포함되지 않는다.

---

## 주의할 점

1. **모든 런타임 타입이 `HUI.*` 네임스페이스 트리 안에 있다.** `UiEvent` 와 `IBasicPanel` 은 루트 `HUI` 네임스페이스에 있다.
2. **DOTween 조건부 컴파일이 파일마다 갈린다** (위 콜아웃). 새 코드를 넣을 때 어느 규약을 따를지
   먼저 정해야 한다.
3. 이 카드는 설치·구성만 다룬다. 각 컴포넌트가 무엇을 하고 어디서 상태가 꼬이는지는
   [Runtime README](Runtime/README.md) 와 그 아래 시스템 문서 8종에 있다.

---

## 히스토리

### 2026-08-07 :: `UiEvent` / `IBasicPanel` 을 `HUI` 네임스페이스로 이동

- 이전: `UiEvent` 와 `IBasicPanel` 두 타입만 전역 네임스페이스에 있었다.
- 현재: 두 파일 모두 `namespace HUI` 안에 있다.

### 2026-08-06 :: `DOTWEEN_PRO` 설치 안내 정정

- 이전: 2026-05-04 판 문서는 "`DOTWEEN_PRO` 를 Scripting Define Symbols 에 추가해야 한다"고 안내했다.
- 현재: 그 심볼은 `ColorUiEntity` 의 트윈 분기만 켠다. 설치 요건은 DOTween 패키지 자체다 (위 콜아웃).
