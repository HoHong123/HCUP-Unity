# HUtil - 패키지 카드

> 모듈: `HUtil/` · 소스 23파일 · `package.json` v1.0.2 (`com.hohong123.hutil`)
> 구성 어셈블리 3개
> 코드 문서: **[Runtime README](Runtime/README.md)** · [Editor README](Editor/README.md)

---

## 이 패키지가 담는 것

Animator 상태 콜백 라우팅, 오브젝트 풀링, 폰트 외곽선. **세 가지뿐이다.**

### 이관표 - 예전 HUtil 에 있던 것을 찾는다면

| 찾는 것 | 현재 위치 |
|---|---|
| `AssetProvider`, `MemoryAssetCache`, Load Gate, Leash, Owner 추적 | `HResource` |
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
| `HCUP.HUtil` | Runtime | 21 | `HCUP.HDiagnosis` |
| `HCUP.HUtil.Editor` | Editor | 1 | `Unity.Addressables.Editor`, `HCUP.HUtil`, `HCUP.HDiagnosis` |
| `HCUP.HUtil.Odin.Editor` | Editor (`ODIN_INSPECTOR`) | 1 | `HCUP.HDiagnosis` |

Runtime 21파일의 내역: `Animation` 15 · `Pooling` 5 · `Font` 1.

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다. 개별 UPM 설치는 현재 동작하지 않는다
([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 최소 2021.3 (`package.json` 의 `unity` 필드) |
| Addressables | `HCUP.HUtil.Editor` 가 `Unity.Addressables.Editor` 를 참조한다 |
| Odin Inspector | 선택 (`ODIN_INSPECTOR` 정의 시 `FileBrowser` 창 활성) |

`Samples~/SceneUtil` 하나가 있다.

---

## 주의할 점

1. **`HCUP.HUtil.Odin.Editor` 는 파일명과 내부 이름이 다르다.** 파일은 `HCUP.Util.Odin.Editor.asmdef`, `name` 필드는 `HCUP.HUtil.Odin.Editor`.
2. 이 카드는 설치·구성만 다룬다. 라우터가 무엇을 하고 풀이 언제 반납되는지는
   [Runtime README](Runtime/README.md) 에 있다.

---

## 히스토리

### 2026-08-07 :: 빈 어셈블리 2개 삭제와 asmdef 참조 정리

- 이전: `HCUP.Util.Odin`(Runtime, `ODIN_INSPECTOR`) / `HCUP.Util.Tween`(Runtime, `USE_DOTWEEN`) 이 소스 0파일 빈 어셈블리로 있었다. `HCUP.HUtil` 은 `HCUP.HData`·`HCUP.HInspector` 를, `HCUP.HUtil.Editor` 는 `Unity.TextMeshPro(.Editor)` 2종을, `HCUP.HUtil.Odin.Editor` 는 `HCUP.HUtil` 을 코드 근거 없이 참조했다.
- 현재: 빈 어셈블리 2개는 폴더째 삭제됐고, 근거 없는 참조는 전부 asmdef 에서 제거됐다.

### 2026-08-05 :: HUtil 계층 분리 완료 (마지막 이관은 `HResource`)

- 이전: 이 문서는 `AssetHandler` / `Data` / `Scene` / `Logger` / `Collection` / `Inspector` / `Encode` / `Encrypt` / `Mathx` / `Primitives` / `Web` / `Time` 계층을 HUtil 소속으로 서술했다. `Samples~` 에는 `AddressableSequence`·`OwnerTracking` 샘플도 있었다.
- 현재: 이 계층들은 전부 다른 어셈블리로 분리됐다 (위 이관표). 두 샘플은 리소스 계층이 `HResource` 로 이관되면서 함께 사라졌다.
