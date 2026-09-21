# HExcel - 패키지 카드

> 모듈: `HExcel/` · 소스 10파일 · `package.json` 없음 (저장소 통째 사용)
> 구성 어셈블리 1개. **에디터 전용**
> 코드 문서: **[Editor README](Editor/README.md)**

---

## 이 패키지가 담는 것

NPOI 로 엑셀(`.xlsx`)을 읽어 Unity 에셋으로 임포트하는 에디터 도구.

| 폴더 | 파일 | 담는 것 |
|---|---|---|
| `Core` | 5 (+2 Editor) | 워크북 로더, 시트 파서, 에셋 기록 래퍼, 에디터 창 |
| `Localization` | 3 | 로컬라이제이션 시트 → `HcupLocalization` 테이블 변환. 이 모듈 안의 Core 구현 예제 |

메뉴: `HCUP/Windows/Data Editor Window` (`DataEditorWindow.cs:43`).

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 참조 |
|---|---|---|---|
| `HCUP.HExcel.Editor` (파일명 `HCUP.HExcel.asmdef`) | Editor | 10 | GUID 4건 (`HCUP.HDiagnosis`, `HCUP.HCollection`, `HCUP.HData`, `HCUP.HInspector`) + `HCUP.HcupLocalization`, `HCUP.HInspector.Editor` |

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다. 이 모듈에는 `package.json` 이 없어 개별 UPM 설치 대상이 아니다 ([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 6000.3 에서 개발·검증 |
| NPOI (4개 DLL) | 엑셀 파싱. HCUP 에 포함되어 있지 않으므로 프로젝트가 DLL 을 넣어야 한다. `HCUP.HExcel.Editor` 는 `overrideReferences: false` 라 자동 참조로 해결한다 |
| Newtonsoft.Json | `ExcelLoader` 의 `JArray` 변환 |

에디터 전용이므로 플레이어 빌드에는 포함되지 않는다.

---

## 주의할 점

1. **헤더 주석의 메뉴 경로가 실제와 다르다.** `DataEditorWindow.cs:6` 은 "HData/NPOI 에서 오픈"이라 적었지만 실제 `[MenuItem]` 은 `HCUP/Windows/Data Editor Window` 다.
2. **자동 테스트가 없다.** 2026-09-22 에 테스트 어셈블리를 제거했다. 엑셀 임포트 동작은 에디터 창에서 직접 확인한다.

코드 결함 목록과 근거 라인은 [Editor README](Editor/README.md) 의 "정리 대상" · "코드 주석과의 불일치" 절에 있다.

---

## 히스토리

### 2026-09-22 :: 테스트 어셈블리 제거

- 이전: `Editor/Tests` 에 EditMode 테스트 어셈블리 `HCUP.HExcel.Tests` (`ExcelLoaderTests.cs` 1파일 + 전용 README)가 있었다.
- 현재: 사용자 지시로 HCUP 테스트 코드를 모두 제거하면서 함께 지웠다. 구성 어셈블리는 `HCUP.HExcel.Editor` 하나다.

### 2026-08-07 :: `AssetDatabaseInstance.CreateAsset`/`CreateAssetAt` 기록 대상 수정

- 이전: 두 메서드가 `target` 이 아니라 static `instance` 에 기록해, 인자로 넘긴 대상이 무시됐다.
- 현재: 두 메서드 모두 `this` 에 기록한다.

### 2026-08-06 :: 구 `Editor/README.md` 대체

- 이전: 구 문서는 존재하지 않는 6개 문서(`00_OVERVIEW.md`~`05_TEST_CASES.md`)를 링크했고 코드 경로도 모듈 분리 전 위치를 가리켰다.
- 현재: 코드 기준으로 새로 작성한 [Editor README](Editor/README.md) 로 대체했다. 원본 사본은 저장소에 남아 있지 않다.

### 2026-08-05 :: 테스트 어셈블리 복구

- 이전: 모듈 분리 전 `HCUP.HData.NPOI.Tests` 는 `excludePlatforms` 에 `Editor` 가 들어 있어 EditMode 러너에 잡히지 않았고, 그 사이 참조 타입(`HData.NPOI.Samples.SampleData`)이 삭제돼 컴파일도 불가한 상태였다.
- 현재: HExcel 로 분리하면서 `includePlatforms: ["Editor"]` 로 바꾸고, `precompiledReferences` 에 NPOI 4종과 Newtonsoft.Json 을 추가하고, 픽스처 전용 DTO 를 테스트 파일 안에 두었다.
