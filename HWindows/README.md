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

| 서브모듈 | asmdef | 상태 | 선택 의존 |
|---|---|---|---|
| `NodeWindow` | `HCUP.HWindows.NodeWindow.Editor` (Editor) | 활성 | — |
| `NodeWindow` (Runtime) | `HCUP.HWindows.NodeWindow` (Runtime) | 활성 | HInspector, HCollection |
| `FileBrowser` | `HCUP.HWindows.FileBrowser.Editor` | 계획 | Sirenix.OdinInspector |

---

## NodeWindow 서브모듈

GraphView 기반 노드 에디터 윈도우입니다. 데이터 컨테이너(`NodeCatalogSO`) 와 에디터 비주얼(`HGraphCanvas`) 이 분리된 구조로 되어 있습니다.

**진입 메뉴:** `Window ▸ HWindows ▸ Node Window ▸ Graph Editor`

### 디렉토리 구성

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
    SimpleNode.cs            — 기본 입출력 노드
    HubNode.cs               — 키 기반 1→N 라우팅 노드
    CatalogNode.cs           — 다른 카탈로그 참조 노드
    BaseNodeEdge.cs          — 엣지 추상 베이스
    SimpleNodeEdge.cs        — SimpleNode 간 엣지
    HubNodeEdge.cs           — HubNode 출구 포트 키 포함 엣지
  Identity/
    NodeUID.cs               — GUID 기반 노드 고유 식별자 (struct)
```

### 핵심 타입

| 타입 | 위치 | 역할 |
|---|---|---|
| `NodeCatalogSO` | Runtime | 그래프 전체 데이터 컨테이너. 노드/엣지/루트 UID를 HDictionary + List로 직렬화 |
| `BaseNode` | Runtime | 노드 데이터 ScriptableObject. `editorPosition`, `isFoldoutOpen` 에디터 상태 포함 |
| `NodeUID` | Runtime | GUID 기반 struct. `.None`으로 유효성 구분. `NodeUID.New()`로 발급 |
| `NodeCatalogAuthor` | Editor | 정적 클래스. catalog 변경의 유일한 진입점. Undo 그룹, dirty, SaveAssets 일괄 처리 |
| `HGraphCanvas` | Editor | `GraphView` 상속. 카탈로그 바인드 → `_PopulateInternal` → 노드/엣지 VisualElement 재구성 |
| `HGraphWindow` | Editor | `EditorWindow` 상속. Toolbar(검색/카탈로그 필드/설정 토글) + 메뉴바([View▾][Edit▾]) 빌드 |

### 노드 타입

| 노드 | ScriptableObject 클래스 | 시각 클래스 | 특징 |
|---|---|---|---|
| Simple | `SimpleNode` | `HGraphNode` | 입구 1 + 출구 1 |
| Hub | `HubNode` | `HGraphHubNode` | 입구 1 + 출구 N (키 목록 기반). 키 추가/제거 시 포트 수 자동 동기화 |
| Catalog | `CatalogNode` | `HGraphCatalogNode` | 다른 카탈로그 참조. 더블클릭 시 윈도우가 해당 카탈로그로 전환 |

### 주요 기능

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

### 사용법

**카탈로그 생성**

```
Project 창 우클릭 → Create → HWindows → Node Catalog
```

**윈도우에 바인드**

```
1. Window ▸ HWindows ▸ Node Window ▸ Graph Editor 열기
2. Toolbar의 ObjectField에서 NodeCatalogSO 선택, 또는
3. Project 창에서 NodeCatalogSO를 캔버스에 드래그드롭
```

**다른 카탈로그로 전환**

```
1. 다른 NodeCatalogSO를 캔버스에 드래그드롭 → 참조 노드(CatalogNode) 생성
2. 생성된 CatalogNode를 더블클릭 → 해당 카탈로그로 자동 전환
3. Toolbar ObjectField에서 직접 선택 가능
```

**Settings 패널 (`ProjectSettings > HCUP > Node Window`)**

```
- Snap Mode: Off / OnShiftHold / Always
- Grid Unit: 1~100 (px, 기본 20)
- Show Grid: GridBackground 표시 여부
```

### 경계 규칙

1. **Experimental API 격리** — `UnityEditor.Experimental.GraphView`를 직접 `using` 하는 파일은 `Core/HGraphCanvas.cs`, `Core/HGraphNode.cs` 두 파일로만 제한합니다. 외부 코드는 `HWindows.Editor.NodeWindow` 네임스페이스만 소비합니다.

2. **단일 mutation 게이트** — `NodeCatalogSO` 변경(노드 생성/삭제/엣지 연결 등)은 반드시 `NodeCatalogAuthor`를 통해서만 수행합니다. `catalog.Internal*` 메서드를 직접 호출하지 않습니다.

3. **HCUP 계층 경계** — `HWindows` 패키지는 `HUtil`에만 의존합니다. `HGame`, `HUI`를 역방향 참조하지 않습니다.

---

## 주의사항

- `NodeCatalogSO`를 Unity MCP의 `assets-modify`로 수정하지 않습니다. 명시하지 않은 필드가 기본값(0)으로 리셋됩니다. YAML 직접 수정 + `assets-refresh` 조합을 사용합니다.
- 노드 sub-asset을 Project 창에서 직접 삭제하면 `HDictionary` UID 키는 남고 참조만 null이 됩니다(ghost UID). 윈도우를 다시 열면 자동 정리됩니다. 삭제가 필요할 때는 우클릭 컨텍스트 메뉴 `Delete`를 사용합니다.
- Unity 2022.3에서 `TextField.SetPlaceholderText`를 사용하면 CS1061 컴파일 오류가 발생합니다. 이 API는 `#if UNITY_6000_0_OR_NEWER` 분기 또는 사용 제거로 처리합니다.
- `package.json`의 `"unity": "2021.3"` 항목은 패키지 배포 호환 선언이며, 실제 동작 검증 최저 버전은 **2022.3.x LTS**입니다.

---

## Dev Log 이력

각 파일의 전체 변경 이력은 `docs/history/HWindows/Editor/NodeWindow/` 에 보관됩니다.

| 파일 | 이력 |
|---|---|
| `HGraphCanvas.cs` | [HGraphCanvas.md](docs/history/HWindows/Editor/NodeWindow/Core/HGraphCanvas.md) |
| `HGraphWindow.cs` | [HGraphWindow.md](docs/history/HWindows/Editor/NodeWindow/Core/HGraphWindow.md) |
| `HGraphNode.cs` | [HGraphNode.md](docs/history/HWindows/Editor/NodeWindow/Core/HGraphNode.md) |
| `HGraphHubNode.cs` | [HGraphHubNode.md](docs/history/HWindows/Editor/NodeWindow/Core/HGraphHubNode.md) |
| `HGraphCatalogNode.cs` | [HGraphCatalogNode.md](docs/history/HWindows/Editor/NodeWindow/Core/HGraphCatalogNode.md) |
| `HGraphEdge.cs` | [HGraphEdge.md](docs/history/HWindows/Editor/NodeWindow/Core/HGraphEdge.md) |
| `HGraphClipboard.cs` | [HGraphClipboard.md](docs/history/HWindows/Editor/NodeWindow/Core/HGraphClipboard.md) |
| `NodeCatalogAuthor.cs` | [NodeCatalogAuthor.md](docs/history/HWindows/Editor/NodeWindow/NodeCatalog/Authoring/NodeCatalogAuthor.md) |
| `NodeCatalogObjectChangeWatcher.cs` | [NodeCatalogObjectChangeWatcher.md](docs/history/HWindows/Editor/NodeWindow/NodeCatalog/Authoring/NodeCatalogObjectChangeWatcher.md) |
| `NodeSnapSettings.cs` | [NodeSnapSettings.md](docs/history/HWindows/Editor/NodeWindow/Settings/NodeSnapSettings.md) |
| `NodeWindowSettingsProvider.cs` | [NodeWindowSettingsProvider.md](docs/history/HWindows/Editor/NodeWindow/Settings/NodeWindowSettingsProvider.md) |
