# HDeploy - 운영 매뉴얼

> 모듈: `HDeploy/` · 소스 11파일 · `package.json` 없음 (저장소 통째 사용)
> 어셈블리: `HCUP.HDeploy.Editor` (Editor 전용). 참조 `HCUP.HInspector.Editor`
> 네임스페이스: Git `HDeploy.Git` / 배포 `HDeploy.Deploy` / Vercel `HDeploy.Vercel` (asmdef rootNamespace 는 `HDeploy`)
> 메뉴: **`HCUP/Deployment/Vercel Deployment`** (`VercelDeployWindow.cs:31`)
> 코드 문서: [Editor README](Editor/README.md). 클래스 구조와 실행 시퀀스는 그쪽

Unity 에디터 안에서 WebGL 빌드 → 배포 레포 push → Vercel 자동 배포까지 수행한다.
이 문서는 **쓰는 사람을 위한 절차서**다. 코드가 어떻게 짜여 있는지는 위 Editor README 를 본다.

---

## 요구 조건

아래를 모두 갖춘 프로젝트에서 동작한다.

1. WebGL 빌드가 가능한 Unity 프로젝트 (Active Build Target: WebGL)
2. 배포 전용 git 레포 (레포 루트 = `index.html` + `Build/` 구조)
3. Vercel 프로젝트가 배포 레포에 Git 연동되어 있음 (브랜치 push 시 자동 배포)
4. 로컬 머신에 git 설치 + 배포 레포 원격 자격 증명 구성. **push 인증이 프롬프트 없이 통과해야 한다**
5. `Name Files As Hashes` 활성 권장 (파일명이 콘텐츠 해시가 되어 `Build/` 장기 캐시가 안전해진다)

---

## 설정

창의 설정 항목은 두 파일로 나뉘어 저장된다.

### 머신 종속 - `UserSettings/VercelDeployUserSettings.asset` (미커밋)

| 항목 | 설명 | 기본값 |
|---|---|---|
| 배포 레포 경로 | 배포 전용 레포의 절대경로 | (비어 있음) |
| `LocalGitTimeoutSeconds` | 로컬 git 명령 타임아웃 | `15` |
| `RemoteGitTimeoutSeconds` | fetch/pull/push 타임아웃 | `120` |

### 프로젝트 공유 - `ProjectSettings/VercelDeployProjectSettings.asset` (커밋 대상)

| 항목 | 설명 | 기본값 |
|---|---|---|
| Dev 브랜치 | 자동 배포 대상 브랜치 | `dev` |
| Release 브랜치 | 승격 대상 브랜치 | `main` |
| Dev 커밋 템플릿 | Dev 배포 커밋 메시지 | `[Build] 🛠️ : WebGL 빌드 배포 v{version} ({timestamp})` |
| 승격 커밋 템플릿 | Release merge 커밋 메시지 | `[Build] 🛠️ : Release 배포 v{version}` + em dash(U+2014) 한 글자 + ` {devBranch} → {releaseBranch} ({timestamp})` (`VercelDeployProjectSettings.cs:31`) |
| 빌드 출력 경로 | 프로젝트 루트 기준 임시 빌드 폴더. 배포 시 내용 전체가 삭제 후 재생성된다 | `Builds/VercelDeploy` |
| Dev/Release 서버 URL | 배포 성공 후 "열기" 버튼용 (선택) | (비어 있음) |

### 커밋 메시지 placeholder

| Placeholder | 치환 값 |
|---|---|
| `{version}` | `PlayerSettings.bundleVersion` |
| `{timestamp}` | 실행 시각 `yyyy-MM-dd HH:mm` |
| `{devBranch}` / `{releaseBranch}` | 설정된 브랜치명 |

---

## 사용법

### Dev Deploy

빌드부터 push까지 한 번에 수행한다. 실행 순서:

1. **Preflight** - 레포 검증 → 워킹트리 클린 확인 → fetch → dev 브랜치 checkout → `pull --ff-only`
2. **WebGL 빌드** - 출력 폴더 전체 삭제 후 빌드. 산출물 검증: `index.html` + `.wasm`/`.data`/`.framework.js`/`.loader.js` 각 1개
3. **교체** - 배포 레포의 `index.html` + `Build/` 만 교체 (그 외 파일 불변)
4. **push** - 커밋 후 `push origin {dev}` → Vercel 이 자동 배포

빌드 중에는 에디터가 응답 없음 상태가 된다. 변경이 없으면(해시 동일) 커밋 없이 정상 종료된다.

### Release Deploy

재빌드 없이 `origin/{dev}` 를 `{release}` 브랜치에 `merge --no-ff` 후 push 한다.
Dev 서버에서 확인한 빌드와 Release 빌드가 **바이너리 단위로 동일함이 보장된다.**
승격할 커밋이 없으면 아무것도 하지 않는다.

### 버전

창의 Version 필드는 `PlayerSettings.bundleVersion` 을 표시한다. SemVer(`major.minor.patch`)
형식 검증 후 적용되며, ProjectSettings 변경분의 커밋은 사용자가 수행한다.

---

## 안전 장치 - 무엇이 삭제를 막는가

이 패키지에는 `Directory.Delete(recursive)` 가 두 곳 있다. 둘 다 사전 검증을 통과해야만 실행된다.

**빌드 출력 폴더** (`WebGLBuildService._ValidateOutputPath:69-109`) - 네 단계를 순서대로 통과해야 한다.

1. 빈 문자열 거부
2. `Path.GetFullPath` 정규화 후 **프로젝트 루트 하위인지** 검사 (`OrdinalIgnoreCase` 접두 비교)
3. 루트 기준 경로 세그먼트가 **2개 이상**인지 (`MIN_OUTPUT_PATH_DEPTH`) - 최상위 폴더 통째 삭제 차단
4. 첫 세그먼트가 **보호 폴더**(`Assets`, `Library`, `Packages`, `ProjectSettings`, `UserSettings`, `Temp`, `Logs`, `.git`)가 아닌지

**배포 레포** (`DeployRepoGitService.ReplaceArtifacts:128-147`) - 삭제 전에 해당 경로가 `.git` 폴더와 `index.html` 을 모두 가진 배포 레포인지 확인한다. 삭제 범위도 `Build/` + `index.html` 로 고정이다.

---

## 문제 해결

| 중단 메시지 | 원인 | 조치 |
|---|---|---|
| `Deploy repo is dirty` | 배포 레포에 커밋되지 않은 변경 존재 | 배포 레포에서 직접 커밋 또는 폐기한다. **자동 stash 는 하지 않는다** |
| `Deploy repo path not found` / `Not a git work tree` | 배포 레포 경로가 없거나 git 작업 트리가 아님 | 배포 레포 경로 설정을 확인한다 |
| `git fetch failed` | 네트워크 또는 자격 증명 문제 | 터미널에서 해당 레포의 `git fetch` 가 통과하는지 확인한다 |
| `git pull --ff-only ... failed` | 로컬/원격 브랜치 분기 | 배포 레포에서 분기를 직접 해소한다 |
| `git push ... rejected` | non-fast-forward 등 push 거부 | 커밋은 로컬에 보존된다. 원인 해소 후 수동 push 또는 재배포. **force push 는 하지 않는다** |
| `Merge failed` | 승격 merge 충돌 | 자동으로 `merge --abort` 된다. 배포 레포에서 충돌을 직접 해소한다 |
| `Sanity check failed` | 배포 레포 경로가 `.git` + `index.html` 구조가 아님 | 배포 레포 경로 설정을 확인한다 |
| `Build output path is empty / too shallow / escapes the project root / targets a protected folder` | 빌드 출력 경로가 위 안전 장치에 걸림 | `Builds/VercelDeploy` 처럼 두 단계 이상의 프로젝트 하위 경로로 설정한다 |
| git 타임아웃 | 자격 증명 프롬프트 대기 등 | `GIT_TERMINAL_PROMPT=0` 으로 실행되므로 프롬프트가 뜨지 않는 자격 증명(credential manager)을 구성한다 |

---

## 구성 파일

```
HDeploy/
├── README.md                            ← 이 문서 (운영 절차)
└── Editor/
    ├── README.md                        ← 어셈블리 문서 (코드 구조·시퀀스)
    ├── HCUP.HDeploy.Editor.asmdef
    ├── Git/                             (namespace HDeploy.Git)
    │   ├── GitCommandRunner.cs          ← git 프로세스 실행
    │   ├── GitResult.cs                 ← 실행 결과 구조체
    │   └── DeployRepoGitService.cs      ← git 시나리오 (preflight/교체/커밋/승격)
    ├── Deploy/                          (namespace HDeploy.Deploy)
    │   ├── DeployLog.cs / DeployLogEntry.cs / DeployLogSeverity.cs   ← 로그 버퍼
    │   └── WebGLBuildService.cs         ← WebGL 빌드·산출물 검증·출력 경로 가드
    └── Vercel/                          (namespace HDeploy.Vercel)
        ├── VercelDeployWindow.cs        ← EditorWindow (UI 전용)
        ├── VercelDeployService.cs       ← Dev/Release 시퀀스 오케스트레이션
        ├── VercelDeployProjectSettings.cs  ← 공유 설정
        └── VercelDeployUserSettings.cs  ← 머신 설정
```

---

## 주의할 점

1. **연결 테스트는 배포 흐름보다 검증이 느슨하다.** `VercelDeployWindow._RunConnectionTestAsync` 는 `DeployRepoGitService.ValidateRepoAsync`(경로 존재 + `rev-parse --is-inside-work-tree`)만 거친 뒤 `GitCommandRunner` 를 직접 호출한다. 배포 시의 sanity check(`.git` 폴더 + `index.html`)는 하지 않으므로, 다른 레포의 하위 폴더를 지정해도 통과하고 그 레포의 상태를 보고한다.
2. 이 문서의 기본값 표는 설정 클래스의 필드 초기값(`VercelDeployProjectSettings.cs:26-38`, `VercelDeployUserSettings.cs:24-27`)을 옮긴 것이다. 필드 기본값이 바뀌면 이 표도 함께 고친다.

---

## 히스토리

### 2026-08-07 :: 연결 테스트에 레포 검증 추가

- 이전: `_RunConnectionTestAsync` 는 경로 공백 검사만 하고 `GitCommandRunner` 를 바로 호출해, 존재하지 않는 경로나 git 작업 트리가 아닌 폴더에서도 git 명령이 실행됐다.
- 현재: git 명령 전에 `DeployRepoGitService.ValidateRepoAsync` 를 거친다 (`VercelDeployWindow.cs:252-257`).

### 2026-08-06 :: 문서 값 정정

- 이전: 이 문서는 "Editor 전용, 외부 참조 0", 메뉴 `Tools/HDeploy/Vercel Deploy`, 빌드 출력 기본값 `Builds/WebGL_Deploy`, 커밋 템플릿 `chore: WebGL 빌드 배포 v{version} ({timestamp})` 로 적고 있었다.
- 현재: 코드 기준 값(`HCUP.HInspector.Editor` 참조, `HCUP/Deployment/Vercel Deployment`, `Builds/VercelDeploy`, `[Build] 🛠️ : ...` 템플릿)으로 고쳤다.

### 2026-07-04 :: `HCUP.HInspector.Editor` 참조 추가

- 이전: 외부 어셈블리 참조가 없는 단독 Editor 어셈블리였다.
- 현재: 창의 섹션 헤더를 `HInspector.Editor.HTitleDrawer.Draw` 로 그리기 위해 `HCUP.HInspector.Editor` 를 참조한다.

### 2026-07-03 :: 메뉴 경로 이동

- 이전: `Tools/HDeploy/Vercel Deploy`.
- 현재: `HCUP/Deployment/Vercel Deployment` (HCUP 메뉴 트리로 통합).

### 2026-07-03 :: 네임스페이스를 디렉토리 기반으로 분리

- 이전: 로그·빌드·git 타입이 `Editor/` 루트의 `HDeploy` 네임스페이스에 있었다. 이 문서 헤더도 2026-09-21 까지 "공용 코어 `HDeploy`" 를 적고 있었다.
- 현재: `Editor/Deploy/` (`HDeploy.Deploy`), `Editor/Git/` (`HDeploy.Git`) 로 옮겼다. 네임스페이스 `HDeploy` 에 직접 선언된 타입은 없다.
