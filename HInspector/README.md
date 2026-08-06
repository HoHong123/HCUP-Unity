# HInspector — 패키지 카드

> 모듈: `HInspector/` · 소스 32파일 · `package.json` 없음 (저장소 통째 사용)
> 구성 어셈블리 3개
> 코드 문서: **[Editor README](Editor/README.md)** (어트리뷰트 → 처리 주체 매칭표가 여기 있다) · [Runtime README](Runtime/README.md)

---

## 이 패키지가 담는 것

Unity 인스펙터 확장 어트리뷰트 **23종**과 그것을 그리는 IMGUI 렌더러. Odin 이 설치돼 있으면
브릿지 어셈블리가 붙는다.

`HInspectorBehaviour` 또는 `HInspectorScriptableObject` 를 상속한 타겟에서 동작한다.
Odin 미설치 환경에서는 일반 `MonoBehaviour` 도 전역 fallback 으로 처리된다.

> [!IMPORTANT]
> 종전 이 문서는 어트리뷰트를 8종만 나열했다. `HMinMaxSlider`·`HSpritePreview`·`HOnValueChanged`·
> `HListDrawer`·`HRequired`·`HEnableIf`·`HHideIf`·`HLabelText`·`HMin`·`HMax` 등이 빠져 있었다.
> **전체 목록과 각 어트리뷰트를 누가 그리는지는 [Editor README](Editor/README.md) 의 매칭표가 정본이다** —
> 여기에 다시 적으면 두 벌이 되어 다시 어긋난다.

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 역할 |
|---|---|---|---|
| `HCUP.HInspector` | Runtime | 23 | 어트리뷰트 타입 정의. 참조 0 |
| `HCUP.HInspector.Editor` | Editor | 9 | `HInspectorEditor` 렌더러 + 공개 IMGUI 헬퍼 |
| `HCUP.HInspector.Odin.Editor` | Editor (`ODIN_INSPECTOR`) | 1 | Odin 브릿지 |

Editor 어셈블리 참조: `HCUP.HInspector`, `HCUP.HDiagnosis`, `Unity.Addressables`, `Unity.ResourceManager`.

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다 — 이 모듈에는 `package.json` 이 없어 개별 UPM 설치 대상이 아니다
([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 이 프로젝트 기준 6000.3.18f1 |
| Odin Inspector | 선택. `ODIN_INSPECTOR` 정의 시 브릿지 어셈블리가 컴파일된다 |

---

## CustomEditor 밖에서 쓰기

`SettingsProvider`·`IMGUIContainer` 처럼 `CustomEditor` 경로 밖의 IMGUI 영역에서도
`HTitle` 과 같은 시각 규격을 쓸 수 있다.

```csharp
using HInspector.Editor;

void OnGUI(string searchContext) {
    HTitleDrawer.Draw("Snap Settings");
    EditorGUILayout.PropertyField(...);
}
```

`HInspectorEditor` 도 내부에서 이 헬퍼에 위임하므로 시각 규격이 한 곳에서만 정의된다.

---

## 주의할 점

1. **드로어에 성능 부담이 있다.** `GetPropertyHeight` 와 `OnGUI` 양쪽에서 리플렉션을 다시 돌리고,
   `[HShowIf("@...")]` 표현식은 프레임당 2회 파싱된다. 근거와 라인은
   [Editor README](Editor/README.md) 의 "정리 대상" 절에 있다.
2. **어트리뷰트는 프로젝트 전반에 퍼지기 쉽다.** 런타임 타입에 붙는 어트리뷰트이므로 한 번 퍼지면
   이 모듈에 대한 의존이 되돌리기 어려워진다. 런타임/에디터 경계를 의식하고 쓸 것.
