# NodeWindow

GraphView 기반 노드 에디터 윈도우 서브모듈입니다. 데이터 컨테이너(`NodeCatalogSO`)와 에디터 비주얼(`HGraphCanvas`)이 분리된 구조로 되어 있습니다.

**사용 방식:** `HGraphWindow`를 직접 열지 않습니다. 기능별 파생 창을 `HGraphWindow<TCatalog>`로 구현해 사용합니다. 아래 [파생 기능 창 구현 가이드](#파생-기능-창-구현-가이드)를 참고하세요.

---

## 디렉토리 구성

```
Editor/NodeWindow/
  Core/
    HGraphWindow.cs          — EditorWindow 진입점, Toolbar, 메뉴바 빌드
    HGraphCanvas.cs          — GraphView 어댑터 (pan/zoom/selection/snap)
    HGraphNode.cs            — BaseNode 1개에 대응하는 VisualElement (Node 상속)
    HGraphCatalogNode.cs     — CatalogNode 전용 시각 노드 (더블클릭 → 카탈로그 전환)
    HGraphHubNode.cs         — HubNode 전용 시각 노드 (동적 N개 출구 포트)
    HGraphEdge.cs            — BaseNodeEdge에 대응하는 Edge VisualElement
    HGraphClipboard.cs       — 노드 Cut/Paste 직렬화 (JSON)
    HGraphNodeStyles.cs      — USS 클래스 상수
  NodeCatalog/
    Authoring/
      NodeCatalogAuthor.cs          — catalog 변경의 단일 게이트 (정적 Editor-only)
      NodeCatalogObjectChangeWatcher.cs — 외부 SO 변경 감지 → repopulate
    Identity/
      NodeUIDDrawer.cs       — NodeUID 인스펙터 PropertyDrawer
  Settings/
    NodeSnapSettings.cs      — 스냅/그리드 설정 ScriptableSingleton
    NodeWindowSettingsProvider.cs — Project Settings HCUP 페이지
    SnapMode.cs              — Off / OnShiftHold / Always 열거형
  UI/
    HGraphWindow.uss         — 윈도우 레이아웃 스타일
    HGraphNode.uss           — 노드 VisualElement 스타일

Runtime/NodeWindow/
  NodeCatalog/
    NodeCatalogSO.cs         — 그래프 데이터 컨테이너 (ScriptableObject)
    BaseNode.cs              — 모든 노드의 추상 베이스 (ScriptableObject)
    SimpleNode.cs            — 인프라 검증용 최소 구현 (직접 사용 금지 — 기능별 BaseNode 파생 노드로 교체)
    HubNode.cs               — 키 기반 1→N 라우팅 노드 (직접 인스턴스화 금지 — 기능별 HubNode 파생 노드로 교체)
    CatalogNode.cs           — 다른 카탈로그 참조 노드
    BaseNodeEdge.cs          — 엣지 추상 베이스
    SimpleNodeEdge.cs        — 비-허브 노드 간 엣지 (내부 연결 전용)
    HubNodeEdge.cs           — HubNode 출구 포트 키 포함 엣지
  Identity/
    NodeUID.cs               — GUID 기반 노드 고유 식별자 (struct)
```

---

## 핵심 타입

| 타입 | 위치 | 역할 |
|---|---|---|
| `NodeCatalogSO` | Runtime | 그래프 전체 데이터 컨테이너. 노드/엣지/루트 UID를 HDictionary + List로 직렬화 |
| `BaseNode` | Runtime | 노드 데이터 ScriptableObject. `editorPosition`, `isFoldoutOpen` 에디터 상태 포함 |
| `NodeUID` | Runtime | GUID 기반 struct. `.None`으로 유효성 구분. `NodeUID.New()`로 발급 |
| `NodeCatalogAuthor` | Editor | 정적 클래스. catalog 변경의 유일한 진입점. Undo 그룹, dirty, SaveAssets 일괄 처리 |
| `HGraphCanvas` | Editor | `GraphView` 상속. 카탈로그 바인드 → `_PopulateInternal` → 노드/엣지 VisualElement 재구성 |
| `HGraphWindow` | Editor | `EditorWindow` 상속. Toolbar(검색/카탈로그 필드/설정 토글) + 메뉴바([View▾][Edit▾]) 빌드 |

---

## 노드 타입

| 노드 | ScriptableObject 클래스 | 시각 클래스 | 특징 |
|---|---|---|---|
| Simple | `SimpleNode` | `HGraphNode` | 인프라 검증용. **직접 사용 금지** — 기능별 `BaseNode` 파생 노드를 구현할 것 |
| Hub | `HubNode` | `HGraphHubNode` | 입구 1 + 출구 N (키 목록 기반). **직접 인스턴스화 금지** — 기능별 `HubNode` 파생 노드를 구현할 것 |
| Catalog | `CatalogNode` | `HGraphCatalogNode` | 다른 카탈로그 참조. 더블클릭 시 윈도우가 해당 카탈로그로 전환 |

---

## 주요 기능

| 기능 | 진입점 | 비고 |
|---|---|---|
| 노드 생성/삭제 | 우클릭 컨텍스트 메뉴 | Undo/Redo 지원 |
| 노드 복제 | 우클릭 `Duplicate` | 위치 (+40, +40) 자동 오프셋 |
| Cut / Paste | 우클릭 또는 `Ctrl+X` / `Ctrl+V` | JSON 클립보드 경유, 항상 새 UID 발급 |
| 엣지 연결/해제 | 포트 드래그 | 자기 루프, 중복 엣지 거부. ghost UID 엣지 생성 차단 |
| 루트 노드 지정 | 우클릭 또는 `Edit ▸ Set as Root` | CatalogNode는 루트 지정 불가 |
| Go To Root | `View ▸ Go To Root` 또는 `Ctrl+Home` | 뷰포트를 루트 노드 위치로 이동 |
| Close All Foldouts | `View ▸ Close All Foldouts` 또는 `Ctrl+0` | 열린 노드 본문 일괄 접기 |
| Select All | `Edit ▸ Select All` 또는 `Ctrl+A` | 캔버스 내 모든 노드 선택 |
| 타이틀 검색 | Toolbar `Search` 입력 | Enter로 다음 결과 순환, ESC로 초기화 |
| 스냅 | `Shift` 드래그 또는 Settings 패널 | `OnShiftHold` / `Always` / `Off` 세 가지 모드 |
| 그리드 배경 | Settings 패널 `Show Grid` 토글 | `NodeSnapSettings.showGrid` 연동 |
| Inspector 동기화 | 노드 선택 | `Selection.activeObject = dataNode` — 클릭 즉시 Inspector에 SO 표시 |
| Ghost UID 정리 | 카탈로그 열기 / repopulate 시 자동 | Project 창에서 sub-asset 직접 삭제 후 생긴 null 참조 자동 제거 |
| CatalogNode 양방향 생성 | 카탈로그 드래그드롭 | A→B 연결 시 B에 A 참조 CatalogNode 자동 생성 (1:1 관계) |
| HubNode 키 실시간 동기화 | Inspector에서 키 편집 | `HubNode.OnValidate` → `KeysChanged` 이벤트 → 포트 수/라벨 즉시 갱신 |

---

## 사용법

`HGraphWindow`를 직접 열지 않습니다. 아래 [파생 기능 창 구현 가이드](#파생-기능-창-구현-가이드)를 따라 기능별 전용 창을 구현한 뒤, 해당 창을 열어 사용합니다.

**카탈로그 생성**

```
Project 창 우클릭 → Create → HWindows → [기능별 CreateAssetMenu 경로]
```

**윈도우에 바인드**

```
1. 기능별 파생 창 메뉴 항목으로 열기 (예: HCUP/Dialogue/Open Node Window)
2. Toolbar의 ObjectField에서 기능 전용 CatalogSO 선택, 또는
3. Project 창에서 CatalogSO를 캔버스에 드래그드롭
```

**다른 카탈로그로 전환**

```
1. 다른 CatalogSO를 캔버스에 드래그드롭 → CatalogNode 생성
2. CatalogNode 더블클릭 → 해당 카탈로그로 자동 전환
3. Toolbar ObjectField에서 직접 선택 가능
```

**Settings 패널 (`ProjectSettings > HCUP > Node Window`)**

```
- Snap Mode: Off / OnShiftHold / Always
- Grid Unit: 1~100 (px, 기본 20)
- Show Grid: GridBackground 표시 여부
```

---

## 경계 규칙

1. **Experimental API 격리** — `UnityEditor.Experimental.GraphView`를 직접 `using` 하는 파일은 `Core/HGraphCanvas.cs`, `Core/HGraphNode.cs` 두 파일로만 제한합니다. 외부 코드는 `HWindows.Editor.NodeWindow` 네임스페이스만 소비합니다.

2. **단일 mutation 게이트** — `NodeCatalogSO` 변경(노드 생성/삭제/엣지 연결 등)은 반드시 `NodeCatalogAuthor`를 통해서만 수행합니다. `catalog.Internal*` 메서드를 직접 호출하지 않습니다.

3. **HCUP 계층 경계** — `HWindows` 패키지는 `HUtil`에만 의존합니다. `HGame`, `HUI`를 역방향 참조하지 않습니다.

---

## 파생 기능 창 구현 가이드

`HGraphWindow`는 직접 열거나 사용하지 않습니다. 기능별 전용 창을 `HGraphWindow<TCatalog>`로 파생해 구현합니다.

### 1단계 — 기능 전용 카탈로그 SO

`NodeCatalogSO`를 상속한 기능 전용 SO를 선언합니다.

```csharp
[CreateAssetMenu(menuName = "HWindows/MyFeature/My Feature Catalog")]
public sealed class MyFeatureCatalogSO : NodeCatalogSO { }
```

### 2단계 — 기능 노드 클래스

단일 출구 노드는 `BaseNode`, 다중 출구(분기) 노드는 `HubNode`를 상속합니다.

```csharp
// 단일 출구 — BaseNode 상속
public sealed class MyLineNode : BaseNode {
    [SerializeField] string text;
    public string Text => text;
    public override string ClipboardMagic => "HGRAPH_MYFEATURE_LINE_NODE_V1";
}

// 다중 출구 — HubNode 상속
public sealed class MyBranchNode : HubNode {
    [SerializeField] string conditionKey;
    public string ConditionKey => conditionKey;
    public override string ClipboardMagic => "HGRAPH_MYFEATURE_BRANCH_NODE_V1";
}
```

**규칙:**
- `SimpleNode`를 직접 사용하거나 상속하지 않습니다. 인프라 검증용이며 신규 사용 금지.
- `HubNode`를 직접 인스턴스화하지 않습니다. 기능 전용 파생 클래스를 통해서만 사용합니다.
- `ClipboardMagic`은 도메인별 고유 문자열로 선언합니다. Cut/Paste 역직렬화의 타입 식별자입니다. 리네임 시 기존 클립보드 페이로드와 호환이 깨집니다.

### 3단계 — 기능 전용 창 선언

```csharp
using System;
using HWindows.Editor.NodeWindow;
using HWindows.Editor.NodeWindow.Authoring;
using HWindows.NodeWindow;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class MyFeatureNodeWindow : HGraphWindow<MyFeatureCatalogSO> {

    [MenuItem("MyProject/MyFeature/Open Node Window", false, 10)]
    public static void Open() {
        MyFeatureNodeWindow window = GetWindow<MyFeatureNodeWindow>();
        window.titleContent = new GUIContent("MyFeature Graph Editor");
        window.minSize = new Vector2(400, 300);
    }

    protected override void CreateGUI() {
        base.CreateGUI();           // 반드시 먼저 호출 — canvas 초기화
        _WireFeatureContextMenu();  // canvas 준비 후 주입
    }

    protected override void _AppendExtraMenuBarItems(VisualElement menuBar) {
        ToolbarMenu createMenu = new ToolbarMenu { text = "Create" };
        _AppendFeatureNodeItems(createMenu.menu, _ => canvas.GetViewportCenterWorld());
        menuBar.Add(createMenu);
    }

    void _WireFeatureContextMenu() {
        canvas.AdditionalContextMenuActions = evt => {
            if (currentCatalog == null) return;
            _AppendFeatureNodeItems(evt.menu,
                action => canvas.ToGraphPosition(action.eventInfo.localMousePosition));
        };
    }

    void _AppendFeatureNodeItems(DropdownMenu menu, Func<DropdownMenuAction, Vector2> getPosition) {
        DropdownMenuAction.Status Status(DropdownMenuAction _) =>
            currentCatalog != null
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled;

        menu.AppendAction("My Line Node",
            a => _CreateNodeAt<MyLineNode>(getPosition(a)), Status);
        menu.AppendAction("My Branch Node (Hub)",
            a => _CreateNodeAt<MyBranchNode>(getPosition(a)), Status);
    }

    void _CreateNodeAt<T>(Vector2 position) where T : BaseNode {
        if (currentCatalog == null) return;
        NodeCatalogAuthor.CreateNode<T>(currentCatalog, position);
    }
}
```

**주의:**
- `base.CreateGUI()` 이후 `_WireFeatureContextMenu()` 호출 순서는 변경 불가. `base.CreateGUI()`가 `canvas`를 초기화한다.
- `_AppendExtraMenuBarItems`는 `base.CreateGUI()` 내부에서 호출된다. `canvas`가 null인 시점이므로 `canvas`에 접근하지 않는다.
- 제너릭 파라미터 `<MyFeatureCatalogSO>`가 `ObjectField` 타입을 자동으로 제한한다. `currentCatalog`는 항상 `MyFeatureCatalogSO` 타입이므로 내부 캐스팅이 불필요하다.

### 4단계 — 노드 뷰 팩토리 등록 (선택)

기본 `HGraphNode`로 충분하면 생략합니다. 노드 타입별 헤더 색상이나 본문 미리보기가 필요한 경우에만 구현합니다.

```csharp
[InitializeOnLoadMethod]
static void _Register() {
    // 노드 타입별 커스텀 뷰 팩토리
    HGraphCanvas.RegisterNodeViewFactory(
        typeof(MyLineNode),
        (node, catalog) => new HGraphNode(node, catalog));  // 커스텀 뷰로 교체 가능

    // 헤더 색 등록
    HGraphNodeStyles.RegisterHeaderColor(typeof(MyLineNode), new Color(0.2f, 0.4f, 0.8f));
}
```

### 5단계 — 어셈블리 참조 설정

기능 에디터 어셈블리의 `.asmdef`에 아래 참조를 추가합니다.

| 참조 | 이유 |
|---|---|
| `HCUP.HWindows.NodeWindow.Editor` | HGraphWindow, HGraphCanvas, NodeCatalogAuthor |
| `HCUP.HWindows.NodeWindow` | BaseNode, HubNode, NodeCatalogSO |
| 기능 런타임 어셈블리 | MyFeatureCatalogSO, MyLineNode 등 |

> `HCUP.HWindows.NodeWindow`의 `autoReferenced`는 `false`입니다. 명시 참조 없이 사용하면 CS0012 오류가 발생합니다.

### 우클릭 메뉴 구성 규칙

| 항목 | 제공처 | 비고 |
|---|---|---|
| 기능별 노드 생성 | 파생 창 (`AdditionalContextMenuActions` 경유) | 각 노드 타입별 항목 나열 |
| 붙여넣기 (Paste) | `HGraphCanvas` 고정 | 클립보드 유효 시 활성 |
| 모두 선택 (Select All) | `HGraphCanvas` 고정 | 노드 1개 이상 시 활성 |

`HGraphCanvas.BuildContextualMenu`는 기능별 노드 생성 항목을 직접 포함하지 않습니다. 모든 기능별 노드 생성은 파생 창의 `AdditionalContextMenuActions`를 통해 주입합니다.

---

## 주의사항

- `NodeCatalogSO`를 Unity MCP의 `assets-modify`로 수정하지 않습니다. 명시하지 않은 필드가 기본값(0)으로 리셋됩니다. YAML 직접 수정 + `assets-refresh` 조합을 사용합니다.
- 노드 sub-asset을 Project 창에서 직접 삭제하면 `HDictionary` UID 키는 남고 참조만 null이 됩니다(ghost UID). 윈도우를 다시 열면 자동 정리됩니다. 삭제가 필요할 때는 우클릭 컨텍스트 메뉴 `Delete`를 사용합니다.
- Unity 2022.3에서 `TextField.SetPlaceholderText`를 사용하면 CS1061 컴파일 오류가 발생합니다. 이 API는 `#if UNITY_6000_0_OR_NEWER` 분기 또는 사용 제거로 처리합니다.
- `package.json`의 `"unity": "2021.3"` 항목은 패키지 배포 호환 선언이며, 실제 동작 검증 최저 버전은 **2022.3.x LTS**입니다.

---

## Dev Log 이력

각 파일의 전체 변경 이력은 `../../../../docs/history/HWindows/Editor/NodeWindow/` 에 보관됩니다.

| 파일 | 이력 |
|---|---|
| `HGraphCanvas.cs` | [HGraphCanvas.md](../../../../docs/history/HWindows/Editor/NodeWindow/Core/HGraphCanvas.md) |
| `HGraphWindow.cs` | [HGraphWindow.md](../../../../docs/history/HWindows/Editor/NodeWindow/Core/HGraphWindow.md) |
| `HGraphNode.cs` | [HGraphNode.md](../../../../docs/history/HWindows/Editor/NodeWindow/Core/HGraphNode.md) |
| `HGraphHubNode.cs` | [HGraphHubNode.md](../../../../docs/history/HWindows/Editor/NodeWindow/Core/HGraphHubNode.md) |
| `HGraphCatalogNode.cs` | [HGraphCatalogNode.md](../../../../docs/history/HWindows/Editor/NodeWindow/Core/HGraphCatalogNode.md) |
| `HGraphEdge.cs` | [HGraphEdge.md](../../../../docs/history/HWindows/Editor/NodeWindow/Core/HGraphEdge.md) |
| `HGraphClipboard.cs` | [HGraphClipboard.md](../../../../docs/history/HWindows/Editor/NodeWindow/Core/HGraphClipboard.md) |
| `NodeCatalogAuthor.cs` | [NodeCatalogAuthor.md](../../../../docs/history/HWindows/Editor/NodeWindow/NodeCatalog/Authoring/NodeCatalogAuthor.md) |
| `NodeCatalogObjectChangeWatcher.cs` | [NodeCatalogObjectChangeWatcher.md](../../../../docs/history/HWindows/Editor/NodeWindow/NodeCatalog/Authoring/NodeCatalogObjectChangeWatcher.md) |
| `NodeSnapSettings.cs` | [NodeSnapSettings.md](../../../../docs/history/HWindows/Editor/NodeWindow/Settings/NodeSnapSettings.md) |
| `NodeWindowSettingsProvider.cs` | [NodeWindowSettingsProvider.md](../../../../docs/history/HWindows/Editor/NodeWindow/Settings/NodeWindowSettingsProvider.md) |
