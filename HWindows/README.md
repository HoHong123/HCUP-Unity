# HCUP Windows (HWindows)

Editor-only 커스텀 에디터 윈도우 패키지입니다. 현재 활성 서브모듈은 `NodeWindow` 하나이며, `FileBrowser` 등 추가 서브모듈은 별도 asmdef로 확장할 수 있습니다.

---

## 호환성

| 항목 | 범위 |
|---|---|
| **Unity 최저** | **2022.3.x LTS 이상** |
| **Unity 최고 (검증)** | **6000.3.11f1** |
| **C# 언어 버전** | **C# 9** (Unity 2022.3 기준 상한) |

새 코드를 작성하거나 기존 코드를 수정할 때는 C# 10+ 문법(`file-scoped namespace`, `global using`, `required` 멤버, raw string literal, list pattern, primary constructor 등)을 사용하지 않습니다. Unity 6000+ 전용 API는 `#if UNITY_6000_0_OR_NEWER`로 분기합니다.

### Unity API 의존 목록

| API | 도입 버전 | 상태 | 분기 정책 |
|---|---|---|---|
| `UnityEditor.Experimental.GraphView` | 2022.2+ | Experimental (6000.3 기준) | 2022.3 ~ 6000.3 범위 breaking change 없음. 어댑터 경계(Core/ 2파일)로 격리 |
| `ScriptableSingleton<T>` | 2020+ | 안정 | 분기 불요 |
| `ToolbarMenu` / `ToolbarToggle` | 2021+ | 안정 | 분기 불요 |
| `ToolbarMenu.menu.AppendAction` (동적 status callback) | 2021+ | 안정 | 분기 불요 |
| `IMGUIContainer` | 2019+ | 안정 | 분기 불요 |
| `ObjectField` (UIElements) | 2020+ | 안정 | 분기 불요 |
| `TextField.SetPlaceholderText` | 6000+ 전용 | 6000+ | `#if UNITY_6000_0_OR_NEWER` 적용 또는 else 무동작 |
| `Undo.undoRedoPerformed` | 2022.2+ | 안정 | 분기 불요 |

---

## 패키지 구성

서브모듈별로 asmdef가 분리되어 있어 선택 의존(Odin 등)이 다른 모듈에 전파되지 않습니다.

| 서브모듈 | asmdef | 상태 | 선택 의존 | 문서 |
|---|---|---|---|---|
| `NodeWindow` (Editor) | `HCUP.HWindows.NodeWindow.Editor` | 활성 | — | [docs](Runtime/NodeWindow/docs/README.md) |
| `NodeWindow` (Runtime) | `HCUP.HWindows.NodeWindow` | 활성 | HInspector, HCollection | [docs](Runtime/NodeWindow/docs/README.md) |
| `FileBrowser` | `HCUP.HWindows.FileBrowser.Editor` | 계획 | Sirenix.OdinInspector | — |
