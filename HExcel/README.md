# HExcel — 패키지 카드

> 모듈: `HExcel/` · 소스 11파일 · `package.json` 없음 (저장소 통째 사용)
> 구성 어셈블리 2개 — **전부 에디터 전용**
> 코드 문서: **[Editor README](Editor/README.md)** · [Tests README](Editor/Tests/README.md)

---

## 이 패키지가 담는 것

NPOI 로 엑셀(`.xlsx`)을 읽어 Unity 에셋으로 임포트하는 에디터 도구.

| 폴더 | 파일 | 담는 것 |
|---|---|---|
| `Core` | 5 (+2 Editor) | 워크북 로더, 시트 파서, 에셋 기록 래퍼, 에디터 창 |
| `Localization` | 3 | 로컬라이제이션 시트 → `HcupLocalization` 테이블 변환. Core 의 유일한 구현 예제 |
| `Tests` | 1 | EditMode 테스트 |

메뉴: `HCUP/Windows/Data Editor Window` (`DataEditorWindow.cs:43`).

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 참조 |
|---|---|---|---|
| `HCUP.HExcel` | Editor | 11 | GUID 4건 (NPOI 계열) + `HCUP.HcupLocalization`, `HCUP.HInspector.Editor` |
| `HCUP.HExcel.Tests` | Editor (`UNITY_INCLUDE_TESTS`) | 1 | GUID 3건 + `overrideReferences: true` |

`HCUP.HExcel.Tests` 는 `overrideReferences` 가 켜져 있어 precompiled DLL 을 명시 지정한다:
`nunit.framework`, `NPOI.Core`, `NPOI.OOXML`, `NPOI.OpenXml4Net`, `NPOI.OpenXmlFormats`,
`Newtonsoft.Json`. 이 목록에서 빠진 DLL 은 테스트 어셈블리에서 보이지 않는다.

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다 — 이 모듈에는 `package.json` 이 없어 개별 UPM 설치 대상이 아니다
([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 이 프로젝트 기준 6000.3.18f1 |
| NPOI (4개 DLL) | 엑셀 파싱. asmdef 가 GUID 로 참조한다 |
| Newtonsoft.Json | 테스트 어셈블리 |

에디터 전용이므로 플레이어 빌드에는 포함되지 않는다.

---

## 주의할 점

1. **이 모듈의 테스트는 오랫동안 한 번도 실행되지 않았다.** `excludePlatforms` 에 `Editor` 가
   들어 있어 EditMode 러너에 잡히지 않았고, 그 사이 참조 타입이 삭제돼 컴파일도 불가한 상태로
   방치돼 있었다. 2026-08-05 에 플랫폼 설정과 `precompiledReferences` 를 바로잡아 복구했다.
2. **`AssetDatabaseInstance.CreateAsset`/`CreateAssetAt` 이 `target` 이 아니라 static
   `instance` 에 기록한다.** 인자로 넘긴 대상이 무시된다.
3. **구 `Editor/README.md` 는 대체됐다.** 링크한 6개 문서(`00_OVERVIEW.md`~`05_TEST_CASES.md`)가
   전부 부재였고 코드 경로도 구 프로젝트(`Assets/01_Scripts/02_Data/NPOI/`)를 가리켰다.
   원본은 `Editor/_to_delete/README.old.md` 에 남겨 뒀다.
4. **헤더 주석의 메뉴 경로가 실제와 다르다** — `DataEditorWindow.cs:6` 은 "HData/NPOI 에서
   오픈"이라 적었지만 실제 `[MenuItem]` 은 `HCUP/Windows/Data Editor Window` 다.

근거 라인은 [Editor README](Editor/README.md) 의 "정리 대상" 절에 있다.
