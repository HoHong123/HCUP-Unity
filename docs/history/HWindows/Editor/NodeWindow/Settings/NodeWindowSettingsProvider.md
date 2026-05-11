---
script_path: Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/Settings/NodeWindowSettingsProvider.cs
script_name: NodeWindowSettingsProvider
latest_log_id: LOG-20260512-1
total_entries: 3
created: 2026-05-12
updated: 2026-05-12
---

# NodeWindowSettingsProvider Dev Log History

`Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/Settings/NodeWindowSettingsProvider.cs` 의 Dev Log 엔트리 풀 본문이 본 파일에 시간 역순으로 보관됩니다 (최신이 위). .cs 파일 안에는 최신 3 엔트리의 1-줄 요약 view 가 유지되며, **본 history MD 가 ground truth** — 모든 엔트리의 풀 본문은 손실 없이 본 파일에 보관됩니다.

본 파일은 unity-devlog-history-mirror 스킬에 의해 사용자 명시 요청으로 생성되었습니다 (2026-05-12). legacy 형식 엔트리 포함.

=============================================================================
@Jason - PKH 2026.05.12 Show Grid 미적용 버그픽스 — SnapSettingsChanged Action → Action<bool> [LOG-20260512-1]

# 변경
- SnapSettingsChanged 이벤트 시그니처: Action → Action<bool>.
- _DrawSnapSettings: ApplyModifiedProperties 전에 so.FindProperty("showGrid").boolValue 를 캡처.
  showGrid 값을 Invoke 인자로 직접 전달.

# 이유
- ApplyModifiedProperties 이후 ScriptableSingleton C# 필드 갱신이 지연될 수 있음.
  NodeSnapSettings.instance.ShowGrid 를 콜백에서 읽을 때 old 값을 반환하는 경우.
- SerializedProperty.boolValue (so 기준) 는 PropertyField 가 수정한 직후 값이므로 타이밍 무관 신뢰 가능.

# 결과
- _OnSnapSettingsChanged(bool showGrid) 가 올바른 값 수신.
  HGraphCanvas 에서 gridBackground.style.display 갱신 정상화.

=============================================================================
@Jason - PKH 2026.05.09 UID Registry UI 섹션 제거 [LOG-20260509-1]

# 삭제
- _DrawUIDRegistry() 메서드 제거
- DrawSettingsGUI 내 _DrawUIDRegistry() 호출 제거
- using HWindows.Editor.NodeWindow.Identity 제거
- NodeUIDRegistry 삭제에 따른 의존 정리

=============================================================================
2026-05-08 (최초 설계) :: Phase 1-E P1E-α/θ + Q4 D 채택 [LOG-20260508-1]
=============================================================================

변경 / SettingsProvider + DrawSettingsGUI 공유 헬퍼 + SnapSettingsChanged event.
이유 / Q4 D — Project Settings 페이지 + HGraphWindow Toolbar 사이드패널 양쪽 진입점.
       DRY 단일 진입점 (P1E-7) — DrawSettingsGUI 가 SettingsProvider.guiHandler 와
       IMGUIContainer 양쪽에서 호출.
결과 / 한 인스턴스 SerializedObject 양쪽 자동 동기. HGraphCanvas 가 SnapSettingsChanged
       구독해 GridBackground.visible + 시각 갱신.
주의 / NodeUIDRegistry 영역은 EditorGUI.BeginDisabledGroup 으로 ReadOnly (P1E-8).
       Reset 버튼 미배치 — Phase 0 의 "삭제 UID 재사용 금지" 데이터 무결성 보호.
       PeekNext() 반환형 = int (NodeUID 아님) — .Value 접근 불필요.

=============================================================================
