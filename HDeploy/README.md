# HDeploy

Unity 에디터 안에서 WebGL 빌드, 배포 레포 push, Vercel 자동 배포까지 수행하는 에디터 전용 패키지입니다.

- 어셈블리: `HCUP.HDeploy.Editor` (Editor 전용, 외부 참조 0)
- 네임스페이스: 공용 코어 `HDeploy` / Vercel 특화 `HDeploy.Vercel`
- 메뉴: `Tools/HDeploy/Vercel Deploy`

## 요구 조건

아래 조건을 모두 갖춘 프로젝트에서 동작합니다.

1. WebGL 빌드가 가능한 Unity 프로젝트 (Active Build Target: WebGL)
2. 배포 전용 git 레포 (레포 루트 = `index.html` + `Build/` 구조)
3. Vercel 프로젝트가 배포 레포에 Git 연동되어 있음 (브랜치 push 시 자동 배포)
4. 로컬 머신에 git 설치 + 배포 레포 원격에 대한 자격 증명 구성 (push 인증이 프롬프트 없이 통과해야 합니다)
5. `Name Files As Hashes` 활성 권장 (파일명이 콘텐츠 해시가 되어 `Build/` 장기 캐시가 안전해집니다)

## 설치

`HDeploy/` 폴더를 프로젝트의 `Assets/` 하위로 복사합니다. 별도 패키지 의존성은 없습니다.

## 설정

창의 설정 항목은 두 파일로 분리 저장됩니다.

### 머신 종속 설정 — `UserSettings/VercelDeployUserSettings.asset` (미커밋)

| 항목 | 설명 | 기본값 |
|---|---|---|
| 배포 레포 경로 | 배포 전용 레포의 절대경로 | (비어 있음) |
| LocalGitTimeoutSeconds | 로컬 git 명령 타임아웃 | 15 |
| RemoteGitTimeoutSeconds | fetch/pull/push 타임아웃 | 120 |

### 프로젝트 공유 설정 — `ProjectSettings/VercelDeployProjectSettings.asset` (커밋 대상)

| 항목 | 설명 | 기본값 |
|---|---|---|
| Dev 브랜치 | 자동 배포 대상 브랜치 | `dev` |
| Release 브랜치 | 승격 대상 브랜치 | `main` |
| Dev 커밋 템플릿 | Dev 배포 커밋 메시지 | `chore: WebGL 빌드 배포 v{version} ({timestamp})` |
| 승격 커밋 템플릿 | Release merge 커밋 메시지 | `chore: Release 배포 v{version} — {devBranch} → {releaseBranch} ({timestamp})` |
| 빌드 출력 경로 | 프로젝트 루트 기준 임시 빌드 폴더. 배포 시 내용 전체가 삭제 후 재생성됩니다 | `Builds/WebGL_Deploy` |
| Dev/Release 서버 URL | 배포 성공 후 "열기" 버튼용 (선택) | (비어 있음) |

### 커밋 메시지 템플릿 placeholder

| Placeholder | 치환 값 |
|---|---|
| `{version}` | `PlayerSettings.bundleVersion` |
| `{timestamp}` | 실행 시각 `yyyy-MM-dd HH:mm` |
| `{devBranch}` / `{releaseBranch}` | 설정된 브랜치명 |

## 사용법

### Dev Deploy

빌드부터 push까지 한 번에 수행합니다. 실행 순서:

1. Preflight: 레포 검증 → 워킹트리 클린 확인 → fetch → dev 브랜치 checkout → `pull --ff-only`
2. WebGL 빌드 (출력 폴더 전체 삭제 후 빌드, 산출물 검증: `index.html` + `.wasm`/`.data`/`.framework.js`/`.loader.js` 각 1개)
3. 배포 레포의 `index.html` + `Build/`만 교체 (그 외 파일 불변)
4. 커밋 후 `push origin {dev}` → Vercel이 자동 배포

빌드 중에는 에디터가 응답 없음 상태가 됩니다. 변경이 없으면(해시 동일) 커밋 없이 정상 종료됩니다.

### Release Deploy

재빌드 없이 `origin/{dev}`를 `{release}` 브랜치에 `merge --no-ff` 후 push합니다. Dev 서버에서 확인한 빌드와 Release 빌드가 바이너리 단위로 동일함이 보장됩니다. 승격할 커밋이 없으면 아무것도 하지 않습니다.

### 버전

창의 Version 필드는 `PlayerSettings.bundleVersion`을 표시합니다. SemVer(`major.minor.patch`) 형식 검증 후 적용되며, ProjectSettings 변경분의 커밋은 사용자가 수행합니다.

## 문제 해결

| 중단 메시지 | 원인 | 조치 |
|---|---|---|
| `Deploy repo is dirty` | 배포 레포에 커밋되지 않은 변경 존재 | 배포 레포에서 변경을 직접 커밋 또는 폐기합니다. 자동 stash는 수행하지 않습니다 |
| `git fetch failed` | 네트워크 또는 자격 증명 문제 | 터미널에서 해당 레포의 `git fetch`가 통과하는지 확인합니다 |
| `git pull --ff-only failed` | 로컬/원격 브랜치 분기 | 배포 레포에서 분기를 직접 해소합니다 |
| `git push rejected` | non-fast-forward 등 push 거부 | 커밋은 로컬에 보존됩니다. 원인 해소 후 수동 push 또는 재배포합니다. force push는 수행하지 않습니다 |
| `Merge failed — aborting` | 승격 merge 충돌 | 자동으로 `merge --abort` 됩니다. 배포 레포에서 충돌을 직접 해소합니다 |
| `Sanity check failed` | 배포 레포 경로가 배포 레포 구조(`.git` + `index.html`)가 아님 | 배포 레포 경로 설정을 확인합니다 |
| git 타임아웃 | 자격 증명 프롬프트 대기 등 | `GIT_TERMINAL_PROMPT=0`으로 실행되므로 프롬프트가 뜨지 않는 자격 증명(credential manager)을 구성합니다 |

## 구성 파일

```
HDeploy/
├── README.md
└── Editor/
    ├── HCUP.HDeploy.Editor.asmdef
    ├── Git/                             (namespace HDeploy.Git)
    │   ├── GitCommandRunner.cs          ← git 프로세스 실행
    │   ├── GitResult.cs                 ← 실행 결과 구조체
    │   └── DeployRepoGitService.cs      ← git 시나리오 (preflight/교체/커밋/승격)
    ├── Deploy/                          (namespace HDeploy.Deploy)
    │   ├── DeployLog.cs / DeployLogEntry.cs / DeployLogSeverity.cs   ← 로그 버퍼
    │   └── WebGLBuildService.cs         ← WebGL 빌드·산출물 검증
    └── Vercel/                          (namespace HDeploy.Vercel)
        ├── VercelDeployWindow.cs        ← EditorWindow (UI 전용)
        ├── VercelDeployService.cs       ← Dev/Release 시퀀스 오케스트레이션
        ├── VercelDeployProjectSettings.cs  ← 공유 설정
        └── VercelDeployUserSettings.cs  ← 머신 설정
```
