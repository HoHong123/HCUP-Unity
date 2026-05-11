using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("HCUP.HWindows.NodeWindow.Editor")]

#if UNITY_EDITOR
// =============================================================================
// Dev Log
// =============================================================================
// @Jason - PKH 2026-04-22 AssemblyInfo.cs 의 역할 - 어셈블리 단위 attribute 진입점
//
//   [역할] 단방향·선택적 노출.
//   - InternalsVisibleTo 로 Editor asmdef 를 친구 어셈블리(friend assembly)로 선언.
//   - 이 Runtime 어셈블리의 internal 멤버를 Editor 어셈블리에서 접근 가능하게 함.
//   - 그 외 모든 어셈블리(HGame, 외부 패키지)는 여전히 internal 차단
//
//   [필요한 이유]
//   + NodeCatalogAuthor 는 Editor asmdef 에 거주하고, mutation API(BaseNode.AssignIdentity, NodeCatalogSO.InternalAddNode 등)
//      는 Runtime asmdef 에 internal 로 선언되어 있음.
//   + 이 한 줄이 없으면 Author 의 모든 호출이 "inaccessible due to its protection level" 컴파일 에러로 실패.
//   + 즉 Phase 0 의 "Author 외 mutation차단" 계약을 컴파일러 수준에서 강제하는 유일한 합법 메커니즘.
//
//   [대안 비교]
//    + public 으로 변경 → 외부 어셈블리도 mutation 가능, 계약 깨짐
//    + Reflection 사용 → IDE 자동완성·refactor 안전성·성능 모두 손실
//    + Author 를 Runtime 으로 이동 → AssetDatabase / EditorUtility 가 빌드 컴파일 깨뜨림
//    + InternalsVisibleTo (현재 채택) → 정확히 지목된 어셈블리만 노출, 그 외 모든 차단 유지. 계약과 가시성 동시 만족.
//
//   [매칭 규칙] asmdef 이름 변경 시 이 파일도 같이 갱신해야 하는 동기화 포인트.
//   + 문자열 "HCUP.HWindows.NodeWindow.Editor" 는 Editor asmdef 의 "name" 필드와 정확히 일치해야 함.
//   + 한 글자만 달라도 친구 어셈블리로 인식되지 않고 internal 접근이 컴파일 실패.
//
//   [위치 규칙]
//   + Runtime asmdef 의 컴파일 범위 안에 있어야 함.
//   + 디렉토리 깊이는 무관 (Identity/AssemblyInfo.cs 도 동작).
//   + Editor asmdef 디렉토리에 두면 무의미 — 자기 자신을 친구로 가리키는 셈.
//
//   [확장]
//   + 향후 Unity Test Framework 도입 시 테스트 어셈블리도 internal API 를 직접 검증하도록 같은 문법으로 한 줄 추가 가능:
//          [assembly: InternalsVisibleTo("HCUP.HWindows.NodeWindow.Tests")]
// =============================================================================
#endif
