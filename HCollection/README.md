# HCollection — 패키지 카드

> 모듈: `HCollection/` · 소스 8파일 · `package.json` 없음 (저장소 통째 사용)
> 구성 어셈블리 3개
> 코드 문서: **[Runtime README](Runtime/README.md)** · [Editor README](Editor/README.md) · [Odin Editor README](Editor/Odin/README.md)

---

## 이 패키지가 담는 것

Unity 직렬화를 견디는 자료구조 셋.

| 타입 | 무엇 | 문서 |
|---|---|---|
| `HDictionary<TKey, TValue>` | 인스펙터에서 편집 가능한 딕셔너리. 이 모듈 코드의 대부분 | [docs/HDictionary.md](docs/HDictionary.md) |
| `CircularList<T>` | Pivot 을 들고 순환하는 리스트 | Runtime README |
| `EnumArray<TEnum, TValue>` | enum 을 인덱스로 쓰는 배열 | Runtime README |

`HDictionary` 는 어셈블리 3개에 걸쳐 있다 — 런타임 타입, 에디터 드로어, Odin 브릿지.
그래서 별도 문서로 뺐다.

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 참조 |
|---|---|---|---|
| `HCUP.HCollection` | Runtime | 5 | `HCUP.HDiagnosis` |
| `HCUP.HCollection.Editor` | Editor | 3 | `HCUP.HCollection` |
| `HCUP.HCollection.Odin.Editor` | Editor (`ODIN_INSPECTOR`) | 1 | `HCUP.HCollection` |

Odin 어셈블리는 브릿지가 아니라 **차단기**다 — Odin 이 `HDictionary` 를 자기 방식으로 그리면
커스텀 드로어가 무력화되므로, Odin 렌더를 막고 이쪽 드로어를 강제한다.

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다 — 이 모듈에는 `package.json` 이 없어 개별 UPM 설치 대상이 아니다
([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 이 프로젝트 기준 6000.3.18f1 |
| Odin Inspector | 선택. 설치돼 있으면 차단기 어셈블리가 컴파일된다 |

---

## 쓰기 전에 알아야 할 계약

1. **`HDictionary` 의 직렬화 콜백이 데이터 유실의 원점이었다.** `OnAfterDeserialize` 는 로컬
   딕셔너리에 재구축한 뒤 성공했을 때만 반영한다 — 이 순서를 되돌리면 예외 한 번에 필드가 통째로
   비워진다. 손대기 전에 [docs/HDictionary.md](docs/HDictionary.md) 의 계약 절을 읽을 것.
2. **중복 키는 first-wins 로 보존된다.** 조용히 버리지 않고 경고를 남긴다.
3. **null 키 행은 편집 중인 행이다.** 고아 정리에서 제외되므로 입력 도중 사라지지 않는다.

---

## 주의할 점

1. **진단 API 5종의 호출처가 0건이다** — `NeedsEntriesSync` / `IsEntriesOutOfSync` /
   `ForceSyncEntriesFromDictionary` / `DescribeEntriesSyncState` / `DebugSnapshot`.
   앞의 둘은 이름이 비슷한데 판정 기준이 달라 혼동을 부른다.
2. **`DuplicateKeyCount()` 는 중복 "행" 수를 센다** (같은 키 3행 → `2`). 검증 메시지도
   `"{n} duplicate row(s) (rows sharing an already-used key)"` 로 정정해 행 수임을 명시한다
   (2026-08-07, `HDictionaryValidator.cs` 반영).
3. **`HDictionary` 만 `UnityEngine.Debug` 를 직접 쓴다.** 같은 어셈블리의 `CircularList` 는
   `HLogger` 를 쓰고 asmdef 도 `HCUP.HDiagnosis` 를 참조하므로 기술적 제약은 아니다.
4. **`EnumArray` 는 선언 파일 밖 사용처가 0건이다.**

근거 라인은 [Runtime README](Runtime/README.md) 의 "정리 대상" 절에 있다.
