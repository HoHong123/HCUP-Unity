#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- HCUP-2.2.0 대화 텍스트 타이프라이터 컨트롤러 (Phase 2~4).
 *
 * 특징 / 지원기능 ::
 * + PlayLine(DialogueLine) : 토큰 순회 → maxVisibleCharacters 점진 증가 타이프라이터
 * + SkipToEnd()            : Typing/Paused → 즉시 완료, Skipped 전이
 * + Pause() / Resume()     : 외부 강제 일시정지/재개
 * + SetSpeedMode()         : Slow / Normal / Fast / Instant 전환
 * + SetHoldAccelerate()    : 키 홀드 시 0.5배 추가 가속
 * + 5종 이벤트             : OnLineStart / OnLineComplete / OnCharPrinted /
 *                            OnEventTagFired / OnAdvanceRequested
 * + TextEffectHandler 연결 : EffectPush/Pop 범위 → vertex 효과 위임 (Phase 3)
 * + DialogueBlipSfxAgent   : 글자별 블립 재생 / silent 억제 / voice 교체 (Phase 4)
 *
 * 주의사항 ::
 * tmpText 필수 연결 — Awake Debug.Assert 로 검증.
 * EffectPush/Pop → TextEffectHandler 위임. Sfx 미구현(no-op) — Validator 경고로 명시.
 * Instant 모드에서는 Pause 지연이 0. Event 태그는 모드 무관 항상 발화.
 * PlayLine 직후 INPUT_GUARD_DURATION(50ms) 동안 SkipToEnd 무시.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using HInspector;

namespace HDialogue {
    public sealed class DialogueTextController : MonoBehaviour {
        #region Fields
        [HTitle("References")]
        [SerializeField]
        TMP_Text tmpText;

        [HTitle("Effect")]
        [SerializeField]
        TextEffectHandler textEffectHandler;

        [HTitle("Blip")]
        [SerializeField]
        DialogueBlipSfxAgent blipAgent;

        TextDisplayState state = TextDisplayState.Idle;
        TextSpeedMode speedMode = TextSpeedMode.Normal;
        float inlineSpeedMultiplier = 1f;
        bool isSilent;
        bool isHoldAccelerate;
        float playStartTime;

        IReadOnlyList<DialogueToken> currentTokens;
        DialogueLine currentLine;

        CancellationTokenSource playCts;
        #endregion

        #region Events
        public event Action<DialogueLine> OnLineStart;
        public event Action OnLineComplete;
        public event Action<char> OnCharPrinted;
        public event Action<string> OnEventTagFired;
        public event Action OnAdvanceRequested;
        #endregion

        #region Properties
        public TextDisplayState State => state;
        public bool IsTyping => state == TextDisplayState.Typing;
        public bool IsWaiting => state == TextDisplayState.Waiting;
        #endregion

        #region Unity Life Cycle
        private void Awake() {
            Debug.Assert(tmpText != null, "[DialogueTextController] tmpText is not assigned.");
        }

        private void OnDestroy() {
            _CancelPlay();
        }
        #endregion

        #region Public
        public void PlayLine(DialogueLine line) {
            _CancelPlay();
            playCts = new CancellationTokenSource();

            currentLine = line;
            currentTokens = DialogueTagParser.Parse(line.RawText);
            textEffectHandler?.SetEffectRanges(_BuildEffectRanges(currentTokens));
            inlineSpeedMultiplier = 1f;
            isSilent = false;
            blipAgent?.ResetVoice(line.OverrideBlipToken);
            playStartTime = Time.unscaledTime;

            _SetState(TextDisplayState.Typing);
            OnLineStart?.Invoke(line);
            _PlayLineAsync(playCts.Token).Forget();
        }

        public void Clear() {
            _CancelPlay();
            if (tmpText != null) {
                tmpText.text = string.Empty;
                tmpText.maxVisibleCharacters = 0;
            }
            textEffectHandler?.ClearEffects();
            _SetState(TextDisplayState.Idle);
        }

        public void SkipToEnd() {
            if (state != TextDisplayState.Typing && state != TextDisplayState.Paused) return;
            if (!_IsInputGuardPassed()) return;

            _CancelPlay();
            if (tmpText != null) tmpText.maxVisibleCharacters = int.MaxValue;
            _SetState(TextDisplayState.Skipped);
            OnLineComplete?.Invoke();
        }

        public void Pause() {
            if (state != TextDisplayState.Typing) return;
            _SetState(TextDisplayState.Paused);
        }

        public void Resume() {
            if (state != TextDisplayState.Paused) return;
            _SetState(TextDisplayState.Typing);
        }

        public void SetSpeedMode(TextSpeedMode mode) {
            speedMode = mode;
        }

        public void SetHoldAccelerate(bool enabled) {
            isHoldAccelerate = enabled;
        }

        public void RequestAdvance() {
            OnAdvanceRequested?.Invoke();
        }

        /// <summary> INPUT_GUARD 없이 현재 라인을 즉시 완료. 스킵은 런타임 사양이므로 플레이어 빌드에도 포함된다. </summary>
        public void ForceSkipToEnd() {
            if (state == TextDisplayState.Idle
                || state == TextDisplayState.Waiting
                || state == TextDisplayState.Skipped) return;
            _CancelPlay();
            if (tmpText != null) tmpText.maxVisibleCharacters = int.MaxValue;
            _SetState(TextDisplayState.Skipped);
            OnLineComplete?.Invoke();
        }
        #endregion

        #region Private — 타이프라이터
        private async UniTaskVoid _PlayLineAsync(CancellationToken token) {
            if (string.IsNullOrEmpty(currentLine.RawText)) {
                await UniTask.NextFrame(cancellationToken: token);
                if (token.IsCancellationRequested) return;
                _SetState(TextDisplayState.Waiting);
                OnLineComplete?.Invoke();
                return;
            }

            if (tmpText != null) {
                tmpText.text = _BuildDisplayText(currentTokens);
                tmpText.maxVisibleCharacters = 0;
            }

            for (int k = 0; k < currentTokens.Count; k++) {
                if (token.IsCancellationRequested) return;

                while (state == TextDisplayState.Paused) {
                    await UniTask.NextFrame(cancellationToken: token);
                    if (token.IsCancellationRequested) return;
                }

                await _ProcessTokenAsync(currentTokens[k], token);
                if (token.IsCancellationRequested) return;
            }

            if (tmpText != null) tmpText.maxVisibleCharacters = int.MaxValue;
            _SetState(TextDisplayState.Waiting);
            OnLineComplete?.Invoke();
        }

        private async UniTask _ProcessTokenAsync(DialogueToken t, CancellationToken token) {
            switch (t.Type) {
                case DialogueTokenType.Char: {
                    if (tmpText != null) tmpText.maxVisibleCharacters++;
                    if (!isSilent) blipAgent?.PlayBlip();
                    OnCharPrinted?.Invoke(t.Character);
                    float delay = _CalcCharDelay(t.Character);
                    if (delay > 0f)
                        await UniTask.WaitForSeconds(delay, ignoreTimeScale: true, cancellationToken: token);
                    break;
                }
                case DialogueTokenType.Pause:
                    if (speedMode != TextSpeedMode.Instant)
                        await UniTask.WaitForSeconds(t.NumericArg, ignoreTimeScale: true, cancellationToken: token);
                    break;

                case DialogueTokenType.SpeedSet:
                    inlineSpeedMultiplier = t.NumericArg;
                    break;

                case DialogueTokenType.SpeedReset:
                    inlineSpeedMultiplier = 1f;
                    break;

                case DialogueTokenType.SilentPush:
                    isSilent = true;
                    break;

                case DialogueTokenType.SilentPop:
                    isSilent = false;
                    break;

                case DialogueTokenType.Event:
                    OnEventTagFired?.Invoke(t.StringArg);
                    break;

                case DialogueTokenType.VoiceSet:
                    blipAgent?.SetVoice(t.StringArg);
                    break;

                // _BuildEffectRanges / _BuildDisplayText 에서 사전 처리됨
                case DialogueTokenType.EffectPush:
                case DialogueTokenType.EffectPop:
                case DialogueTokenType.PassThrough:
                    break;

                // 미구현 — Phase 5+ 구현 예정. Validator에서 no-op 경고 발행.
                case DialogueTokenType.Sfx:
                    break;
            }
        }

        private float _CalcCharDelay(char c) {
            if (speedMode == TextSpeedMode.Instant) return 0f;
            float holdMult = isHoldAccelerate ? TextSpeedConstants.HOLD_SPEED_MULTIPLIER : 1f;
            return (_GetBaseInterval() + _GetPunctuationDelay(c)) * holdMult;
        }

        private float _GetBaseInterval() {
            float baseInterval = speedMode switch {
                TextSpeedMode.Slow => TextSpeedConstants.BASE_INTERVAL_SLOW,
                TextSpeedMode.Fast => TextSpeedConstants.BASE_INTERVAL_FAST,
                TextSpeedMode.Instant => TextSpeedConstants.BASE_INTERVAL_INSTANT,
                _ => TextSpeedConstants.BASE_INTERVAL_NORMAL,
            };
            // 스펙: BaseInterval × LineSpeedMultiplier ÷ InlineSpeedMultiplier
            return baseInterval * currentLine.SpeedMultiplier / inlineSpeedMultiplier;
        }

        private static float _GetPunctuationDelay(char c) {
            return c switch {
                '.' or '!' or '?' => TextSpeedConstants.PUNCT_DELAY_SENTENCE,
                ',' => TextSpeedConstants.PUNCT_DELAY_COMMA,
                '\n' => TextSpeedConstants.PUNCT_DELAY_NEWLINE,
                _ => 0f,
            };
        }

        private static string _BuildDisplayText(IReadOnlyList<DialogueToken> tokens) {
            var sb = new StringBuilder();
            for (int k = 0; k < tokens.Count; k++) {
                DialogueToken t = tokens[k];
                switch (t.Type) {
                    case DialogueTokenType.Char: sb.Append(t.Character); break;
                    case DialogueTokenType.PassThrough: sb.Append(t.StringArg); break;
                }
            }
            return sb.ToString();
        }

        private static IReadOnlyList<TextEffectRange> _BuildEffectRanges(IReadOnlyList<DialogueToken> tokens) {
            var ranges = new List<TextEffectRange>();
            var stack = new Stack<(int startIdx, string effectName)>();
            int charIdx = 0;

            for (int k = 0; k < tokens.Count; k++) {
                DialogueToken t = tokens[k];
                switch (t.Type) {
                    case DialogueTokenType.Char:
                        charIdx++;
                        break;
                    case DialogueTokenType.EffectPush:
                        stack.Push((charIdx, t.StringArg));
                        break;
                    case DialogueTokenType.EffectPop:
                        if (stack.Count > 0) {
                            (int startIdx, string effectName) = stack.Pop();
                            ranges.Add(new TextEffectRange(startIdx, charIdx, effectName));
                        }
                        break;
                }
            }

            return ranges;
        }

        private void _SetState(TextDisplayState newState) {
            state = newState;
        }

        private void _CancelPlay() {
            playCts?.Cancel();
            playCts?.Dispose();
            playCts = null;
        }

        private bool _IsInputGuardPassed() =>
            Time.unscaledTime - playStartTime >= TextSpeedConstants.INPUT_GUARD_DURATION;
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: Sfx 토큰 case 분리 — 미구현 의도 명확화
 *
 * # 변경
 * - _ProcessTokenAsync: `EffectPush/EffectPop/Sfx/PassThrough` 합산 case 분리.
 *   EffectPush/EffectPop/PassThrough: "_BuildEffectRanges/_BuildDisplayText에서 사전 처리됨" 주석.
 *   Sfx: 별도 case + "미구현 — Phase 5+ 구현 예정. Validator에서 no-op 경고 발행" 주석.
 *
 * # 이유
 * - AgentReview Warning #9 (2026-05-17 19:13:03).
 * - 셋은 "다른 경로에서 이미 처리됨"이고 Sfx는 "아무 곳에서도 처리 안 됨" — 의미가 다름.
 *   같은 case에 묶으면 "Sfx도 처리됐다"는 오독 가능성.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: tmpText 필수 Assert 추가
 *
 * # 변경
 * - Awake 신설. Debug.Assert(tmpText != null, ...) 추가.
 *
 * # 이유
 * - AgentReview Warning #6. tmpText null 시 PlayLine에서 NRE 직행. Awake Assert로 사전 차단.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: Instant 모드 Event 토큰 발화 복원
 *
 * # 변경
 * - _ProcessTokenAsync Event case: `if (speedMode != Instant)` 조건 제거 → 항상 발화
 *
 * # 이유
 * - Instant 모드(전체 스킵)에서 portrait.* 이벤트가 누락되어 스테이지가 마지막 라인
 *   상태로 굳어버리는 버그. Pause는 시간 지연이므로 스킵이 맞고, Event는 액션이므로
 *   모드 무관하게 발화해야 함.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 HUI.TextUI → HDialogue 패키지 이관
 *
 * # 변경
 * - namespace HUI.TextUI → HDialogue
 * - HUI/Runtime/HUI/Text/Dialogue/ → HDialogue/Runtime/ 이관
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueTextController Phase 4 연결 (DialogueBlipSfxAgent B안)
 *
 * # 변경
 * - AudioClip currentBlipClip 필드 제거 (clip 직접 참조 폐기)
 * - PlayLine(): blipAgent?.ResetVoice(line.OverrideBlipToken) 으로 변경
 * - Char 토큰: blipAgent?.PlayBlip() (인자 없음 — token은 agent 내부 관리)
 * - VoiceSet 토큰: blipAgent?.SetVoice(t.StringArg) 으로 변경
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueTextController Phase 3 연결 (TextEffectHandler)
 *
 * # 변경
 * - [SerializeField] TextEffectHandler textEffectHandler 필드 추가
 * - PlayLine(): _BuildEffectRanges(currentTokens) → textEffectHandler?.SetEffectRanges()
 * - Clear(): textEffectHandler?.ClearEffects()
 * - _BuildEffectRanges() 정적 메서드 추가 (EffectPush/Pop 인덱스 기반 범위 계산)
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueTextController Phase 2 구현
 *
 * # 목적
 * - DialogueLine → maxVisibleCharacters 기반 타이프라이터 재생
 * - Phase 1 DialogueTagParser 출력을 실제 TMP 출력으로 연결하는 핵심 컴포넌트
 *
 * # 설계 결정
 * - displayText = Char + PassThrough 토큰만 : 커스텀 태그는 동작 전용, TMP에 노출 X.
 * - UniTask + CancellationToken : PlayLine 재호출 시 이전 재생 즉시 취소. OnDestroy 보장.
 * - Instant 모드 : Char 지연 0, Pause 0, Event 항상 발화 (← 2026-05-17 수정, 아래 참조).
 *
 * =============================================================================
 */
#endif
