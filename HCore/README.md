# HCore - 패키지 카드

> 모듈: `HCore/` · 소스 17파일 · `package.json` 없음 (저장소 통째 사용)
> 구성 어셈블리 1개
> 코드 문서: **[Runtime README](Runtime/README.md)**

---

## 이 패키지가 담는 것

다른 모듈이 딛고 서는 엔진 기반 계층. 네 갈래이며 서로 거의 참조하지 않는다.

| 폴더 | 파일 | 담는 것 | 문서 |
|---|---|---|---|
| `Core` | 4 | **`SingletonBehaviour<T>`**, `PlayerPrefsHandler`, `TransformExtension`, `HServiceLocator` | [docs/SingletonBehaviour.md](docs/SingletonBehaviour.md) |
| `Scene` | 6 (+Demo 1) | 씬 카탈로그·키 매핑·비동기 로드·진행률·전환 이벤트 | [docs/Scene.md](docs/Scene.md) |
| `Time` | 3 | `CooldownTimer`, `DateChecker`, `TimeUtil` | Runtime README |
| `Web` | 3 | WebGL JS → C# 수신 배선 | Runtime README |

**이 모듈에서 가장 중요한 파일은 `SingletonBehaviour.cs` 다.** 저장소 전체에서 12종 이상이
이걸 상속하고, 그 계약 위반이 이 저장소의 반복 결함 원인이었다. 그래서 40행짜리 파일에
별도 문서를 뒀다.

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 참조 |
|---|---|---|---|
| `HCUP.HCore` | Runtime | 17 | `HCUP.HData`, `HCUP.HDiagnosis`, `HCUP.HInspector`, `UniTask` |

동반 Editor 어셈블리는 없다.

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다 - 이 모듈에는 `package.json` 이 없어 개별 UPM 설치 대상이 아니다
([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 6000.3 에서 개발·검증 |
| UniTask | 씬 로드의 비동기 흐름 |
| Odin Inspector | 선택. 정의 시 `SingletonBehaviour` 의 base 가 `SerializedMonoBehaviour` 로 바뀐다 |

---

## 쓰기 전에 알아야 할 계약

`SingletonBehaviour<T>` 를 상속하면 지켜야 하는 것들이다. 어기면 조용히 깨진다.

1. **`Awake` 를 오버라이드하면 첫 줄에서 `base.Awake()` 를 부른다.** base 가 `instance` 할당과
   중복 인스턴스 파괴를 담당한다.
2. **`base.Awake()` 뒤에 `if (instance != this) return;` 가드를 둔다.** base 는 중복 인스턴스에
   `Destroy` 를 걸지만 그 프레임의 나머지 코드는 계속 실행된다. 가드가 없으면 죽어가는 인스턴스가
   이벤트를 구독하거나 정적 상태를 덮어쓴다.
3. **`OnDestroy` 는 `protected override` + 마지막에 `base.OnDestroy()`.** `private new void
   OnDestroy()` 로 숨기면 Unity 가 최파생 하나만 호출해 **base 정리가 영원히 실행되지 않는다.**

세 규약의 근거 라인과 실제 위반 사례는 [docs/SingletonBehaviour.md](docs/SingletonBehaviour.md) 에 있다.

---

## 주의할 점

1. **`Scene/Demo/` 가 런타임 asmdef 폴더 안에 있다.** `SceneTester.cs`(전역 네임스페이스)는 파일 전체가 `#if UNITY_EDITOR` 로 가드돼 빌드 컴파일에서 빠진다. 테스트 씬 3개(`Test1~3.unity`)와 `TestScenes.asset` 은 같은 폴더에 남아 있다.
2. **`CooldownTimer` / `DateChecker` / `TimeUtil` 의 네임스페이스는 `HCore.HTime` 이다.** 폴더명 `Time` 과 다르고, `HCore` 만 `using` 하면 보이지 않는다.

근거 라인은 [Runtime README](Runtime/README.md) 의 "정리 대상" 절에 있다.

---

## 히스토리

### 2026-08-07 :: asmdef 미사용 참조 제거와 감사 지적 해소

- 이전: `HCUP.HCore.asmdef` 가 코드 근거 없는 참조 4건(`Unity.Addressables`, `Unity.ResourceManager`, `UniTask.Addressables`, `HCUP.HUtil`)을 들고 있어, `HCore` 를 참조하는 모든 어셈블리에 Addressables 의존이 전파됐다. `SceneLoader.ReloadActiveSceneAsync` 는 in-flight 가드보다 먼저 `Time.timeScale = 1f` 를 대입해 거부된 요청도 배속을 초기화했다. `SceneCatalogSO` 는 `UnityEngine.Debug.LogError` 를 직접 써서 `HLogger.OnLogPublished` 를 타지 않았다. `Scene/Demo/SceneTester.cs` 는 가드 없이 런타임 asmdef 에 포함돼 플레이어 빌드에 실렸다.
- 현재: asmdef 참조는 `HCUP.HData` / `HCUP.HDiagnosis` / `HCUP.HInspector` / `UniTask` 넷이다. `ReloadActiveSceneAsync` 는 `isLoading` 검사를 `timeScale` 보정보다 앞에 둔다. `SceneCatalogSO` 의 진단 로그는 `HLogger.Error` 경유다. `SceneTester.cs` 는 파일 전체가 `#if UNITY_EDITOR` 가드 안에 있다.
