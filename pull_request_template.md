## 🔍 변경 유형

- [ ] 🆕 기능 추가 (Feature)
- [ ] 🐛 버그 수정 (Fix)
- [ ] ♻️ 리팩토링 (Refactor)
- [ ] 📐 주석 / Dev Log (Comment)
- [ ] 📝 문서 변경 (Docs)
- [ ] ⭐ 스타일 (Style)
- [ ] 🧪 테스트 (Test)
- [ ] 💀 코드 제거 (Remove)
- [ ] 🚚 파일 이동 (Move)

## 📦 영향 패키지 / 어셈블리

해당하는 패키지에 체크. asmdef 수준의 변경이 있으면 `[asmdef]`도 체크.

**Core Packages**
- [ ] HUtil (Animation, AssetHandler, Data, Font, Pooling)
- [ ] HUI (UI 컴포넌트)
- [ ] HGame (게임 로직)

**Foundation Assemblies**
- [ ] HData (Primitives, Encode, Encrypt, Mathx)
- [ ] HDiagnosis (Logger, HDebug)
- [ ] HInspector (Inspector 속성 + Editor Drawer)
- [ ] HCollection (자료구조)
- [ ] HCore (Scene, Singleton, Time, Web)

**기타**
- [ ] `[asmdef]` 어셈블리 참조 변경
- [ ] `[namespace]` 네임스페이스 변경
- [ ] Editor 전용 변경
- [ ] Samples~ 변경

## ✅ 변경 사유

<!-- 이 PR이 왜 필요한지. 마일스톤/이슈 번호가 있으면 연결. -->

## 📋 주요 변경 내역

<!-- 핵심 변경만 요약. 커밋 메시지 복붙 X — diff로 안 보이는 "왜"를 설명. -->

-
-
-

## ⚠️ Breaking Changes

<!-- namespace 변경, API 시그니처 변경, 의존 방향 변경 등. 없으면 "없음". -->

없음

## 🧪 테스트

### 자동 검증
- [ ] Unity 에디터 컴파일 에러 0개
- [ ] Library 삭제 후 full recompile 통과

### 수동 검증
- [ ] 관련 씬 Play Mode 정상 동작
<!-- 검증한 씬 경로 기재. 예: Assets/05_Study/.../1003_MVP.unity -->

### 미검증 (리뷰어 확인 요청)
<!-- 검증하지 못한 항목. 빈칸이면 삭제. -->

## 🔗 관련 링크

<!-- 관련 이슈, 위키 페이지, 마일스톤 문서 등. -->

-

## ☑️ 셀프 체크리스트

- [ ] 코드 정상 작동 확인
- [ ] 불필요한 주석/로그 제거
- [ ] HCUP 의존 방향 준수 (HData → HDiagnosis → HCollection → HUtil → HCore → HUI → HGame)
- [ ] `.meta` 파일 누락 없음 (파일 추가/이동 시)
- [ ] CHANGELOG.md 업데이트 (릴리즈 예정 시)
