# HCUP.HInspector

Unity Inspector 확장 attribute 모음 + CustomEditor 렌더러.

## 구성

| asmdef | 범위 | 역할 |
|---|---|---|
| `HCUP.HInspector` | Runtime | H-attribute 타입 정의 |
| `HCUP.HInspector.Editor` | Editor | `HInspectorEditor` CustomEditor 렌더러 + public IMGUI helper |
| `HCUP.HInspector.Odin.Editor` | Editor (Odin 선택) | Odin Inspector 브릿지 |

## Attribute 목록

`HInspectorBehaviour` 또는 `HInspectorScriptableObject` 를 상속한 타겟에서 동작.

| Attribute | 효과 |
|---|---|
| `[HTitle("그룹명")]` | 볼드 라벨 + 구분선 섹션 헤더 |
| `[HBoxGroup("그룹명")]` | 테두리 박스 그룹 |
| `[HHorizontalGroup("그룹명")]` | 가로 배치 그룹 |
| `[HVerticalGroup("그룹명")]` | 세로 배치 그룹 |
| `[HButton]` | 메서드 → Inspector 버튼 |
| `[HShowInInspector]` | 직렬화되지 않은 멤버 읽기 전용 노출 |
| `[HReadOnly]` | 필드 편집 불가 회색 처리 |
| `[HHideLabel]` | 필드 라벨 숨김 |

## Public API (Editor 계층)

### `HTitleDrawer.Draw(string title)` (1.0.3+)

`SettingsProvider`, `IMGUIContainer` 등 `CustomEditor` 경로 밖의 임의 IMGUI 영역에서
`HTitle` attribute 와 동일한 시각 효과 (볼드 라벨 + 1px 구분선) 를 적용한다.

```csharp
using HInspector.Editor;

void OnGUI(string searchContext) {
    HTitleDrawer.Draw("Snap Settings");
    EditorGUILayout.PropertyField(...);
}
```

`HTitle` attribute 와 시각 규격을 공유 (DRY 단일 진입점 — `HInspectorEditor` 도 내부에서 본 helper 를 위임 호출).
