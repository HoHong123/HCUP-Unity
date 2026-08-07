# HCUP 모듈 현황 (2026-08-04, Phase 1 정리 기준)

Lisa Tail Cafe (LTC) 프로젝트의 Phase 1 "HCUP 패키지 최신화/정리" 작업 시점의 모듈 현황 기록입니다.

## 모듈 목록 (13개)

| 모듈 | asmdef | 상태 / 비고 |
|---|---|---|
| HCore | HCUP.HCore | SingletonBehaviour 등 기반 계층. 정상 |
| HUtil | HCUP.HUtil (+Editor/Odin/Tween) | 풀링·로더·유틸. 정상 |
| HCollection | HCUP.HCollection (+Editor/Odin.Editor) | HDictionary 등. 정상 |
| HInspector | HCUP.HInspector (+Editor/Odin.Editor) | HTitle 등 인스펙터 확장. 정상 |
| HDiagnosis | HCUP.HDiagnosis | HLogger/HDebug. 정상 |
| HAudio | HCUP.HAudio (+Editor/Odin) | **2026-08-04 재구성 완료** — 아래 참조 |
| HGame | HCUP.HGame | **GameModule → InitModule 개칭 완료** — 아래 참조 |
| HUI | HCUP.HUI (+Editor) | UI 계층. 정상 |
| HData | HCUP.HData (+NPOI/NPOI.Tests) | 엑셀/데이터 로딩. 정상 |
| HDialogue | HCUP.HDialogue (+Editor) | dev/feat-dialog-node-2.0.0 작업 중 계층 |
| HDeploy | HCUP.HDeploy.Editor | 배포 에디터 도구 |
| HWindows | HCUP.HWindows.NodeWindow (+Editor) | 노드 그래프 윈도우 |
| HLocalization (HcupLocalization + HUnityLocalization) | 각 asmdef 유지 | 로컬라이제이션 이중 구현 (자체 / Unity 패키지 기반). 2026-08-04 우산 폴더로 통합 |

## 2026-08-04 재구성 내역

### HAudio

- `Runtime/Legacy/` 해체. 진짜 구형 클래스 `SoundManager` 삭제 (외부 참조 0건 확인 후).
- `SfxAgent` 의 `useNewManager` 이중 매니저 분기 제거 — AudioManager 단일 라우팅.
- 신규 클래스의 legacy 호환 partial 4종은 클래스명 기준 `*.Uid.cs` 로 개칭해 본체 옆으로 이동:
  - `AudioManager.Uid.cs`, `Repository/AudioClipRepository.Uid.cs`, `Repository/IAudioClipRepository.Uid.cs`, `Catalog/AudioCatalogRegistry.Uid.cs`
- `Runtime/New/` 디렉토리를 `Runtime/` 직하로 플래튼.
- 파일명 정정: `AudioManager.Preivew.cs` → `AudioManager.Preview.cs`, `SoundClipDebugWindow.cs` → `SoundClipDiagnosticsWindow.cs`.
- 샘플: `[BM] SoundManager.Legacy.prefab` 삭제, `[BM] SoundManager.prefab` 의 dangling guid 를 AudioManager 로 복구.

### HGame

- `GameModule` → `InitModule` 개칭 (폴더 + 클래스 + Samples~ + package.json path).
- dead 인터페이스 `IGameInitRequire` 삭제.

## 알려진 이슈 / 후속 과제

1. ~~**HCUP.HAudio.Odin 빈 어셈블리**~~ — 2026-08-06 제거 완료 (`HCUP.Util.Odin`, `HCUP.Util.Tween` 동시 제거).
2. ~~**ODIN_INSPECTOR versionDefines 미설정**~~ — **오기술 정정(2026-08-04 전수 감사)**: Odin 은 `Assets/Plugins/Sirenix` 에 설치돼 있고 `ODIN_INSPECTOR` 는 PlayerSettings 전역 define 으로 활성 상태다. 가드 코드는 정상 컴파일된다. 남는 과제는 asmdef 에 Odin 참조가 없다는 점뿐.
3. **uid API (`*.Uid.cs` partial) 는 제거 후보** — 신규 코드는 string token 경로만 사용할 것. LTC 프로젝트에서 uid 경로 사용이 없음이 확정되면 제거.
4. **codex 워크트리** — `C:/Users/epzmf/.codex/worktrees/eb4a/HCUP-Unity` 가 detached 4c46c67 을 참조 중 (현 브랜치의 ancestor, 미반영 작업 없음). 사용하지 않으면 `git worktree remove` 로 정리 가능. 이 워크트리가 남아 있는 동안 `git submodule absorbgitdirs` 실행 금지 (gitdir 포인터 파손 위험).
5. **HGame 네임스페이스 갈림** — `2D/Map` 하위에 `HGame.H2D.Map` / `HGame.Map` / 글로벌 네임스페이스가 혼재 (`MapManager`, `MinimapTracker`, `IWorldBoundSource`, `MapBoundType`, `World/EventPoint/BaseEventPoint`). 후속 정리 후보.
6. **스펠링 정리 후보** — `ParallexLayer`(Parallax), `CameraBoundry*` 5종(Boundary).
