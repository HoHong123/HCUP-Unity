#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 캐릭터 포트레이트 무대 감독 MonoBehaviour.
 *
 * 특징 / 지원기능 ::
 * + Bind(registry, layout, textController) — 레지스트리·레이아웃·텍스트컨트롤러 연결
 * + EnterLine(LineStageContext)  — 화자 등장·포즈·슬롯·하이라이트 5단계 처리
 * + ShowCharacter / HideCharacter / SetPose / MoveToSlot / SetFacing / ClearAll
 * + portrait.* 인라인 이벤트: textController.OnEventTagFired 구독
 *
 * 주의사항 ::
 * controllerPrefab / registry / layout 필수 연결 — Awake Debug.Assert 로 검증.
 * leftSlotRoot / rightSlotRoot : 씬의 정확한 위치에 배치할 것. 미연결 시 this.transform 사용.
 * spriteProvider: Awake에서 Addressable 기본 구성으로 생성. OnDestroy에서 ReleaseAll.
 * OnDestroy에서 OnEventTagFired 구독 해제 + spriteProvider.ReleaseAll.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using HDiagnosis.Logger;
using HInspector;
using HResource.Provider;
using UnityEngine;

namespace HDialogue {
    public sealed class CharacterStageDirector : MonoBehaviour {
        #region Fields
        [HTitle("Controller")]
        [SerializeField]
        CharacterPortraitController controllerPrefab;

        [HTitle("Stage Roots")]
        [SerializeField]
        Transform leftSlotRoot;
        [SerializeField]
        Transform rightSlotRoot;

        [HTitle("Character Data")]
        [SerializeField]
        CharacterRegistrySO registry;
        [SerializeField]
        StageLayoutSO layout;

        DialogueTextController textController;
        IAssetProvider<string, Sprite> spriteProvider;

        readonly Dictionary<string, CharacterPortraitController> controllers = new();
        readonly Dictionary<string, StageSlot> characterToSlot = new();
        readonly Dictionary<StageSlot, string> slotToCharacter = new();

        Action<string> onEventTagFiredHandler;
        #endregion

        #region Events
        public event Action<string> OnCharacterEntered;
        public event Action<string> OnCharacterExited;
        public event Action<string, string> OnPoseChanged;
        #endregion

        #region Unity Life Cycle
        private void Awake() {
            if (controllerPrefab == null) HLogger.Error("[CharacterStageDirector] controllerPrefab is not assigned.");
            if (registry == null) HLogger.Error("[CharacterStageDirector] registry is not assigned.");
            if (layout == null) HLogger.Error("[CharacterStageDirector] layout is not assigned.");
            spriteProvider = AssetProviderFactory.CreateAddressable<Sprite>();
        }

        private void OnDestroy() {
            if (textController != null && onEventTagFiredHandler != null) {
                textController.OnEventTagFired -= onEventTagFiredHandler;
            }
            spriteProvider?.ReleaseAll();
        }
        #endregion

        #region Public API
        public void Bind(CharacterRegistrySO reg, StageLayoutSO lay, DialogueTextController ctrl) {
            registry = reg;
            layout = lay;
            if (textController != null && onEventTagFiredHandler != null) {
                textController.OnEventTagFired -= onEventTagFiredHandler;
            }
            textController = ctrl;
            if (textController != null) {
                onEventTagFiredHandler = _OnEventTagFired;
                textController.OnEventTagFired += onEventTagFiredHandler;
            }
        }

        public void EnterLine(LineStageContext ctx) {
            if (string.IsNullOrEmpty(ctx.SpeakerKey)) return;
            if (registry == null || !registry.TryGet(ctx.SpeakerKey, out CharacterPortraitSetSO set)) {
                HLogger.Warning($"[CharacterStageDirector] 레지스트리에 '{ctx.SpeakerKey}' 없음.");
                return;
            }

            bool isOnStage = characterToSlot.ContainsKey(ctx.SpeakerKey);

            StageSlot targetSlot = ctx.SpeakerSlot
                ?? (isOnStage ? characterToSlot[ctx.SpeakerKey] : layout?.DefaultSlot ?? StageSlot.Left);

            if (!isOnStage) {
                _ShowCharacterCore(ctx.SpeakerKey, set, targetSlot, ctx.SpeakerPoseKey,
                    ctx.SpeakerFacing, PortraitTransition.Fade(0.2f));
            } else {
                if (!string.IsNullOrEmpty(ctx.SpeakerPoseKey)) {
                    SetPose(ctx.SpeakerKey, ctx.SpeakerPoseKey, PortraitTransition.Instant);
                }
                if (characterToSlot[ctx.SpeakerKey] != targetSlot) {
                    MoveToSlot(ctx.SpeakerKey, targetSlot, PortraitTransition.Instant);
                }
            }

            if (ctx.AutoHighlightSpeaker) {
                foreach (var (charKey, controller) in controllers) {
                    controller.SetHighlight(charKey == ctx.SpeakerKey);
                }
            }
        }

        public void ShowCharacter(string characterKey, StageSlot slot, string poseKey,
            FacingDirection facing, PortraitTransition transition) {
            if (registry == null || !registry.TryGet(characterKey, out CharacterPortraitSetSO set)) {
                HLogger.Warning($"[CharacterStageDirector] 레지스트리에 '{characterKey}' 없음.");
                return;
            }
            _ShowCharacterCore(characterKey, set, slot, poseKey, facing, transition);
        }

        private void _ShowCharacterCore(string characterKey, CharacterPortraitSetSO set,
            StageSlot slot, string poseKey, FacingDirection facing, PortraitTransition transition) {
            if (layout == null || !layout.TryGet(slot, out SlotConfig slotConfig)) {
                HLogger.Warning($"[CharacterStageDirector] 레이아웃에 슬롯 '{slot}' 없음.");
                return;
            }

            if (slotToCharacter.TryGetValue(slot, out string occupant) && occupant != characterKey) {
                HideCharacter(occupant, PortraitTransition.Fade(0.15f));
            }

            CharacterPortraitController ctrl = _GetOrCreateController(characterKey, set);
            if (ctrl == null) return;

            Transform slotRoot = _GetSlotRoot(slot);
            if (ctrl.transform.parent != slotRoot) ctrl.transform.SetParent(slotRoot, false);

            ctrl.SetSlot(slotConfig);
            ctrl.SetFacing(facing);

            string resolvedPose = !string.IsNullOrEmpty(poseKey) ? poseKey : set.DefaultPoseKey;
            ctrl.SetPose(resolvedPose, PortraitTransition.Instant);
            ctrl.Show(transition);

            characterToSlot[characterKey] = slot;
            slotToCharacter[slot] = characterKey;
            OnCharacterEntered?.Invoke(characterKey);
        }

        public void HideCharacter(string characterKey, PortraitTransition transition) {
            if (!controllers.TryGetValue(characterKey, out CharacterPortraitController ctrl)) return;
            if (characterToSlot.TryGetValue(characterKey, out StageSlot slot)) {
                slotToCharacter.Remove(slot);
                characterToSlot.Remove(characterKey);
            }
            ctrl.Hide(transition);
            OnCharacterExited?.Invoke(characterKey);
        }

        public void SetPose(string characterKey, string poseKey, PortraitTransition transition) {
            if (!controllers.TryGetValue(characterKey, out CharacterPortraitController ctrl)) return;
            ctrl.SetPose(poseKey, transition);
            OnPoseChanged?.Invoke(characterKey, poseKey);
        }

        public void MoveToSlot(string characterKey, StageSlot slot, PortraitTransition transition) {
            if (!controllers.TryGetValue(characterKey, out CharacterPortraitController ctrl)) return;
            if (layout == null || !layout.TryGet(slot, out SlotConfig config)) {
                HLogger.Warning($"[CharacterStageDirector] 슬롯 '{slot}' 없음.");
                return;
            }
            if (characterToSlot.TryGetValue(characterKey, out StageSlot oldSlot)) {
                slotToCharacter.Remove(oldSlot);
            }
            Transform slotRoot = _GetSlotRoot(slot);
            if (ctrl.transform.parent != slotRoot) ctrl.transform.SetParent(slotRoot, false);
            ctrl.SetSlot(config);
            characterToSlot[characterKey] = slot;
            slotToCharacter[slot] = characterKey;
        }

        public void SetFacing(string characterKey, FacingDirection facing) {
            if (controllers.TryGetValue(characterKey, out CharacterPortraitController ctrl)) {
                ctrl.SetFacing(facing);
            }
        }

        public void ClearAll(PortraitTransition transition) {
            foreach (string charKey in new List<string>(characterToSlot.Keys)) {
                HideCharacter(charKey, transition);
            }
        }

        public void ClearAll() => ClearAll(PortraitTransition.Fade(0.2f));

        public async UniTask WaitForActiveTransitionsAsync(CancellationToken ct) {
            while (true) {
                bool anyTransitioning = false;
                foreach (CharacterPortraitController ctrl in controllers.Values) {
                    if (ctrl != null && ctrl.IsTransitioning) {
                        anyTransitioning = true;
                        break;
                    }
                }
                if (!anyTransitioning) return;
                await UniTask.NextFrame(cancellationToken: ct);
            }
        }
        #endregion

        #region Inline Event Handler
        private void _OnEventTagFired(string eventKey) {
            if (!PortraitEventParser.TryParse(eventKey, out PortraitEventInstruction ins)) return;
            _Apply(ins);
        }

        public void ApplyInstruction(PortraitEventInstruction ins) => _Apply(ins);

        private void _Apply(PortraitEventInstruction ins) {
            string charKey = ins.TargetCharacterKey;
            string[] args = ins.Args;

            switch (ins.Verb) {
                case PortraitVerb.Pose:
                    if (args.Length < 1) { HLogger.Warning("[CharacterStageDirector] pose 동사에 포즈키 인자 필요."); return; }
                    SetPose(charKey, args[0], PortraitTransition.Crossfade(0.2f));
                    break;
                case PortraitVerb.Face:
                    if (args.Length < 1) { HLogger.Warning("[CharacterStageDirector] face 동사에 left/right 인자 필요."); return; }
                    if (!_TryParseFacing(args[0], out FacingDirection dir)) {
                        HLogger.Warning($"[CharacterStageDirector] face 인자 '{args[0]}' 는 left/right 여야 함.");
                        return;
                    }
                    SetFacing(charKey, dir);
                    break;
                case PortraitVerb.Slot:
                    if (args.Length < 1 || !_TryParseSlot(args[0], out StageSlot slotDir)) {
                        HLogger.Warning("[CharacterStageDirector] slot 동사에 left/right 인자 필요.");
                        return;
                    }
                    MoveToSlot(charKey, slotDir, PortraitTransition.Instant);
                    break;
                case PortraitVerb.Show: {
                    StageSlot showSlot = (args.Length > 0 && _TryParseSlot(args[0], out StageSlot parsedShow))
                        ? parsedShow
                        : layout?.DefaultSlot ?? StageSlot.Left;
                    string poseKey = args.Length > 1 ? args[1] : string.Empty;
                    FacingDirection facing = FacingDirection.Right;
                    if (args.Length > 2 && _TryParseFacing(args[2], out FacingDirection parsedFacing)) {
                        facing = parsedFacing;
                    } else if (registry != null && registry.TryGet(charKey, out CharacterPortraitSetSO s)) {
                        facing = s.DefaultFacing;
                        _ShowCharacterCore(charKey, s, showSlot, poseKey, facing, PortraitTransition.Fade(0.2f));
                        break;
                    }
                    ShowCharacter(charKey, showSlot, poseKey, facing, PortraitTransition.Fade(0.2f));
                    break;
                }
                case PortraitVerb.Hide:
                    HideCharacter(charKey, PortraitTransition.Fade(0.2f));
                    break;
                case PortraitVerb.Shake:
                    if (controllers.TryGetValue(charKey, out CharacterPortraitController shakeCtrl))
                        shakeCtrl.Shake();
                    break;
                case PortraitVerb.Bounce:
                    if (controllers.TryGetValue(charKey, out CharacterPortraitController bounceCtrl))
                        bounceCtrl.Bounce();
                    break;
                default:
                    HLogger.Warning($"[CharacterStageDirector] 알 수 없는 PortraitVerb: {ins.Verb}.");
                    break;
            }
        }
        #endregion

        #region Private Helpers
        private CharacterPortraitController _GetOrCreateController(string characterKey, CharacterPortraitSetSO set) {
            if (!controllers.TryGetValue(characterKey, out CharacterPortraitController ctrl)) {
                if (controllerPrefab == null) {
                    HLogger.Error("[CharacterStageDirector] controllerPrefab 이 연결되지 않았습니다.");
                    return null;
                }
                ctrl = Instantiate(controllerPrefab, transform);
                ctrl.gameObject.name = $"Portrait_{characterKey}";
                controllers[characterKey] = ctrl;
                PortraitHighlightStyle style = layout != null ? layout.HighlightStyle : PortraitHighlightStyle.Default;
                ctrl.Bind(set, style);
                ctrl.BindProvider(spriteProvider);
            } else {
                ctrl.gameObject.SetActive(true);
            }
            return ctrl;
        }

        private Transform _GetSlotRoot(StageSlot slot) {
            if (slot == StageSlot.Right) {
                if (rightSlotRoot == null) HLogger.Warning("[CharacterStageDirector] rightSlotRoot 미연결 — this.transform 사용.");
                return rightSlotRoot != null ? rightSlotRoot : transform;
            }
            if (leftSlotRoot == null) HLogger.Warning("[CharacterStageDirector] leftSlotRoot 미연결 — this.transform 사용.");
            return leftSlotRoot != null ? leftSlotRoot : transform;
        }

        private static bool _TryParseFacing(string raw, out FacingDirection dir) {
            if (raw.Equals("left", StringComparison.OrdinalIgnoreCase)) { dir = FacingDirection.Left; return true; }
            if (raw.Equals("right", StringComparison.OrdinalIgnoreCase)) { dir = FacingDirection.Right; return true; }
            dir = default;
            return false;
        }

        private static bool _TryParseSlot(string raw, out StageSlot slot) {
            if (raw.Equals("left", StringComparison.OrdinalIgnoreCase)) { slot = StageSlot.Left; return true; }
            if (raw.Equals("right", StringComparison.OrdinalIgnoreCase)) { slot = StageSlot.Right; return true; }
            slot = default;
            return false;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: Addressable spriteProvider 생성 + Controller 주입
 *
 * # 변경
 * - IAssetProvider<string, Sprite> spriteProvider 필드 추가
 * - Awake: AssetProviderFactory.CreateAddressable<Sprite>() 로 spriteProvider 생성
 * - OnDestroy: spriteProvider?.ReleaseAll() 추가 — Addressable 핸들 일괄 해제
 * - _GetOrCreateController: 신규 컨트롤러 생성 시 ctrl.BindProvider(spriteProvider) 호출
 *
 * # 이유
 * - PortraitPose.SpriteKey 전환으로 Controller가 IAssetProvider 의존성 필요
 * - Provider 수명: Awake 생성 + OnDestroy 해제로 StageDirector 수명과 일치
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: HideCharacter — SetActive 비활성화를 Controller에 위임
 *
 * # 변경
 * - HideCharacter(): ctrl.Hide(transition) 뒤의 `if (!ctrl.IsVisible) ctrl.gameObject.SetActive(false)` 제거.
 *   관련 풀링 의도 주석 3줄 제거.
 *
 * # 이유
 * - AgentReview Warning #8 (2026-05-17 19:13:03).
 * - CharacterPortraitController.Hide()/_HideAsync()가 직접 SetActive(false)를 처리하도록 변경됨.
 *   기존 구조: Instant=즉시 비활성화, Fade=미비활성화(IsVisible 아직 true) — 두 경로 불일치.
 *   Controller 내부에서 통일 처리. StageDirector의 중복 체크 불필요.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: FacingDirection → StageSlot 슬롯 위치 타입 분리
 *
 * # 변경
 * - Dictionary<string, FacingDirection> characterToSlot → Dictionary<string, StageSlot>.
 * - Dictionary<FacingDirection, string> slotToCharacter → Dictionary<StageSlot, string>.
 * - ShowCharacter / _ShowCharacterCore / MoveToSlot slot 파라미터 → StageSlot.
 * - HideCharacter / EnterLine 내부 out var → StageSlot.
 * - _GetSlotRoot(FacingDirection) → _GetSlotRoot(StageSlot).
 * - _TryParseSlot(string, out StageSlot) 헬퍼 추가 — Slot/Show 동사 전용.
 * - Slot 동사: _TryParseFacing → _TryParseSlot + FacingDirection slotDir → StageSlot slotDir.
 * - Show 동사: _TryParseFacing(슬롯 파싱) → _TryParseSlot. FacingDirection.Left fallback → StageSlot.Left.
 * - Face 동사 / parsedFacing: FacingDirection 유지 (방향 개념).
 *
 * # 이유
 * - AgentReview Warning #7. _TryParseFacing이 "방향"과 "슬롯 위치" 두 개념 모두에 재사용 —
 *   컴파일 타임에 의미 혼동을 차단하려면 타입 분리 필수.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: 필수 SerializeField Assert 추가
 *
 * # 변경
 * - Awake 신설. controllerPrefab / registry / layout 세 필드에 Debug.Assert 추가.
 * - leftSlotRoot / rightSlotRoot는 선택 연결 (null 시 this.transform 폴백) → Assert 제외.
 *
 * # 이유
 * - AgentReview Warning #6. 세 필드 null 시 EnterLine/ShowCharacter에서 즉시 실패.
 *   Awake Assert로 씬 로드 시점에 사전 차단.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: 슬롯 루트 피벗 기준 (0,0) 생성 구조로 전환
 *
 * # 변경
 * - _GetOrCreateController: 임시 부모를 leftSlotRoot → this.transform 으로 변경.
 *   (생성 직후 _ShowCharacterCore 에서 SetParent 로 정확한 루트로 이동하므로 임시 부모 무관)
 * - _GetSlotRoot: leftSlotRoot / rightSlotRoot null 시 HLogger.Warning 추가.
 * - DialogueStageLayout.asset: left/right 슬롯 AnchorPos (-350/350,0) → (0,0).
 *   슬롯 루트 RectTransform: full-stretch → anchor(0.5,0.5) + anchoredPosition(-350/350,0).
 *
 * # 이유
 * - 컨트롤러가 슬롯 루트 기준 (0,0,0)에 배치되어야 씬에서 루트 위치를 이동하면 캐릭터 위치가
 *   직관적으로 바뀌는 구조. 기존 AnchorPos 오프셋 방식은 루트 위치와 오프셋이 중첩되어 혼란.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: stageRoot → leftSlotRoot/rightSlotRoot 분리 + HTitle 그룹 세분화 + IMGUI 시각화
 *
 * # 변경
 * - `Transform stageRoot` → `Transform leftSlotRoot` + `Transform rightSlotRoot`.
 * - HTitle 그룹: "References" 단일 → "Controller" / "Stage Roots" / "Character Data" 세분화.
 * - `_GetSlotRoot(slotKey)` 헬퍼 추가: slotKey에 "right" 포함 시 rightSlotRoot, 그 외 leftSlotRoot 반환.
 * - `_ShowCharacterCore` / `MoveToSlot`: SetSlot 전에 `_GetSlotRoot`로 결정된 루트로 `SetParent(root, false)`.
 * - `_GetOrCreateController`: 생성 시 기본 부모를 leftSlotRoot 사용 (즉시 SetParent로 덮어씀).
 *
 * # 이유
 * - 단일 stageRoot 기준 AnchorPos 오프셋 배치 → 의도치 않은 위치 이슈.
 * - leftSlotRoot/rightSlotRoot가 씬에서 직접 확인 가능한 정확한 피벗 역할.
 * - Inspector 그룹 세분화로 각 필드 용도를 한 눈에 파악 가능.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: Warning 보강 — ShowCharacterCore 분리·풀링 명시·default·facing·WaitForTransitions
 *
 * # 변경
 * - ShowCharacter → _ShowCharacterCore 분리: public 경계의 registry.TryGet과 내부 로직 분리.
 *   EnterLine(_isOnStage=false), Show verb(_Apply) 모두 _ShowCharacterCore 직접 호출로 이중 TryGet 제거.
 * - _Apply Show case: args[2] facing 파싱 지원 + 미지정 시 set.DefaultFacing 사용.
 * - _Apply switch: default case 추가 (HLogger.Warning).
 * - HideCharacter: Instant 전환 후 IsVisible=false 시 SetActive(false). 풀링 의도 주석 명시.
 * - _GetOrCreateController: 기존 컨트롤러 재사용 시 SetActive(true) — HideCharacter로 비활성화된 풀 객체 복원.
 * - WaitForActiveTransitionsAsync 신규 추가: controllers 순회로 IsTransitioning 감시. CinematicNode용.
 * - using Cysharp.Threading.Tasks / System.Threading 추가.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: _GetOrCreateController — Bind 신규 생성 시에만 호출
 *
 * # 변경
 * - _GetOrCreateController: ctrl.Bind(set, style) 호출을 신규 컨트롤러 생성 분기 내부로 이동.
 *   기존 컨트롤러 반환 경로에서는 Bind 재호출 없음.
 *
 * # 이유
 * - ShowCharacter 재호출 시(같은 캐릭터 재등장 등) Bind가 재실행되면 _ApplyPoseImmediate가
 *   DefaultPoseKey로 포즈를 초기화한다. 직후 SetPose로 복원되지만 Animated→Static 전환 시
 *   1프레임 플래시 발생 가능. Bind는 초기화 전용, 포즈 관리는 ShowCharacter/SetPose 책임.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: OnDestroy — Unity Life Cycle region으로 이동
 *
 * # 변경
 * - OnDestroy : #region Private Helpers → 신규 #region Unity Life Cycle로 이동
 *   (Events region 바로 아래, Public API 위에 삽입)
 *
 * # 이유
 * - OnDestroy는 Unity 라이프사이클 메서드이므로 Private Helpers가 아닌
 *   Unity Life Cycle region에 위치해야 독자가 생명주기 흐름을 단일 region에서 파악 가능.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: 필드 언더바 제거 + ShowCharacter null guard
 *
 * # 변경
 * - controllers / characterToSlot / slotToCharacter / onEventTagFiredHandler :
 *   _접두 제거 (CLAUDE.md: 언더바는 private 함수/Getter/Property 전용)
 * - ShowCharacter: _GetOrCreateController 반환값 null 가드 추가
 *   (controllerPrefab 미연결 시 즉시 NullReferenceException → return으로 안전 처리)
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 CharacterStageDirector 전체 구현 (스텁 교체)
 *
 * # 목적
 * - HCUP-2.3.0 Phase 4-D/E — 포트레이트 오케스트레이션 + 인라인 이벤트 처리 완성
 *
 * # 상태 관리
 * - controllers:      characterKey → Controller (alive 모든 컨트롤러)
 * - characterToSlot:  characterKey → slotKey    (현재 스테이지에 있는 캐릭터만)
 * - slotToCharacter:  slotKey → characterKey    (역방향 조회용)
 *
 * # EnterLine 5단계
 * 1. Registry 확인  2. 스테이지 없으면 ShowCharacter  3. 포즈 적용
 * 4. 슬롯 이동      5. AutoHighlight 처리
 *
 * # 이전 스텁
 * - 2026-05-15: Phase 2 컴파일 선행 조건으로 EnterLine(){} / ClearAll(){} 스텁 생성
 * - 이번 커밋: Phase 4 전체 구현으로 스텁 교체
 *
 * =============================================================================
 */
#endif
