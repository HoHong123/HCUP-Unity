# HWindows — 패키지 카드

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
| NodeCatalog | 10 | `NodeCatalogSO`, 노드 타입 3종, 엣지 3종, `NodeUID`, 저작 게이트 | [docs/NodeCatalog.md](docs/NodeCatalog.md) |
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

저장소를 통째로 가져다 쓴다 — 개별 UPM 설치는 현재 동작하지 않는다
([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 이 프로젝트 기준 6000.3.18f1. **아래 호환성 주의 참조** |
| `UnityEditor.Experimental.GraphView` | Editor 어셈블리의 토대 |

### 호환성 — C# 언어 버전

새 코드를 쓰거나 기존 코드를 고칠 때 **C# 10+ 문법을 쓰지 않는다** (file-scoped namespace,
global using, `required` 멤버, raw string literal, list pattern, primary constructor 등).
Unity 2022.3 기준 상한이 C# 9 이기 때문이다.

### Unity API 의존

| API | 도입 | 분기 정책 |
|---|---|---|
| `UnityEditor.Experimental.GraphView` | 2022.2+ | 2022.3~6000.3 범위에서 breaking change 없음. 어댑터 경계(`Core/` 2파일)로 격리 |
| `TextField.SetPlaceholderText` | 6000+ 전용 | `#if UNITY_6000_0_OR_NEWER` 로 분기 |
| `EditorUtility.EntityIdToObject` | **6000+ 전용** | **분기 없음 — 아래 주의할 점 1번** |
| `ScriptableSingleton<T>` / `ToolbarMenu` / `IMGUIContainer` / `ObjectField` / `Undo.undoRedoPerformed` | 2019~2022 | 안정. 분기 불요 |

---

## 주의할 점

1. **Unity 2022.3 에서는 컴파일되지 않는다.** `NodeCatalogObjectChangeWatcher.cs:18` 이
   `EditorUtility.EntityIdToObject`(Unity 6000+ 전용)를 `#if` 분기 없이 쓴다. 종전 이 문서가
   내걸던 "Unity 최저 2022.3.x LTS" 는 현재 코드와 맞지 않는다. 같은 저장소의
   `HLogConsole.Actions.cs:219-224` 는 `#if UNITY_6000_3_OR_NEWER` 로 분기하고 있어 대조가 뚜렷하다.
2. **`NodeCatalogAuthor` 의 루트 이전 경로가 자기 가드를 우회할 수 있다.** `PurgeNullNodes` 와
   `RemoveNode` 가 `InternalSetRoot` 를 직접 호출해 `CatalogNode` 루트 금지 검사를 건너뛴다.
3. **`FileBrowser` 서브모듈은 이 패키지에 없다.** 종전 문서가 "계획"으로 적어 둔 항목인데,
   실제 파일은 `HUtil/Editor/Odin/FileBrowser.cs` 에 따로 있다.
4. **소비처 0건 API** — `HubNode.SetEntryKey`, `NodeCatalogSO.EditorDescription`,
   `BaseNodeEdge.GetEdgeSummary`, `NodeCatalogAuthor.CreateHubNode`, `HGraphNode.GetOutputPort(int)`.
5. **`Runtime/NodeWindow/docs/README.md` 는 낡았다.** 노드 뷰 팩토리 시그니처를 `(node, catalog)`
   로 적었으나 실제 둘째 인자는 `isRoot` 이고, `NodeUIDDrawer` 경로도 실제와 다르며, Active
   하이라이트·Trace 모드가 빠져 있다. [Editor README](Editor/NodeWindow/README.md) 가 현행이다.

근거 라인은 [Editor README](Editor/NodeWindow/README.md) 의 "정리 대상" 절에 있다.
