# HWindows - 패키지 카드

> 모듈: `HWindows/` · 소스 24파일 · `package.json` v1.0.0 (`com.hohong123.hwindows`)
> 구성 어셈블리 2개 (Runtime 은 데이터 계약만, Editor 가 본체)
> 코드 문서: **[Editor README](Editor/NodeWindow/README.md)** · [Runtime README](Runtime/NodeWindow/README.md)

---

## 이 패키지가 담는 것

GraphView 기반 커스텀 에디터 윈도우. 현재 활성 서브모듈은 `NodeWindow` 하나다.

노드 그래프를 저작하고 그 결과를 `ScriptableObject` 로 남긴다. `HDialogue` 의 대화 카탈로그가
이 위에 얹혀 있다.

| 시스템 | 파일 | 담는 것 | 문서 |
|---|---|---|---|
| GraphEditor | 8 | GraphView 어댑터, 노드·엣지 뷰, 클립보드, 창 진입점 | [docs/GraphEditor.md](docs/GraphEditor.md) |
| NodeCatalog | 13 | `NodeCatalogSO`, 노드 베이스 + 노드 타입 3종, 엣지 베이스 + 엣지 2종, `NodeUID` + `NodeUIDDrawer`, 저작 게이트 2파일, `AssemblyInfo` | [docs/NodeCatalog.md](docs/NodeCatalog.md) |
| Settings | 3 | 스냅 설정 + `SettingsProvider` (`Project/HCUP/Node Window`) | [docs/Settings.md](docs/Settings.md) |

**Runtime 어셈블리는 데이터 계약만 담는다.** 노드 SO 는 빌드에 포함되어도 무방하지만, 편집
기능은 전부 Editor 어셈블리에 있다.

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 참조 |
|---|---|---|---|
| `HCUP.HWindows.NodeWindow` | Runtime | 10 | `HCUP.HCollection`, `HCUP.HInspector` |
| `HCUP.HWindows.NodeWindow.Editor` | Editor | 14 | `HCUP.HWindows.NodeWindow`, `HCUP.HInspector.Editor`, `HCUP.HUtil`, `HCUP.HUtil.Editor`, `HCUP.HDiagnosis`, `HCUP.HCollection` |

서브모듈별로 asmdef 를 나눠 두어 선택 의존이 다른 모듈로 전파되지 않는다.
`HGame`·`HUI` 로의 역방향 참조는 없다.

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다 - 개별 UPM 설치는 현재 동작하지 않는다
([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 최소 2021.3 (`package.json` 의 `unity` 필드). 6000.3 에서 개발·검증. **아래 Unity API 의존 참조** |
| `UnityEditor.Experimental.GraphView` | Editor 어셈블리의 토대 |

### 호환성 - C# 언어 버전

새 코드를 쓰거나 기존 코드를 고칠 때 **C# 10+ 문법을 쓰지 않는다** (file-scoped namespace,
global using, `required` 멤버, raw string literal, list pattern, primary constructor 등).
Unity 2022.3 기준 상한이 C# 9 이기 때문이다.

### Unity API 의존

| API | 도입 | 분기 정책 |
|---|---|---|
| `UnityEditor.Experimental.GraphView` | 2022.2+ | 2022.3~6000.3 범위에서 breaking change 없음. 어댑터 경계(`Core/` 5파일)로 격리 |
| `EditorUtility.EntityIdToObject` | 6000.3+ | `#if UNITY_6000_3_OR_NEWER` 로 분기. 그 외 버전은 `InstanceIDToObject` (`NodeCatalogObjectChangeWatcher.cs:18-23`) |
| `ScriptableSingleton<T>` / `ToolbarMenu` / `IMGUIContainer` / `ObjectField` / `Undo.undoRedoPerformed` | 2019~2022 | 안정. 분기 불요 |

---

## 주의할 점

1. **`NodeCatalogAuthor` 의 루트 자동 이전은 `CatalogNode` 를 후보에서 뺀다.** `RemoveNode` 와 `PurgeNullNodes` 는 `InternalSetRoot` 를 직접 호출하므로, `SetRoot` 의 `CatalogNode` 타입 가드를 대신해 후보 탐색 `_FindAnyOtherNode` 가 `CatalogNode` 를 건너뛴다 (`NodeCatalogAuthor.cs:647-657`).
2. **`FileBrowser` 서브모듈은 이 패키지에 없다.** 파일은 `HUtil/Editor/Odin/FileBrowser.cs` 에 따로 있다.

코드 수준의 계약과 정리 대상은 [Editor README](Editor/NodeWindow/README.md) 의 "주의할 점" 절에 있다.

---

## 히스토리

### 2026-09-21 :: `Runtime/NodeWindow/docs/README.md` 를 현행 코드에 맞춤

- 이전: 이 카드의 "주의할 점" 에 그 문서가 낡았다는 항목이 있었다. 노드 뷰 팩토리 시그니처를 `(node, catalog)` 로 적었고, `NodeUIDDrawer` 경로가 실제와 달랐고, Active 하이라이트·Trace 모드가 빠져 있었다.
- 현재: 그 문서의 해당 항목을 코드에 맞게 고쳤다.

### 2026-08-07 :: 루트 자동 이전 가드 우회 수정, `EntityIdToObject` 버전 분기

- 이전: `PurgeNullNodes` 와 `RemoveNode` 가 `InternalSetRoot` 를 직접 호출해 `CatalogNode` 루트 금지 검사를 건너뛸 수 있었다. `NodeCatalogObjectChangeWatcher.cs` 는 `EditorUtility.EntityIdToObject` 를 `#if` 분기 없이 써서 Unity 2022.3 에서 컴파일되지 않았다.
- 현재: `_FindAnyOtherNode` 가 `CatalogNode` 를 후보에서 제외한다. `EntityIdToObject` 는 `#if UNITY_6000_3_OR_NEWER` 안에 있고 그 외 버전은 `InstanceIDToObject` 를 쓴다.

### 2026-05-11 :: `TextField.SetPlaceholderText` 사용 제거

- 이전: Unity API 의존 표에 "`TextField.SetPlaceholderText` - 6000+ 전용 - `#if UNITY_6000_0_OR_NEWER` 로 분기" 행이 있었다.
- 현재: 이 패키지 코드는 `SetPlaceholderText` 를 쓰지 않는다 (`HGraphWindow.cs` Dev Log `LOG-20260511-1`).
