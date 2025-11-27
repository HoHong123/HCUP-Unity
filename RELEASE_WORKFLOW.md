# UPM Release Workflow (HUtil, HGame, HUI)

목표: `dev/update-latest-251125`에서 패키지별 UPM 릴리즈를 빠르고 일관되게 만들기 위한 절차입니다. 패키지별 브랜치를 따로 만들 필요는 없으며, **한 개의 안정 커밋에 패키지별 태그를 각각 생성**하는 방식을 권장합니다.

## 브랜치 전략
- 기본 베이스: `dev/update-latest-251125` (또는 최신 통합 브랜치).
- 안정 검증 후 필요 시 **release/0.2.0** 같은 릴리즈 브랜치를 만들어 최종 검증을 수행합니다. 이 브랜치는 선택 사항이며, 태그는 검증이 끝난 동일 커밋에서 생성합니다.
- 패키지별 브랜치를 따로 생성할 필요는 없습니다. 하나의 커밋 스냅샷에 **패키지 전용 태그**를 여러 개 찍어 관리합니다.

## 태그 네이밍
- 예시: `hutil/v0.2.0`, `hgame/v0.2.0`, `hui/v0.2.0`
- 공통 릴리즈를 한 번에 배포할 때는 동일 커밋에 세 개의 태그를 생성합니다.
- 패키지별 긴급 수정이 필요하면 해당 패키지의 태그만 패치 버전으로 추가합니다(예: `hutil/v0.2.1`).

## 릴리즈 절차 (예: 0.2.0)
1) 최신 반영
```bash
git checkout dev/update-latest-251125
git pull
```

2) (선택) 릴리즈 브랜치 생성 후 최종 검증
```bash
git checkout -b release/0.2.0
# 테스트/검증 후 commit/tag용 최종 커밋 준비
```

3) 버전/체인지로그 확인
- `Packages/com.hohong123.hutil/package.json` 등에서 `version`이 목표 버전인지 확인합니다.
- 각 패키지 `CHANGELOG.md`의 최신 엔트리가 준비되었는지 확인합니다.

4) 동일 커밋에 패키지별 태그 생성
```bash
git tag hutil/v0.2.0
git tag hgame/v0.2.0
git tag hui/v0.2.0
```
필요 시 `-a`와 메시지를 추가합니다. 태그 위치를 특정 커밋으로 지정하려면 커밋 해시를 태그 명령 끝에 지정합니다.

5) 태그 푸시
```bash
git push origin hutil/v0.2.0 hgame/v0.2.0 hui/v0.2.0
```

6) GitHub Release 작성
- 각 태그에 대해 **별도 릴리즈 노트**를 올립니다.
- UPM 의존성 URL 예시(README나 배포 페이지에 활용):
  - HUtil: `https://github.com/HoHong123/Custom-Unity-Utility-Package.git?path=/Packages/com.hohong123.hutil#v0.2.0`
  - HGame: `https://github.com/HoHong123/Custom-Unity-Utility-Package.git?path=/Packages/com.hohong123.hgame#v0.2.0`
  - HUI: `https://github.com/HoHong123/Custom-Unity-Utility-Package.git?path=/Packages/com.hohong123.hui#v0.2.0`

## 릴리즈 노트 템플릿 (패키지 공통)
- 제목: `<패키지명> v<버전> Release Notes`
- 개요: 지원 Unity 버전, 핵심 목적(예: 중복 제거, 안정화).
- 주요 변경: 기능 추가/삭제, 파일 정리, 의존성 변경.
- 호환성/주의: 마이그레이션 단계, API 변경 여부, 의존성 설치 순서.
- 검증 항목: 풀/사운드/UI 등 핵심 기능 스모크 테스트 체크리스트.

## 패키지별 릴리즈 노트 위치
- HUtil: `Packages/com.hohong123.hutil/CHANGELOG.md`
- HGame: `Packages/com.hohong123.hgame/CHANGELOG.md`
- HUI: `Packages/com.hohong123.hui/CHANGELOG.md`

해당 파일을 릴리즈마다 갱신한 뒤, 태그와 함께 GitHub Release 본문에 복사해 사용하십시오.
