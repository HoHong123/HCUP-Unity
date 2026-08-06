# HDialogue — 패키지 카드

> 모듈: `HDialogue/` · 소스 75파일 (저장소 최대) · `package.json` v1.0.0 (`com.hohong123.hdialogue`)
> 구성 어셈블리 2개
> 코드 문서: **[Runtime README](Runtime/README.md)** · [Editor README](Editor/README.md)

---

## 이 패키지가 담는 것

노드 그래프로 저작하는 대화 시스템. 저작(에디터) → 검증 → 런타임 순회 → 텍스트·포트레이트·오디오
표시까지의 파이프라인 전체가 들어 있다.

런타임 55 + 에디터 20 파일이 8개 시스템으로 나뉜다.

| 시스템 | 파일 | 담는 것 | 문서 |
|---|---|---|---|
| Graph | 6 | 순회 엔진 `DialogueDirector`, 카탈로그, 변수 주입 | [docs/Graph.md](docs/Graph.md) |
| Nodes | 12 | 노드 클래스 9종 + 열거형 3 | [docs/Nodes.md](docs/Nodes.md) |
| Controller | 4 | `DialogueManager` 배선, UI·입력·히스토리 중계 | [docs/Controller.md](docs/Controller.md) |
| Text | 11 | 타이핑 출력, 태그 파서, 토큰, 텍스트 이펙트 | [docs/Text.md](docs/Text.md) |
| Portrait | 18 | 캐릭터 등장·퇴장·포즈·트랜지션 연출 | [docs/Portrait.md](docs/Portrait.md) |
| Audio | 4 | 대화 BGM/SFX 와 글자 블립 | [docs/Audio.md](docs/Audio.md) |
| Editor · NodeView | 14 | GraphView 기반 노드 저작 | [docs/Editor-NodeView.md](docs/Editor-NodeView.md) |
| Editor · Validator | 6 | 카탈로그 정적 검증 (E001~E010 / W001~W007) | [docs/Editor-Validator.md](docs/Editor-Validator.md) |

---

## 구성 어셈블리

| asmdef | 범위 | 소스 | 참조 |
|---|---|---|---|
| `HCUP.HDialogue` | Runtime | 55 | `Unity.TextMeshPro`, `Unity.InputSystem`, `UniTask`, `UniTask.TextMeshPro`, `HCUP.HUtil`, `HCUP.HUI`, `HCUP.HAudio`, `HCUP.HCore`, `HCUP.HInspector`, `HCUP.HDiagnosis`, `HCUP.HWindows.NodeWindow`, `HCUP.HCollection`, `HCUP.HcupLocalization`, `HCUP.HResource` |
| `HCUP.HDialogue.Editor` | Editor | 20 | `Unity.Addressables(.Editor)`, `Unity.ResourceManager`, `UniTask(.Addressables)`, `HCUP.HDialogue`, `HCUP.HWindows.NodeWindow(.Editor)` |

**이 저장소에서 의존이 가장 넓은 모듈이다.** 런타임 참조 14개 중 8개가 HCUP 내부 모듈이라,
HDialogue 를 쓰려면 사실상 저장소 전체가 따라온다.

---

## 설치 · 요구 사항

저장소를 통째로 가져다 쓴다 — 개별 UPM 설치는 현재 동작하지 않는다
([루트 README 의 설치 절](../README.md#설치) 참조).

| 항목 | 비고 |
|---|---|
| Unity | 이 프로젝트 기준 6000.3.18f1 |
| TextMeshPro | 텍스트 출력 전 구간 |
| Input System | `Runtime/Input/DialogueInputActions.inputactions` |
| UniTask | 순회·타이핑의 비동기 흐름 |
| Addressables | 에디터 미리보기 경로 |

---

## 어디부터 볼까

1. [`docs/Graph.md`](docs/Graph.md) — 순회 엔진. 이 시스템의 심장이고 방어 장치가 가장 많다
2. [`docs/Nodes.md`](docs/Nodes.md) — 노드 9종이 각각 무엇을 하는지
3. [`docs/Editor-NodeView.md`](docs/Editor-NodeView.md) — 카탈로그를 실제로 만들 때

---

## 주의할 점

1. **`docs/TagUsage.md` 는 인라인 이벤트 라우팅을 사실과 다르게 서술한다.** 문서는
   `<event=sfx.coin>` 이 SFX 를 울린다고 적었지만, 인라인 태그 구독자는 `CharacterStageDirector`
   하나뿐이고 `portrait.` 접두가 아니면 폐기된다. **현재 인라인 태그로 SFX 를 울릴 방법이 없다.**
   `Face`/`Slot` 인자값, `Show` 인자 유무, W003 정의 등 6건이 더 어긋나 있다.
2. **`DialogueDirector` 에는 방어 장치가 여러 겹 들어 있다** (순회 상한, 주기적 프레임 양보,
   실패 종료 경로 통일, TCS 소유권 확인). 그래프 순환이 정상 패턴이라 생긴 것들이므로,
   손대기 전에 [`docs/Graph.md`](docs/Graph.md) 의 "계약" 절을 먼저 읽을 것.
3. **검증기는 자동으로 돌지 않는다.** 저장·빌드·플레이 진입 어디에도 훅이 없다. 부작용 없는
   순수 정적 검사이므로 CI 에 그대로 걸 수 있다.
4. **설계만 남고 런타임이 비어 있는 값들이 있다** — `WaitNode.conditionKey`,
   `PortraitTransitionType.SlideIn`/`Scale`, `PortraitPoseType.Sequence`, `DialogueTokenType.Sfx`.
   인스펙터에서 고를 수 있지만 동작하지 않는다.

근거 라인은 [Runtime README](Runtime/README.md) 의 "정리 대상" 절에 있다.
