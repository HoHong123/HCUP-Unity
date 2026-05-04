---
script_path: Assets/01_Scripts/HCUP-Unity/HUI/Runtime/HUI/DropDown/BaseDropDown.cs
script_name: BaseDropDown
latest_log_id: LOG-20260310-1
total_entries: 2
created: 2026-05-04
updated: 2026-05-04
---

# BaseDropDown Dev Log History

`Assets/01_Scripts/HCUP-Unity/HUI/Runtime/HUI/DropDown/BaseDropDown.cs` 의 Dev Log 엔트리 풀 본문이 본 파일에 시간 역순으로 보관됩니다 (최신이 위). 엔트리가 3 개 이하라 .cs 파일은 변경되지 않았으며, **본 history MD 가 ground truth**.

본 파일은 unity-devlog-history-mirror 스킬에 의해 사용자 명시 요청으로 생성되었습니다 (2026-05-04). .cs 의 카탈로그 헤더(`@Jason - PKH` 날짜 없음 + 네이티브 드롭다운 동작 안내) 는 엔트리가 아닌 영구 메타 설명이라 history MD 에 옮기지 않고 .cs 에 잔존시켰습니다.

==================================
@Jason - PKH 2026.03.10 [LOG-20260310-1]

주요 기능 ::
1. 드롭다운 항목 데이터를 기반으로 유닛 오브젝트를 생성합니다.
2. Toggle 상태에 따라 드롭다운 테이블의 활성/비활성을 제어합니다.
3. DirectionType과 Offset 값을 사용하여 테이블 Pivot 및 위치를 조정합니다.
4. 항목 선택 시 Value를 갱신하고 OnItemSelected 이벤트를 호출합니다.
5. 파생 클래스가 InitUnits(), SelectByIndex(int)를 통해 세부 동작을 구현하도록 강제합니다.

사용법 ::
1. BaseDropDown을 상속받는 파생 클래스를 작성합니다.
2. TData는 드롭다운 데이터 구조, TUnit은 드롭다운 항목 UI 스크립트로 지정합니다.
3. Inspector에서 dropTg, table, tableRect, unitParent, unitPrefab을 연결합니다.
4. Start 시 CreateUnits()로 항목 오브젝트를 생성한 뒤 InitUnits()에서 데이터와 유닛을 매핑합니다.
5. 항목 선택 시 OnSelect(index)를 호출하면 Value 변경과 함께 드롭다운이 닫힙니다.

기타 ::
1. 네이티브 Dropdown이 기존 레이아웃 시스템을 무시하는 문제를 대체하기 위한 커스텀 구조입니다.
2. unitPrefab에 TUnit이 없으면 런타임에 AddComponent로 추가합니다.
3. scene 상의 실제 prefab 오브젝트일 경우 CreateUnits() 이후 원본 unitPrefab은 비활성화합니다.
==================================
@Jason - PKH 23. 07. 2025 [LOG-20250723-1]
KOR ::
코드의 유연성과 유지보수를 고려하여 리펙토링 진행.
파생 클래스는 드롭다운에 사용될 데이터와 유닛 생성을 의무적으로 하도록 유도하였습니다.
필요시 조건에 맞는 외부 클래스를 사용이 가능하여 확장성을 보장하고
데이터 저장용도의 너무 간소한 클래스/구조체를 물리적 코드파일 생성하는 것을 내부 클래스/구조체 생성으로 방지합니다.
ENG ::
Refactoring was performed considering the flexibility and maintainability of the code.
In the derived class, the data and unit to be used for the dropdown were made mandatory to be created.
When necessary, an external class that meets the conditions can be used to ensure extensibility,
The creation of a physical code file for a class/structure that is too simple for just to store some datas can be prevented by creating an inner class/structure.
==================================
