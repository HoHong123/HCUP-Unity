# NPOI — Odin Inspector → Unity 네이티브 IMGUI 전환 문서 패키지

이 디렉토리는 `Assets/01_Scripts/02_Data/NPOI/` 시스템을 Odin Inspector 의존 없이
Unity 네이티브 IMGUI(`EditorGUILayout` + `ReorderableList` 기반)로 전환하기 위한
계획·매핑·패턴·마이그레이션·테스트케이스 문서를 담는다.

---

## 문서 인덱스

| 문서 | 내용 |
|---|---|
| [00_OVERVIEW.md](00_OVERVIEW.md) | 전환 배경, 현재 시스템 진단, 목표·비목표, 의존 그래프 |
| [01_MILESTONES.md](01_MILESTONES.md) | M1~M8 마일스톤 — 산출물·수용 기준·의존·위험 |
| [02_CONVERSION_TABLE.md](02_CONVERSION_TABLE.md) | Odin 어트리뷰트↔IMGUI 1:1 매핑 + 파일별 전환 난이도 표 |
| [03_NATIVE_GUI_PATTERNS.md](03_NATIVE_GUI_PATTERNS.md) | 기존 IMGUI 패턴 카탈로그 + NPOI 전용 헬퍼 클래스 설계 |
| [04_MIGRATION_DATA.md](04_MIGRATION_DATA.md) | 기존 `.asset` OdinSerializer 블록 마이그레이션 절차 |
| [05_TEST_CASES.md](05_TEST_CASES.md) | 마일스톤별 회귀 테스트케이스 (TC-FOUNDATION ~ TC-RUNTIME) |

---

## 의사결정 기록

| 결정 항목 | 결정 내용 | 결정 근거 |
|---|---|---|
| **Odin 제거 범위** | NPOI 디렉토리만 Odin-free화. Odin 패키지는 프로젝트에 잔존. | 영향 범위를 NPOI로 한정하여 리스크 최소화 |
| **GUI 기술** | IMGUI (`EditorGUILayout` + `ReorderableList`) | 기존 프로젝트 모든 네이티브 에디터(7+건)가 IMGUI 기반. Odin 어트리뷰트 1:1 매핑 직관적. UIToolkit 사례 0건. |
| **데이터 마이그레이션** | 전용 마이그레이션 EditorWindow 도구 + Excel 재import 병행 | `.asset`의 OdinSerializer 블록 손실 방지를 위한 이중 안전망 |
| **문서 구조** | 7개 .md 파일 분할 | 마일스톤 단계별 참조가 편하고 PR/커밋 단위와 자연스럽게 매핑됨 |

---

## 관련 코드 위치

| 파일 | 역할 |
|---|---|
| `Assets/01_Scripts/02_Data/NPOI/Core/DataEditorWindow.cs` | 시스템 진입점 (OdinMenuEditorWindow 상속) |
| `Assets/01_Scripts/02_Data/NPOI/Core/ExcelLoader.cs` | 모든 Loader의 베이스 추상 클래스 |
| `Assets/01_Scripts/02_Data/NPOI/Core/AssetDatabaseInstance.cs` | SO 싱글톤 접근 베이스 (SerializedScriptableObject 분기) |
| `Assets/01_Scripts/02_Data/NPOI/Sprite/SpriteCatalogSO.cs` | 가장 Odin 의존이 강한 SO (Dictionary 드로어 포함) |

---

## 진행 상태

- [x] **M1** — Inventory & Audit (본 문서 패키지 작성)
- [ ] **M2** — Foundation Refactor (`AssetDatabaseInstance` + Dictionary 어댑터)
- [ ] **M3** — ExcelLoader CustomEditor
- [ ] **M4** — DataEditorWindow 재구현 (OdinMenuEditorWindow → EditorWindow + IMGUI TreeView)
- [ ] **M5** — SpriteCatalog 전환
- [ ] **M6** — Per-Loader 일괄 전환 (17개 TableLoader)
- [ ] **M7** — 마이그레이션 도구 + Excel 재import
- [ ] **M8** — Cleanup & Verify
