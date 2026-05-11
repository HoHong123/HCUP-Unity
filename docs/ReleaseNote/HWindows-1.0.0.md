# HWindows 1.0.0 릴리즈 노트

---

## 개요

HWindows 패키지 최초 릴리즈입니다. GraphView 기반 노드 에디터 윈도우 서브모듈(`NodeWindow`)이 포함됩니다.

**진입 메뉴:** `Window ▸ HWindows ▸ Node Window ▸ Graph Editor`

## 포함 서브모듈

---

| 서브모듈 | 상태 |
|---|---|
| `NodeWindow` | 활성 |
| `FileBrowser` | 계획 (미포함) |

## NodeWindow 주요 기능

---

**노드 타입**
- `SimpleNode` — 입구 1 + 출구 1 기본 노드.
- `HubNode` — 입구 1 + 출구 N. Inspector에서 키 목록 편집 시 포트 수/라벨 실시간 동기화.
- `CatalogNode` — 다른 `NodeCatalogSO` 참조 노드. 더블클릭 시 해당 카탈로그로 윈도우 전환.

**데이터 계약**
- `NodeCatalogSO` — 그래프 전체 데이터 컨테이너 ScriptableObject. 노드/엣지를 `HDictionary<NodeUID, BaseNode>` + `[SerializeReference] List<BaseNodeEdge>`로 직렬화.
- `NodeUID` — GUID 기반 struct. `.None`으로 유효성 구분. 삭제된 UID 재사용 금지.

**Authoring 인프라**
- `NodeCatalogAuthor` — 정적 클래스. 노드/엣지 변경의 단일 진입점. Undo 그룹 / dirty / SaveAssets 일괄 처리.
- `NodeCatalogObjectChangeWatcher` — 외부 SO 변경 감지 → 캔버스 repopulate 트리거.

**상호작용**
- 노드 생성/삭제/복제/Cut/Paste (JSON 클립보드), Undo/Redo 지원.
- 엣지 연결/해제: 자기 루프, 중복 엣지 거부. ghost UID 엣지 생성 차단.
- 그리드 스냅 (Off / OnShiftHold / Always), Show Grid 토글.
- 타이틀 Search: Enter 다음 결과 순환, ESC 초기화.
- 메뉴바: `[View▾]` (Go To Root / Close All Foldouts) + `[Edit▾]` (Select All / Set as Root).
- Inspector 동기화: 노드 선택 시 `Selection.activeObject = dataNode` → Inspector에 SO 즉시 표시.

**Settings**
- `ProjectSettings > HCUP > Node Window` 페이지 (NodeWindowSettingsProvider).
- HGraphWindow Toolbar Settings 사이드패널(280px)과 DRY 공유.

## 호환성

---

| 항목 | 범위 |
|---|---|
| Unity 최저 | 2022.3.x LTS |
| Unity 최고 (검증) | 6000.3.11f1 |
| C# 언어 버전 | C# 9 상한 |

Unity 6000+ 전용 API(`TextField.SetPlaceholderText` 등)는 `#if UNITY_6000_0_OR_NEWER` 분기 적용. `UnityEditor.Experimental.GraphView`는 2022.3 ~ 6000.3 범위에서 breaking change 없음을 확인했습니다.

## 의존성

---

- Runtime 어셈블리(`HCUP.HWindows.NodeWindow`): `HCollection`, `HInspector` (선택 의존)
- Editor 어셈블리(`HCUP.HWindows.NodeWindow.Editor`): `HUtil`
- `HGame`, `HUI` 역방향 참조 없음.

## 주의사항

---

- `NodeCatalogSO`를 Unity MCP의 `assets-modify`로 수정하면 명시하지 않은 필드가 기본값(0)으로 리셋됩니다. YAML 직접 수정 + `assets-refresh` 조합을 사용합니다.
- 노드 sub-asset을 Project 창에서 직접 삭제하면 ghost UID가 남습니다. 윈도우를 다시 열면 자동 정리됩니다. 삭제는 우클릭 메뉴 `Delete`를 사용합니다.
- `Experimental.GraphView` 직접 `using`은 `Core/HGraphCanvas.cs`, `Core/HGraphNode.cs` 두 파일로만 제한합니다.

## 동봉 태그

---

- HWindows-1.0.0
- v1.1.1 (umbrella)

Full Changelog: https://github.com/HoHong123/HCUP-Unity/releases/tag/HWindows-1.0.0
