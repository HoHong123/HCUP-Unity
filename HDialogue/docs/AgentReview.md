# HDialogue Agent Review

작성일: 2026-05-17  
리뷰 범위: `HDialogue/Runtime`, `HDialogue/Editor`, `HCUP.HDialogue.asmdef`, `HCUP.HDialogue.Editor.asmdef`

## 재검토 결과

재검토일: 2026-05-17  
상황: Claude Code 수정사항 반영 후 `DialogueDirector`, `DialogueTextController`, `CharacterStageDirector`, `CharacterPortraitController`, `DialogueCatalogValidator`를 재확인했다.

### 해결 또는 개선된 항목

- `CharacterStageDirector.ShowCharacter()`의 즉시 null 역참조 위험은 완화됐다. `_GetOrCreateController()` 반환값 null guard가 추가되어 `controllerPrefab` 누락 시 `ctrl.SetSlot()`에서 바로 터지지는 않는다.
- `CharacterStageDirector`의 private dictionary 필드 `_controllers`, `_characterToSlot`, `_slotToCharacter`는 `controllers`, `characterToSlot`, `slotToCharacter`로 정리되어 프로젝트 변수 컨벤션에 가까워졌다.
- `CharacterStageDirector.OnDestroy()`가 Unity Life Cycle region으로 이동했다. 생명주기 함수 위치는 이전보다 낫다.
- `CharacterStageDirector`에 있던 shake/bounce async 로직이 `CharacterPortraitController.Shake()` / `Bounce()`로 이동했고, `TransitionChannel.Motion`을 추가해 중복 호출 취소 구조가 생겼다.
- `DialogueTextController`의 Event 토큰은 Instant 모드에서도 발화되도록 수정됐다. 이 수정은 맞다. 전체 스킵/Instant에서도 `portrait.*` 같은 액션 이벤트는 실행되어야 한다.
- `DialogueCatalogValidator._CheckChoiceKeySync()`가 index 기반 비교에서 set 기반 비교로 바뀌었다. choice와 port entry의 순서가 달라도 key 집합이 같으면 통과하므로, 이전보다 그래프 편집 내성이 좋다.
- 주요 파일의 깨진 한글 dev log와 region 이름이 상당 부분 정상 UTF-8 텍스트로 복구됐다. PowerShell 기본 출력에서는 깨져 보일 수 있으나 `-Encoding UTF8` 기준으로 정상 확인했다.

### 아직 해결되지 않은 핵심 항목

#### 1. `DialogueDirector.Stop()` 종료 계약은 그대로다

`Stop()`은 여전히 `_CancelAndDisposeCts()` 후 `state = Idle`만 수행한다. `OnCatalogExit`는 `DialogueExitNode` 도달 시에만 발행된다.

남은 문제:

- 강제 중단 시 외부 UI/입력 잠금/컷신 상태 복구 로직이 누락될 수 있다.
- `currentCatalog`, `currentNode`, `choiceTcs`, `waitConditionTcs` 정리 계약이 명확하지 않다.
- `Stop()`이 "종료 이벤트가 없는 취소"인지 "대화 종료"인지 API 의미가 흐리다.

판단: 여전히 높은 우선순위 문제다. Claude 수정으로 해결되지 않았다.

#### 2. 선택지 필터링 결과가 여전히 UI로 전달되지 않는다

`_ProcessChoiceNode()`는 `validChoices`를 계산하지만, 이벤트는 여전히 `OnChoicePresent?.Invoke(node)`로 원본 `DialogueChoiceNode`만 전달한다. `SelectChoice()`도 입력 key가 현재 valid choice에 포함되는지 검증하지 않는다.

남은 문제:

- UI는 조건 통과 선택지만 알 수 없다.
- UI가 node.Choices 전체를 표시하면 조건 미통과 선택지도 노출된다.
- 외부에서 임의 key를 `SelectChoice()`로 넘겨도 현재 valid set 검증 없이 다음 노드 resolving으로 넘어간다.

판단: 여전히 높은 우선순위 문제다. 대화 UI와 직접 맞물리는 계약이라 먼저 고쳐야 한다.

#### 3. `DialogueTextController.PlayLine(null)` 방어가 없다

`PlayLine(DialogueLine line)`은 여전히 null 체크 없이 `line.RawText`, `line.OverrideBlipToken`을 읽는다. public API이므로 호출자가 잘못 쓰면 즉시 NRE가 난다.

판단: 작은 수정으로 막을 수 있는 public API 결함이다. `Debug.Assert(line != null)` 또는 명시 예외가 필요하다.

#### 4. 필수 SerializeField Assert 정책은 아직 적용되지 않았다

`controllerPrefab`은 null guard로 즉시 NRE만 막았을 뿐, 필수 연결 누락을 Assert로 강제하지 않는다. `DialogueTextController.tmpText`도 여전히 null-safe로 통과한다.

프로젝트 지침상 사실상 필수 연결인 UI/Prefab 참조는 early return보다 Assert가 맞다.

판단: 컨벤션 관점에서 아직 미해결이다.

#### 5. Parser/Validator 태그 규칙 중복은 그대로다

`DialogueTagParser`와 `DialogueTextValidator`는 여전히 태그 목록과 규칙을 각자 들고 있다. 태그 추가 시 한쪽만 갱신될 위험은 해결되지 않았다.

판단: 즉시 런타임 버그는 아니지만, 확장 단계에서 반드시 문제를 만든다.

### 새로 확인된 문제

#### 1. Motion 취소 시 anchoredPosition 복구가 보장되지 않는다

파일: `HDialogue/Runtime/Portrait/CharacterPortraitController.cs`  
관련 위치: `Shake()`, `Bounce()`, `_ShakeAsync()`, `_BounceAsync()`

`Shake()`가 실행 중일 때 `Bounce()`가 호출되면 `TransitionChannel.Motion` 취소로 이전 `_ShakeAsync()`가 `OperationCanceledException`을 catch하고 종료한다. 그런데 catch 경로에서는 `rt.anchoredPosition = origin` 복구가 실행되지 않는다. 그 결과 shake 중간 offset이 남은 상태에서 bounce가 그 offset을 origin으로 삼을 수 있다.

권장:

- `_ShakeAsync()` / `_BounceAsync()`의 `finally`에서 token 소유권이 유효할 때 origin 복구를 수행한다.
- 또는 motion 시작 전 기준 위치를 `currentSlot.AnchorPos + poseOffset` 같은 canonical position에서 계산하도록 분리한다.
- MoveToSlot/SetPose와 Motion이 동시에 anchoredPosition을 쓰므로, 장기적으로는 슬롯 위치와 motion offset을 분리하는 구조가 더 안전하다.

#### 2. `CharacterPortraitController.Bind()`가 기존 컨트롤러의 포즈/좌표를 초기화할 수 있다

`_GetOrCreateController()`는 새 컨트롤러뿐 아니라 기존 컨트롤러에도 매번 `ctrl.Bind(set, style)`을 호출한다. `Bind()` 내부는 default pose를 적용하고 `_ApplyPoseImmediate()`를 호출한다. 기존 캐릭터를 다시 show하거나 같은 controller를 재사용할 때 의도치 않은 default pose/anchoredPosition 변경이 발생할 수 있다.

권장:

- 최초 생성 시의 `Bind()`와 style refresh를 분리한다.
- `Bind()`가 상태를 초기화하는 함수라면 이름을 `Initialize()` 계열로 명확히 하고 반복 호출을 피한다.

### 최신 우선순위

1. `DialogueDirector` 종료/중단 계약 정리.
2. Choice presentation DTO와 valid key 검증 추가.
3. `DialogueTextController.PlayLine(null)` 및 필수 SerializeField Assert 적용.
4. `CharacterPortraitController` Motion 취소 시 위치 복구 보장.
5. Parser/Validator 태그 정의 단일화.
6. `CharacterPortraitController.Bind()` 반복 호출 정책 정리.

## 결론

HDialogue는 현재 "대화 그래프 데이터 - 런타임 실행기 - 텍스트 출력 - 포트레이트 연출 - 에디터 검증"의 큰 분리는 잡혀 있다. Claude Code가 작성한 코드의 방향은 나쁘지 않다. 다만 지금 상태를 그대로 안정화 단계로 넘기는 것은 이르다. 실행 중단 상태, 선택지 필터링, 포트레이트 null 흐름, 태그 파서/검증기 중복, 문자열 인코딩 깨짐이 유지보수성과 런타임 신뢰도를 직접 깎고 있다.

특히 `DialogueDirector`는 시스템의 중심인데, 상태 전이가 엄밀하지 않고 외부 UI가 선택지 필터 결과를 알 수 없는 구조다. 이 부분은 기능 추가 전에 먼저 고쳐야 한다.

## 구조 요약

- `Runtime/Graph`: `DialogueCatalogSO`, `DialogueDirector`, 노드 타입, 변수 컨텍스트가 위치한다. 그래프 실행의 핵심 계층이다.
- `Runtime/Controller`: `DialogueTextController`가 TMP 기반 타이프라이터 출력, 스킵, 진행 요청, 이벤트 태그 발화를 담당한다.
- `Runtime/Parser`: `DialogueTagParser`가 커스텀 태그와 TMP 태그를 `DialogueToken`으로 분해한다.
- `Runtime/Effect`: `TextEffectHandler`가 TMP vertex 조작으로 shake/wave/rainbow 효과를 처리한다.
- `Runtime/Portrait`: 캐릭터 레지스트리, 스테이지 레이아웃, 포트레이트 컨트롤러, 인라인 이벤트 기반 연출을 담당한다.
- `Runtime/Audio`: `IBlipSfxService`로 HAudio 결합을 낮추려는 브릿지 계층이다.
- `Editor`: 노드 윈도우, 노드 뷰, 카탈로그/텍스트 검증기가 있다.

## 긍정적인 부분

- 그래프 노드 데이터와 런타임 실행이 분리되어 있다. `DialogueCatalogSO`가 HWindows NodeWindow 기반 데이터를 들고, `DialogueDirector`가 실행만 담당하는 방향은 맞다.
- 텍스트 출력이 토큰 기반이다. RawText를 바로 TMP에 넣는 방식보다 확장성이 좋고, pause/speed/event/effect/voice 같은 기능 추가 지점이 명확하다.
- 포트레이트 연출이 별도 director/controller로 분리되어 있다. 대화 텍스트 컨트롤러에 캐릭터 연출까지 넣지 않은 것은 올바른 판단이다.
- 오디오는 `IBlipSfxService`를 통해 토큰 문자열만 넘긴다. HDialogue가 AudioClip을 직접 들고 있지 않은 점은 어셈블리 독립성 측면에서 좋다.
- Editor 검증기를 둔 방향은 맞다. 그래프 대화 시스템은 런타임에서 터지기 전에 에디터에서 막는 것이 비용이 훨씬 낮다.

## 치명/높음 우선순위 지적

### 1. `DialogueDirector.Stop()` 이후 `OnCatalogExit`가 발행되지 않는다

파일: `HDialogue/Runtime/Graph/DialogueDirector.cs`  
관련 위치: `Stop()` 113-117, `_PlayCatalogAsync()` 133-143, `_ProcessExitNode()` 210-216

`Stop()`은 CTS를 취소하고 `state = Idle`만 한다. 정상 종료는 `DialogueExitNode`에서만 `OnCatalogExit`가 발행된다. 즉 강제 중단과 정상 종료가 외부 구독자 입장에서 완전히 다른 흐름이다.

문제:

- UI, 입력 잠금, 컷신 상태 복구 같은 외부 정리 로직이 `OnCatalogExit`에 묶이면 `Stop()`에서 누락된다.
- `currentCatalog`, `currentNode`, `choiceTcs`, `waitConditionTcs`도 명확하게 정리되지 않는다.
- `Stop()`이 "대화 종료"인지 "일시 취소"인지 API 의미가 불명확하다.

권장:

- `Stop()`에 종료 사유를 넣거나 `CancelCatalog`/`FinishCatalog`를 분리한다.
- 강제 중단 시에도 별도 이벤트 또는 `OnCatalogExit(currentCatalog, "Stopped")`에 준하는 정리 신호를 보장한다.
- `currentCatalog`, `currentNode`, pending TCS를 함께 정리한다.

### 2. 선택지 필터링 결과가 UI로 전달되지 않는다

파일: `HDialogue/Runtime/Graph/DialogueDirector.cs`  
관련 위치: `_ProcessChoiceNode()` 261-285, `OnChoicePresent` 60

`_ProcessChoiceNode()`는 `validChoices`를 계산하지만 `OnChoicePresent?.Invoke(node)`로 원본 노드만 넘긴다. 외부 UI는 어떤 선택지가 조건을 통과했는지 알 수 없다. 결국 UI가 조건식을 다시 계산하거나, 유효하지 않은 선택지를 표시할 가능성이 생긴다.

문제:

- 조건 필터링 책임이 director와 UI로 중복될 수 있다.
- `SelectChoice(string choiceKey)`는 전달된 key가 현재 validChoices에 속하는지 검증하지 않는다.
- 조건 미통과 선택지도 외부 UI가 노출하면 런타임 분기가 잘못 진행될 수 있다.

권장:

- `DialogueChoicePresentedContext` 같은 DTO를 만들고 `PromptText`, `IReadOnlyList<ChoiceData> ValidChoices`를 넘긴다.
- `SelectChoice`에서 현재 대기 중인 valid key 집합을 검증한다.
- fallback도 실제 포트 존재 여부를 검증해야 한다.

### 3. `CharacterStageDirector.ShowCharacter()`는 null controller를 즉시 역참조할 수 있다

파일: `HDialogue/Runtime/Portrait/CharacterStageDirector.cs`  
관련 위치: `ShowCharacter()` 98-123, `_GetOrCreateController()` 258-271

`controllerPrefab == null`이면 `_GetOrCreateController()`가 `null`을 반환한다. 그런데 `ShowCharacter()`는 곧바로 `ctrl.SetSlot(slotConfig)`를 호출한다. 이건 경고가 아니라 즉시 `NullReferenceException`이다.

문제:

- `controllerPrefab`은 실제로 필수 의존성이다. 현재 코드처럼 null-safe인 척하면 에러 위치가 흐려진다.
- 프로젝트 컨벤션상 필수 연결 누락은 early-exit보다 `Assert` 대상이다.

권장:

- `Awake()` 또는 `OnValidate()`에서 `Debug.Assert(controllerPrefab != null)`로 강제한다.
- `_GetOrCreateController()`가 null을 반환하지 않도록 계약을 바꾸거나, 반환 직후 명확히 중단한다.

### 4. `DialogueTextController.PlayLine(null)` 방어가 없다

파일: `HDialogue/Runtime/Controller/DialogueTextController.cs`  
관련 위치: `PlayLine()` 83-97, `_PlayLineAsync()` 146-153

`PlayLine(DialogueLine line)`에 null이 들어오면 `currentLine = line` 후 `_PlayLineAsync()`에서 `currentLine.RawText`를 읽다가 터진다. `DialogueDirector`가 항상 정상 값을 만든다는 가정은 현재 시스템 내부에서는 맞을 수 있지만 public API로 열려 있으므로 계약을 명확히 해야 한다.

권장:

- public API 초입에서 `Debug.Assert(line != null)` 또는 명확한 예외를 둔다.
- `tmpText`도 필수 UI 참조라면 null 허용이 아니라 assert가 맞다. 출력 컨트롤러가 텍스트 없이 동작하는 것은 정상 플로우가 아니다.

### 5. 태그 파서와 텍스트 검증기가 같은 지식을 복제한다

파일: `HDialogue/Runtime/Parser/DialogueTagParser.cs`, `HDialogue/Editor/Validator/DialogueTextValidator.cs`  
관련 위치: parser `tmpTags/effectTags` 29-43, validator `pairTags/allCustomTags/tmpTags` 35-67

현재 validator는 parser의 태그 목록과 규칙을 복사해서 들고 있다. 주석에도 복제 필요가 명시되어 있다. 이건 장기적으로 반드시 어긋난다.

문제:

- 새 태그 추가 시 runtime parser와 editor validator 중 하나만 갱신될 가능성이 높다.
- parser는 effect 닫기 태그에서 stack top 이름을 검증하지 않고 pop한다. validator는 mismatch를 에러로 잡는다. 이미 런타임과 에디터 규칙이 다르다.

권장:

- `DialogueTagDefinition` 또는 `DialogueTagRegistry`를 Runtime에 두고 Parser/Validator가 같은 정의를 참조하게 한다.
- validator가 Editor에 있어도 규칙 데이터는 Runtime 또는 공용 내부 타입으로 분리한다.
- parser도 `</wave>`가 `<shake>`를 닫는 식의 mismatch를 감지해야 한다.

## 중간 우선순위 지적

### 6. 인코딩 깨짐이 대량으로 존재한다

여러 파일의 상단/하단 dev log와 일부 문자열이 깨져 있다. 실제 코드 일부는 정상 한글로 보이지만, 많은 주석과 경고 문구가 `???`, `罹먮┃`, `洹몃옒` 형태로 깨져 있다.

이건 단순 미관 문제가 아니다.

- dev log의 역할을 상실한다.
- 에러 메시지가 런타임에서 이해 불가능해질 수 있다.
- 이후 변경자가 주석을 신뢰하지 못한다.

권장:

- 파일 인코딩을 UTF-8로 통일한다.
- 이미 깨진 dev log는 원문 복구가 불가능하면 과감히 다시 작성한다.
- 문서/주석은 코드보다 덜 중요하지 않다. 특히 이 프로젝트는 dev log 규칙이 있으므로 깨진 상태는 컨벤션 위반이다.

### 7. `DialogueDirector`가 너무 많은 책임을 가진다

파일: `HDialogue/Runtime/Graph/DialogueDirector.cs`

현재 director는 그래프 실행, 상태 관리, 텍스트 출력, 선택지 대기, 변수 처리, 포트레이트 stage context 생성, 컷신 자동 진행까지 모두 담당한다.

아직은 파일 크기가 감당 가능하지만, 다음 기능이 붙으면 바로 비대해진다.

- localization table 적용
- save/load resume
- voice acting
- rollback/history
- condition expression 확장
- auto mode/log mode

권장:

- `DialogueRuntimeContext`: catalog, variables, services, cancellation 상태를 묶는다.
- `IDialogueNodeProcessor<TNode>` 또는 최소한 choice/branch/line 처리기를 private class로 분리한다.
- line build와 stage context build는 별도 factory로 빼도 된다.

### 8. 상태 머신이 암묵적이다

파일: `HDialogue/Runtime/Graph/DialogueDirector.cs`, `HDialogue/Runtime/Controller/DialogueTextController.cs`

`DialogueDirectorState`와 `TextDisplayState`가 있지만 전이 규칙이 코드 흐름에 흩어져 있다. 예를 들어 line 재생 중 `PlayingLine`으로 설정했다가 곧바로 `WaitingForLineEnd`가 되며, 중간 상태가 외부에서 의미 있게 관찰될 시간이 거의 없다.

권장:

- 상태 전이를 `_SetState()`로 통일하고, 디버그 로그 또는 이벤트를 선택적으로 붙인다.
- 잘못된 외부 호출은 warning으로 지나가기보다 현재 상태와 허용 상태를 명확히 보여준다.
- `SelectChoice`처럼 런타임 분기에 영향을 주는 API는 상태뿐 아니라 payload 검증까지 해야 한다.

### 9. 포트레이트 효과 코루틴이 오브젝트 수명 취소를 고려하지 않는다

파일: `HDialogue/Runtime/Portrait/CharacterStageDirector.cs`  
관련 위치: `_ShakeAsync()` 224-238, `_BounceAsync()` 240-254

`_ShakeAsync()`와 `_BounceAsync()`는 cancellation token이 없다. 컨트롤러가 hide/destroy되거나 같은 효과가 중복 호출되어도 기존 효과가 계속 anchoredPosition을 덮어쓸 수 있다.

권장:

- 포트레이트 컨트롤러 내부 transition channel로 effect 채널을 추가한다.
- 최소한 `destroyCancellationToken` 또는 controller 제공 토큰을 사용한다.
- 같은 캐릭터의 shake/bounce 중복 호출 정책을 정해야 한다. 덮어쓰기인지 누적인지 불명확하다.

### 10. `AudioManagerBlipAdapter`가 `AudioManager.Instance`에 직접 의존한다

파일: `HDialogue/Runtime/Audio/AudioManagerBlipAdapter.cs`

`IBlipSfxService` 추상화를 둔 것은 좋지만 기본 adapter는 다시 싱글톤에 강하게 묶인다. 이건 adapter 역할상 허용 가능하나, 테스트와 씬 초기화 순서 문제는 남는다.

권장:

- `AudioManager.Instance`가 없을 때의 동작을 명확히 한다.
- prewarm이 필수라면 validator나 diagnostics에서 token 존재 여부를 점검한다.

## Editor/검증기 리뷰

`DialogueCatalogValidator`는 방향이 좋다. EntryNode 개수, Root 일치, outgoing edge, choice key sync, boolean branch key 검증은 필요한 규칙이다.

보완할 규칙:

- `ChoiceNode` fallback key가 실제 port/edge에 존재하는지 검증.
- `BranchMode.IntRange`의 key 형식과 range overlap 검증.
- `BranchMode.Switch`의 빈 key, 중복 key 검증.
- `DialogueLineNode`의 speakerKey가 `CharacterRegistrySO`에 존재하는지 검증할 수 있는 통합 검증 모드.
- `DialogueCatalogSO.LocalizationTable`을 실제로 쓸 예정이면 rawText와 localization key 정책을 명확히 나누는 검증.
- Cutscene catalog에서 choice/wait user input이 허용되는지 정책 검증.

`DialogueTextValidator`는 현재 parser 규칙 복제로 인해 유지보수 위험이 있다. 기능 자체보다 구조가 문제다. parser와 validator가 같은 tag definition을 공유하도록 바꾸는 것이 먼저다.

## 컨벤션 위반/스타일 지적

- 리전 이름이 깨진 파일이 많다. `#region 蹂??`, `#region Private ???` 같은 상태는 즉시 정리해야 한다.
- private 변수 접근제어자 생략 규칙은 대체로 지켜졌지만, readonly dictionary 변수에 `_controllers`처럼 언더스코어가 붙어 있다. 사용자 컨벤션상 private 변수는 camelCase이므로 `controllers`, `characterToSlot`, `slotToCharacter`가 맞다.
- public/protected/private 리전 순서가 일부 파일에서 기능 중심으로 섞인다. 프로젝트 규칙을 엄격히 적용할 거면 구조 정렬이 필요하다.
- 필수 SerializeField가 null일 수 없는 곳에서 null-safe early return을 남발한다. `tmpText`, `controllerPrefab`, 주요 registry/layout은 Assert 또는 명확한 초기화 실패로 처리해야 한다.
- 한 줄 if 괄호 규칙은 대체로 지켜졌지만, 같은 블록 안에서 짧은 if와 긴 if가 섞인 곳은 통일성이 떨어진다.

## 개선 우선순위

1. `DialogueDirector`의 종료/중단 이벤트 계약과 pending TCS 정리부터 고친다.
2. Choice presentation DTO를 추가하고 valid choice만 UI로 전달한다.
3. 필수 SerializedField에 Assert 정책을 적용한다. 특히 `DialogueTextController.tmpText`, `CharacterStageDirector.controllerPrefab`.
4. 태그 정의를 단일 소스로 분리하고 Parser/Validator 중복을 제거한다.
5. 깨진 dev log와 region 이름을 UTF-8 기준으로 정리한다.
6. 포트레이트 effect cancellation 정책을 추가한다.
7. Validator 규칙을 fallback, int range, switch, speaker/pose/slot 검증까지 확장한다.

## 최종 판단

현재 HDialogue는 프로토타입에서 기능형 시스템으로 넘어가는 중간 단계다. 구조의 방향은 맞지만, "돌아가는 것처럼 보이는 코드"와 "운영 가능한 대화 시스템" 사이의 간극이 아직 있다. 가장 큰 문제는 runtime contract가 흐릿하다는 점이다. 특히 선택지와 종료 흐름은 외부 UI/게임 상태와 직접 맞물리므로, 지금 고치지 않으면 이후 기능이 붙을수록 수정 비용이 커진다.

이 리뷰 기준으로는 추가 기능 개발보다 안정화 리팩터링이 먼저다.
