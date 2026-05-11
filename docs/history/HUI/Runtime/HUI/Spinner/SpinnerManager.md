---
script_path: Assets/01_Scripts/HCUP-Unity/HUI/Runtime/HUI/Spinner/SpinnerManager.cs
script_name: SpinnerManager
latest_log_id: LOG-20260310-1
total_entries: 5
created: 2026-05-04
updated: 2026-05-04
---

# SpinnerManager Dev Log History

`Assets/01_Scripts/HCUP-Unity/HUI/Runtime/HUI/Spinner/SpinnerManager.cs` 의 Dev Log 엔트리 풀 본문이 본 파일에 시간 역순으로 보관됩니다 (최신이 위). .cs 파일 안에는 최신 3 엔트리의 1-줄 요약 view 가 유지되며, **본 history MD 가 ground truth** — 모든 엔트리의 풀 본문은 손실 없이 본 파일에 보관됩니다.

본 파일은 unity-devlog-history-mirror 스킬에 의해 사용자 명시 요청으로 생성되었습니다 (2026-05-04). 추출된 엔트리들은 원본 HCUP 포맷 그대로 보존되어 있습니다.

==================================
@Jason - PKH 2026.03.10 [LOG-20260310-1]
설명 주석 추가
주요 기능 ::

1. Spinner 표시
2. 일정 시간 Spinner 표시
3. 비동기 작업 Spinner 처리
4. Spinner 숨김
5. Scene 변경 정리

구조 ::
callers
 + Spinner 호출 객체 추적 Dictionary
IsVisible
 + 현재 Spinner 표시 상태

사용법 ::
SpinnerManager.Instance.Show(this);
await SpinnerManager.Instance.Show(
    this,
    async () => await LoadData()
);
==================================
@Jason - PKH 09. 02. 26 [LOG-20260209-2]
1. IDisposable 파기
==================================
@Jason - PKH 09. 02. 26 [LOG-20260209-1]
1. 스피너 호출 오브젝트들은 반드시 IDisposable이 가능한 오브젝트로 선언.
+ 예기치 못한 호출자 파괴와 같은 이벤트 대비 안전장치 추가.
==================================
@Jason - PKH 22. 07. 25 [LOG-20250722-1]
1. 씬전환 및 콜러의 값이 의도치않게 제거되었을 경우, 스피너에서 이를 확인하여 해당 호출자 정보를 관리하는 기능 추가
1-1. CleanUp함수
2. CleanUp이 씬로드/씬언로드 프로세스가 진행되면 자동으로 활성화되도록 설정
==================================
@Jason - PKH 21. 07. 25 [LOG-20250721-1]
1. 스피너를 전역으로 사용하기 위해 작성한 스크립트 입니다.
1-1. 불필요한 싱글톤 접근을 제외하기 위해 작성했습니다.
2. 스피너는 자신을 호출한 모든 오브젝트를 추적합니다.
2-1. 스피너를 호출한 오브젝트가 비활성화(Hide)를 반드시 시켜주어야 합니다.
3. 비동기 처리도 진행합니다.
4. **팝업 매니저**가 반드시 필요합니다.
Ps. 사용법은 'SpinnerTester.cs'를 확인해주세요.
===============================================
1. This is a script written to use the spinner globally.
1-1. It was written to exclude unnecessary singleton access.
2. The spinner tracks all objects that called it.
2-1. The object that called the spinner must deactivate(Hide) it.
3. It also performs asynchronous processing.
4. A popup manager is absolutely necessary.
Ps. Check 'SpinnerTester.cs' for tutorial.
==================================
