---
script_path: Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/Settings/NodeSnapSettings.cs
script_name: NodeSnapSettings
latest_log_id: LOG-20260508-1
total_entries: 1
created: 2026-05-12
updated: 2026-05-12
---

# NodeSnapSettings Dev Log History

`Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/Settings/NodeSnapSettings.cs` 의 Dev Log 엔트리 풀 본문이 본 파일에 시간 역순으로 보관됩니다 (최신이 위). .cs 파일 안에는 최신 3 엔트리의 1-줄 요약 view 가 유지되며, **본 history MD 가 ground truth** — 모든 엔트리의 풀 본문은 손실 없이 본 파일에 보관됩니다.

본 파일은 unity-devlog-history-mirror 스킬에 의해 사용자 명시 요청으로 생성되었습니다 (2026-05-12). 엔트리 1개로 최초 생성 — legacy 형식 엔트리 포함.

=============================================================================
2026-05-08 (최초 설계) :: Phase 1-E P1E-α/β/γ/δ 채택 [LOG-20260508-1]
=============================================================================

변경 / ScriptableSingleton<NodeSnapSettings> 신규. 3 필드 (gridUnit/showGrid/mode).
이유 / Phase 1-E 의 "Shift 스냅 + Grid 가시 + Snap mode" 가 NodeWindow 시스템 단일
       설정 보관 자리. NodeUIDRegistry 와 동일 ScriptableSingleton 패턴 채택 —
       새 인프라 학습 비용 0.
결과 / Project Settings > HCUP > Node Window 페이지 + HGraphWindow Toolbar 사이드패널
       양쪽 같은 instance 공유. 후속 phase 가 settings 항목 추가할 확장점.
주의 / 외부 노출은 getter 만. 사후 필드 추가 시 직렬 default 0 리셋 리스크
       (CLAUDE.md 전역 규칙 10번 ScriptableObject assets-modify 함정과 같은 분류) —
       본 phase 에 3 필드 일괄 도입.

=============================================================================
