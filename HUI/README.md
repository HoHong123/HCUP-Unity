# HUI — 패키지 카드

> 모듈: `HUI/` · 소스 68파일 · `package.json` v1.0.2 (`com.hohong123.hui`)
> 구성 어셈블리 2개
> 코드 문서: **[Runtime README](Runtime/README.md)** · [Editor README](Editor/README.md)

---

## 이 패키지가 담는 것

버튼·토글·드롭다운·팝업·스피너·패널·재활용 스크롤뷰·디버그 콘솔·이미지 확장 — UI 표현 계층 전부.
런타임 63파일이 8개 시스템으로 나뉘며, 각 시스템 문서는 Runtime README 의 목록 표에서 링크한다.

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 참조 |
|---|---|---|---|
| `HCUP.HUI` | Runtime | 63 | `UniTask`, `Unity.TextMeshPro`, `DOTween.Modules`, `HCUP.HUtil`, `HCUP.HDiagnosis`, `HCUP.HInspector`, `HCUP.HCore`, `HCUP.HResource` |
| `HCUP.HUI.Editor` | Editor | 5 | `HCUP.HUtil`, `HCUP.HUI`, `HCUP.HCore`, `Unity.TextMeshPro(.Editor)` |

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다 — 개별 UPM 설치는 현재 동작하지 않는다
([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 이 프로젝트 기준 6000.3.18f1 |
| UniTask | 팝업·스피너의 비동기 흐름 |
| TextMeshPro | 텍스트 계열 전반 |
| **DOTween** | **선택이 아니라 필수다.** asmdef 가 `DOTween.Modules` 를 직접 참조한다 |

> [!IMPORTANT]
> 종전 이 문서는 "`DOTWEEN_PRO` 를 Scripting Define Symbols 에 추가해야 한다"고 안내했다.
> **사실이 아니다.** `#if DOTWEEN_PRO` 가드가 걸린 파일은 `Entity/ColorUiEntity.cs` 하나뿐이고
> `MovingUiEntity`·`ScalingUiEntity`·`HDropDown` 은 무조건 `using DG.Tweening` 한다.
> asmdef 의 `defineConstraints` 도 `[]` 다. 심볼을 정의하든 말든 컴파일 결과가 같으므로,
> **DOTween 패키지 자체가 없으면 심볼과 무관하게 컴파일이 실패한다.**

`Samples~`: `Button`, `Console`, `Dropdown`, `Popup`, `Scrollview` (+ `02_Resources`).
Unity 가 컴파일하지 않는 영역이라 실제 빌드에는 포함되지 않는다.

---

## 주의할 점

1. **`UiEvent` 와 `IBasicPanel` 만 전역 네임스페이스다.** 나머지 61파일은 `HUI.*` 트리 안에 있다.
2. **DOTween 조건부 컴파일이 파일마다 갈린다** (위 콜아웃). 새 코드를 넣을 때 어느 규약을 따를지
   먼저 정해야 한다.
3. 이 카드는 설치·구성만 다룬다. 각 컴포넌트가 무엇을 하고 어디서 상태가 꼬이는지는
   [Runtime README](Runtime/README.md) 와 그 아래 시스템 문서 8종에 있다.
