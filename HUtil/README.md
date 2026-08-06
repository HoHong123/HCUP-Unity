# HUtil — 패키지 카드

> 모듈: `HUtil/` · 소스 23파일 · `package.json` v1.0.2 (`com.hohong123.hutil`)
> 구성 어셈블리 5개 (그중 2개는 빈 껍데기 — 아래 표 참조)
> 코드 문서: **[Runtime README](Runtime/README.md)** · [Editor README](Editor/README.md)

---

## 이 패키지가 담는 것

Animator 상태 콜백 라우팅, 오브젝트 풀링, 폰트 외곽선 — **세 가지뿐이다.**

> [!IMPORTANT]
> 종전 이 문서는 `AssetHandler` / `Data` / `Scene` / `Logger` / `Collection` / `Inspector` /
> `Encode` / `Encrypt` / `Mathx` / `Primitives` / `Web` / `Time` 계층을 HUtil 소속으로 서술했다.
> **이들은 전부 다른 어셈블리로 분리됐다.** 아래 이관표를 보라.

### 이관표 — 예전 HUtil 에 있던 것을 찾는다면

| 찾는 것 | 현재 위치 |
|---|---|
| `AssetProvider`, `MemoryAssetCache`, Load Gate, Lease, Owner 추적 | `HResource` |
| `Data/` 의 Load·Save·Cache·Sequence·Subscription | `HResource` (구세대 `HUtil.Data.*` 는 재편에서 삭제) |
| `SingletonBehaviour`, `SceneLoader`, `CooldownTimer`, `TransformExtension`, Web 수신 | `HCore` |
| `HLogger`, `HDebug` | `HDiagnosis` |
| `HDictionary`, `CircularList`, `EnumArray` | `HCollection` |
| `H*Attribute` 인스펙터 어트리뷰트 | `HInspector` |
| `Encode`, `Encrypt`, `Mathx`, `Primitives` | `HData` |

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 참조 |
|---|---|---|---|
| `HCUP.HUtil` | Runtime | 21 | `HCUP.HData`, `HCUP.HDiagnosis`, `HCUP.HInspector` |
| `HCUP.HUtil.Editor` | Editor | 2 | `Unity.TextMeshPro(.Editor)`, `HCUP.HUtil`, `HCUP.HDiagnosis` |
| `HCUP.HUtil.Odin.Editor` | Editor (`ODIN_INSPECTOR`) | 1 | `HCUP.HUtil`, `HCUP.HDiagnosis` |
| `HCUP.Util.Odin` | Runtime (`ODIN_INSPECTOR`) | **0** | 삭제 대상 |
| `HCUP.Util.Tween` | Runtime (`USE_DOTWEEN`) | **0** | 삭제 대상 |

Runtime 21파일의 내역: `Animation` 15 · `Pooling` 5 · `Font` 1.

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다 — 개별 UPM 설치는 현재 동작하지 않는다
([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 이 프로젝트 기준 6000.3.18f1 |
| TextMeshPro | `HCUP.HUtil.Editor` 가 참조 |
| Odin Inspector | 선택 (`ODIN_INSPECTOR` 정의 시 브릿지 활성) |

`Samples~/SceneUtil` 하나가 있다. `AddressableSequence`·`OwnerTracking` 샘플은 리소스 계층이
`HResource` 로 이관되면서 함께 사라졌다.

---

## 주의할 점

1. **asmdef 참조 2건이 코드 근거 0건이다** — `HCUP.HUtil` 의 `HCUP.HData`·`HCUP.HInspector`
   는 런타임 코드의 `using` 에 나타나지 않는다.
2. **`HCUP.HUtil.Odin.Editor` 는 파일명과 내부 이름이 다르다** — 파일은
   `HCUP.Util.Odin.Editor.asmdef`, `name` 필드는 `HCUP.HUtil.Odin.Editor`.
3. 이 카드는 설치·구성만 다룬다. 라우터가 무엇을 하고 풀이 언제 반납되는지는
   [Runtime README](Runtime/README.md) 에 있다.
