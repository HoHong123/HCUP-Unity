#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 슬롯 단위 캐릭터 포트레이트 렌더/트랜지션 MonoBehaviour.
 *
 * 특징 / 지원기능 ::
 * + Bind(set)             - 캐릭터 포트레이트 집합 연결
 * + BindProvider(p)       - IAssetProvider<string, Sprite> 주입 (SpriteKey 로드 전제)
 * + SetSlot(config)        - RectTransform 슬롯 이동 + 정렬 순서
 * + SetPose(key, t)       - SpriteKey Addressable 비동기 로드 후 교체 또는 Animator.Play(ClipKey)
 * + SetFacing(dir)         - localScale.x 부호 반전으로 좌우 플립
 * + SetHighlight(bool)    - 화자/비화자 틴트(RGB)·스케일 트랜지션
 * + Show / Hide           - Fade/SlideIn 등 트랜지션 표시·숨김
 * + OnTransitionComplete  - 트랜지션 완료 시 발행
 *
 * 주의사항 ::
 * BindProvider 미주입 시 Static 포즈 스프라이트 표시 안 됨 (spriteProvider == null 무시).
 * image 필수 연결 — Awake Debug.Assert 로 검증.
 * 동시 트랜지션: Appearance(alpha+pose) / Highlight(RGB tint+scale) / Motion 채널 독립 관리.
 * 같은 채널 내 신규 호출은 기존 취소 → 새 트랜지션 시작. 다른 채널은 간섭 없음.
 * anchoredPosition = currentSlot.AnchorPos + currentPoseOffset + motionOffset 삼분리 구조.
 *   Motion 취소/종료 시 finally에서 motionOffset = 0 복구 — SetSlot/SetPose와 충돌 없음.
 * Show: gameObject.SetActive(true) — 비활성화된 풀 컨트롤러 재등장 대응.
 * Hide: 완료 시 gameObject.SetActive(false) — IsVisible=false와 동시 설정.
 * CharacterStageDirector 가 풀/인스턴스화 후 Bind + BindProvider + SetSlot 순서 보장 필요.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using HDiagnosis.Logger;
using HInspector;
using HUtil.AssetHandler.Data;
using HUtil.AssetHandler.Provider;
using UnityEngine;
using UnityEngine.UI;

namespace HDialogue {
    [RequireComponent(typeof(RectTransform))]
    public sealed class CharacterPortraitController : MonoBehaviour {
        enum TransitionChannel {
            Appearance,
            Highlight,
            Motion
        }

        #region Fields
        const float SHAKE_DURATION = 0.4f;
        const float SHAKE_MAGNITUDE = 8f;
        const float BOUNCE_DURATION = 0.3f;
        const float BOUNCE_HEIGHT = 12f;

        [HTitle("Renderer")]
        [SerializeField]
        Image image;
        [SerializeField]
        Animator animator;

        IAssetProvider<string, Sprite> spriteProvider;
        CharacterPortraitSetSO portraitSet;
        SlotConfig currentSlot;
        PortraitHighlightStyle highlightStyle;
        FacingDirection currentFacing;
        float baseScale = 1f;
        float highlightScaleMultiplier = 1f;
        Vector2 currentPoseOffset;
        Vector2 motionOffset;

        readonly Dictionary<TransitionChannel, CancellationTokenSource> ctsByChannel = new();
        #endregion

        #region Properties
        public string CurrentCharacterKey => portraitSet != null ? portraitSet.CharacterKey : string.Empty;
        public StageSlot CurrentSlot => currentSlot.Slot;
        public string CurrentPoseKey { get; private set; }
        public FacingDirection CurrentFacing => currentFacing;
        public bool IsVisible { get; private set; }
        public bool IsTransitioning => ctsByChannel.Count > 0;
        #endregion

        #region Events
        public event Action OnTransitionComplete;
        #endregion

        #region Unity Lifecycle
        private void Awake() {
            if (image == null) HLogger.Error("[CharacterPortraitController] image is not assigned.");
            _SetAlpha(0f);
            if (image != null) image.enabled = false;
            IsVisible = false;
            currentPoseOffset = Vector2.zero;
            motionOffset = Vector2.zero;
        }

        private void OnDestroy() { _Cancel(TransitionChannel.Appearance); _Cancel(TransitionChannel.Highlight); _Cancel(TransitionChannel.Motion); }
        #endregion

        #region Public API
        public void BindProvider(IAssetProvider<string, Sprite> provider) {
            spriteProvider = provider;
        }

        public void Bind(CharacterPortraitSetSO set, PortraitHighlightStyle style) {
            portraitSet = set;
            highlightStyle = style;
            currentFacing = set != null ? set.DefaultFacing : FacingDirection.Right;
            if (set != null && set.TryGetPose(set.DefaultPoseKey, out PortraitPose pose)) {
                _ApplyPoseImmediate(pose);
                CurrentPoseKey = pose.Key;
            }
        }

        public void SetSlot(SlotConfig config) {
            currentSlot = config;
            baseScale = config.Scale > 0f ? config.Scale : 1f;
            var rt = (RectTransform)transform;
            _ApplyAnchoredPosition();
            rt.anchorMin = config.AnchorMin;
            rt.anchorMax = config.AnchorMax;
            _ApplyScale();
            if (TryGetComponent(out Canvas canvas)) canvas.sortingOrder = config.SortingOrder;
        }

        public void SetPose(string poseKey, PortraitTransition transition) {
            if (portraitSet == null) return;
            if (!portraitSet.TryGetPose(poseKey, out PortraitPose pose)) {
                HLogger.Warning($"[CharacterPortraitController] '{CurrentCharacterKey}' 에 포즈 '{poseKey}' 없음.", gameObject);
                return;
            }
            CurrentPoseKey = poseKey;
            if (transition.Type == PortraitTransitionType.Instant || transition.Duration <= 0f) {
                _ApplyPoseImmediate(pose);
                return;
            }
            _Cancel(TransitionChannel.Appearance);
            CancellationToken ct = _RegisterToken(TransitionChannel.Appearance);
            _SetPoseAsync(pose, transition, ct).Forget();
        }

        public void SetFacing(FacingDirection facing) {
            currentFacing = facing;
            _ApplyScale();
        }

        public void SetHighlight(bool isSpeaking) {
            _Cancel(TransitionChannel.Highlight);
            CancellationToken ct = _RegisterToken(TransitionChannel.Highlight);
            _HighlightAsync(isSpeaking, ct).Forget();
        }

        public void Show(PortraitTransition transition) {
            gameObject.SetActive(true);
            if (image != null) image.enabled = true;
            if (transition.Type == PortraitTransitionType.Instant || transition.Duration <= 0f) {
                _SetAlpha(1f);
                IsVisible = true;
                OnTransitionComplete?.Invoke();
                return;
            }
            _Cancel(TransitionChannel.Appearance);
            CancellationToken ct = _RegisterToken(TransitionChannel.Appearance);
            _ShowAsync(transition, ct).Forget();
        }

        public void Shake() {
            _Cancel(TransitionChannel.Motion);
            CancellationToken ct = _RegisterToken(TransitionChannel.Motion);
            _ShakeAsync(ct).Forget();
        }

        public void Bounce() {
            _Cancel(TransitionChannel.Motion);
            CancellationToken ct = _RegisterToken(TransitionChannel.Motion);
            _BounceAsync(ct).Forget();
        }

        public void Hide(PortraitTransition transition) {
            if (transition.Type == PortraitTransitionType.Instant || transition.Duration <= 0f) {
                _SetAlpha(0f);
                if (image != null) image.enabled = false;
                gameObject.SetActive(false);
                IsVisible = false;
                OnTransitionComplete?.Invoke();
                return;
            }
            _Cancel(TransitionChannel.Appearance);
            CancellationToken ct = _RegisterToken(TransitionChannel.Appearance);
            _HideAsync(transition, ct).Forget();
        }
        #endregion

        #region Transition Async
        private async UniTaskVoid _ShowAsync(PortraitTransition t, CancellationToken ct) {
            try {
                _SetAlpha(0f);
                await _LerpAlphaAsync(0f, 1f, t, ct);
                IsVisible = true;
                OnTransitionComplete?.Invoke();
            } catch (OperationCanceledException) { }
            finally { _Cleanup(TransitionChannel.Appearance, ct); }
        }

        private async UniTaskVoid _HideAsync(PortraitTransition t, CancellationToken ct) {
            try {
                await _LerpAlphaAsync(_GetAlpha(), 0f, t, ct);
                if (image != null) image.enabled = false;
                gameObject.SetActive(false);
                IsVisible = false;
                OnTransitionComplete?.Invoke();
            } catch (OperationCanceledException) { }
            finally { _Cleanup(TransitionChannel.Appearance, ct); }
        }

        private async UniTaskVoid _SetPoseAsync(PortraitPose pose, PortraitTransition t, CancellationToken ct) {
            try {
                if (t.Type == PortraitTransitionType.Crossfade) {
                    await _LerpAlphaAsync(_GetAlpha(), 0f, PortraitTransition.Fade(t.Duration * 0.5f), ct);
                    _ApplyPoseImmediate(pose);
                    await _LerpAlphaAsync(0f, 1f, PortraitTransition.Fade(t.Duration * 0.5f), ct);
                } else {
                    _ApplyPoseImmediate(pose);
                }
                OnTransitionComplete?.Invoke();
            } catch (OperationCanceledException) { }
            finally { _Cleanup(TransitionChannel.Appearance, ct); }
        }

        private async UniTaskVoid _HighlightAsync(bool isSpeaking, CancellationToken ct) {
            try {
                Color targetTint = isSpeaking ? highlightStyle.SpeakerTint : highlightStyle.NonSpeakerTint;
                float targetMultiplier = isSpeaking ? highlightStyle.SpeakerScale : highlightStyle.NonSpeakerScale;
                float duration = highlightStyle.TransitionDuration;
                if (duration <= 0f) {
                    if (image != null) {
                        Color c = image.color;
                        image.color = new Color(targetTint.r, targetTint.g, targetTint.b, c.a);
                    }
                    highlightScaleMultiplier = targetMultiplier;
                    _ApplyScale();
                    _Cleanup(TransitionChannel.Highlight, ct);
                    return;
                }
                Color startTint = image != null ? image.color : Color.white;
                float startMultiplier = highlightScaleMultiplier;
                float elapsed = 0f;
                while (elapsed < duration) {
                    float t = elapsed / duration;
                    if (image != null) {
                        Color lerped = Color.Lerp(startTint, targetTint, t);
                        Color cur = image.color;
                        image.color = new Color(lerped.r, lerped.g, lerped.b, cur.a);
                    }
                    highlightScaleMultiplier = Mathf.Lerp(startMultiplier, targetMultiplier, t);
                    _ApplyScale();
                    elapsed += Time.deltaTime;
                    await UniTask.NextFrame(ct);
                }
                if (image != null) {
                    Color c = image.color;
                    image.color = new Color(targetTint.r, targetTint.g, targetTint.b, c.a);
                }
                highlightScaleMultiplier = targetMultiplier;
                _ApplyScale();
            } catch (OperationCanceledException) { }
            finally { _Cleanup(TransitionChannel.Highlight, ct); }
        }

        private async UniTaskVoid _ShakeAsync(CancellationToken ct) {
            try {
                float elapsed = 0f;
                while (elapsed < SHAKE_DURATION) {
                    float t = elapsed / SHAKE_DURATION;
                    motionOffset = UnityEngine.Random.insideUnitCircle * (SHAKE_MAGNITUDE * (1f - t));
                    _ApplyAnchoredPosition();
                    elapsed += Time.deltaTime;
                    await UniTask.NextFrame(ct);
                }
            } catch (OperationCanceledException) { }
            finally {
                if (ctsByChannel.TryGetValue(TransitionChannel.Motion, out CancellationTokenSource activeCts) && activeCts.Token == ct) {
                    motionOffset = Vector2.zero;
                    _ApplyAnchoredPosition();
                }
                _Cleanup(TransitionChannel.Motion, ct);
            }
        }

        private async UniTaskVoid _BounceAsync(CancellationToken ct) {
            try {
                float elapsed = 0f;
                while (elapsed < BOUNCE_DURATION) {
                    float t = elapsed / BOUNCE_DURATION;
                    motionOffset = new Vector2(0f, Mathf.Sin(t * Mathf.PI) * BOUNCE_HEIGHT);
                    _ApplyAnchoredPosition();
                    elapsed += Time.deltaTime;
                    await UniTask.NextFrame(ct);
                }
            } catch (OperationCanceledException) { }
            finally {
                if (ctsByChannel.TryGetValue(TransitionChannel.Motion, out CancellationTokenSource activeCts) && activeCts.Token == ct) {
                    motionOffset = Vector2.zero;
                    _ApplyAnchoredPosition();
                }
                _Cleanup(TransitionChannel.Motion, ct);
            }
        }

        private async UniTask _LerpAlphaAsync(float from, float to, PortraitTransition t, CancellationToken ct) {
            float elapsed = 0f;
            while (elapsed < t.Duration) {
                float raw = elapsed / t.Duration;
                _SetAlpha(Mathf.Lerp(from, to, t.EvaluateCurve(raw)));
                elapsed += Time.deltaTime;
                await UniTask.NextFrame(ct);
            }
            _SetAlpha(to);
        }
        #endregion

        #region Private Helpers
        private void _ApplyAnchoredPosition() {
            ((RectTransform)transform).anchoredPosition = currentSlot.AnchorPos + currentPoseOffset + motionOffset;
        }

        private void _ApplyPoseImmediate(PortraitPose pose) {
            if (pose.Type == PortraitPoseType.Static) {
                if (animator != null) animator.enabled = false;
                _LoadAndSetSpriteAsync(pose.SpriteKey, pose.Key, destroyCancellationToken).Forget();
            } else if (pose.Type == PortraitPoseType.Animated && !string.IsNullOrEmpty(pose.ClipKey)) {
                if (animator != null) {
                    animator.enabled = true;
                    animator.Play(pose.ClipKey);
                }
            }
            currentPoseOffset = pose.PoseOffset;
            _ApplyAnchoredPosition();
        }

        private async UniTaskVoid _LoadAndSetSpriteAsync(string key, string poseKey, CancellationToken ct) {
            if (spriteProvider == null || string.IsNullOrEmpty(key)) return;
            Sprite sprite = await spriteProvider.GetAsync(key, AssetLoadMode.Addressable, AssetFetchMode.CacheFirst);
            if (ct.IsCancellationRequested || image == null) return;
            if (CurrentPoseKey != poseKey) return;
            image.sprite = sprite;
        }

        private void _ApplyScale() {
            // 원본 스프라이트는 Left facing 기준. Right 요청 시 X 반전.
            float facingSign = currentFacing == FacingDirection.Right ? -1f : 1f;
            float s = baseScale * highlightScaleMultiplier;
            transform.localScale = new Vector3(facingSign * s, s, 1f);
        }

        private void _SetAlpha(float alpha) {
            if (image == null) return;
            Color c = image.color;
            image.color = new Color(c.r, c.g, c.b, alpha);
        }

        private float _GetAlpha() => image != null ? image.color.a : 1f;

        private void _Cancel(TransitionChannel channel) {
            if (!ctsByChannel.TryGetValue(channel, out CancellationTokenSource cts)) return;
            cts.Cancel();
            cts.Dispose();
            ctsByChannel.Remove(channel);
        }

        private CancellationToken _RegisterToken(TransitionChannel channel) {
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            ctsByChannel[channel] = cts;
            return cts.Token;
        }

        private void _Cleanup(TransitionChannel channel, CancellationToken token) {
            if (!ctsByChannel.TryGetValue(channel, out CancellationTokenSource cts)) return;
            if (cts.Token != token) return;
            cts.Dispose();
            ctsByChannel.Remove(channel);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: Show/Hide — gameObject 활성·비활성화 계약 명확화
 *
 * # 변경
 * - Show(): 첫 줄 gameObject.SetActive(true) 추가 — 비활성화된 풀 컨트롤러 재등장 대응.
 * - Hide() Instant 경로: image.enabled=false 뒤 gameObject.SetActive(false) 추가.
 * - _HideAsync(): image.enabled=false 뒤 gameObject.SetActive(false) 추가.
 *   Fade 완료 후 IsVisible=false와 동시에 GameObject 비활성화 — 두 상태가 동기화됨.
 *
 * # 이유
 * - AgentReview Warning #8 (2026-05-17 19:13:03).
 * - Fade 완료 후 image.enabled=false로 시각적으로 숨겼지만 gameObject는 active 유지.
 *   풀링 계약("비활성 = 미사용")과 어긋나고, IsVisible=false와 gameObject.activeSelf 불일치.
 * - Show에서 SetActive(true): Controller가 직접 호출되는 외부 경로에도 대응.
 *   CharacterStageDirector._GetOrCreateController의 SetActive(true)와 이중 보장.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: CurrentSlot 반환 타입 FacingDirection → StageSlot
 *
 * # 변경
 * - `public FacingDirection CurrentSlot` → `public StageSlot CurrentSlot`.
 * - currentFacing / CurrentFacing / SetFacing(FacingDirection) — 방향 필드 유지.
 *
 * # 이유
 * - AgentReview Warning #7. SlotConfig.Slot 타입 변경에 따른 연쇄 수정.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: image 필수 Assert 추가
 *
 * # 변경
 * - Awake 첫 줄에 Debug.Assert(image != null, ...) 추가.
 * - 기존 `if (image != null) image.enabled = false` null 체크는 유지
 *   (Assert는 릴리즈에서 스트립되므로 null 방어는 별도 보존).
 *
 * # 이유
 * - AgentReview Warning #6. image null 시 _SetAlpha → NRE. Awake Assert로 사전 차단.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: PortraitPose.Sprite/Clip → SpriteKey/ClipKey Addressable 전환
 *
 * # 변경
 * - IAssetProvider<string, Sprite> spriteProvider 필드 추가
 * - BindProvider(IAssetProvider<string, Sprite>) public API 추가
 * - _ApplyPoseImmediate: image.sprite = pose.Sprite → _LoadAndSetSpriteAsync(pose.SpriteKey) 비동기 로드
 * - _ApplyPoseImmediate: pose.Clip != null → !IsNullOrEmpty(pose.ClipKey), Clip.name → ClipKey
 * - _LoadAndSetSpriteAsync 추가: CacheFirst 비동기 로드 후 CurrentPoseKey 일치 검증 후 적용
 *
 * # 이유
 * - PortraitPose.SpriteKey(Addressable 키)로 전환함에 따라 Controller 로딩 경로 갱신
 * - _LoadAndSetSpriteAsync poseKey 검증: 빠른 포즈 전환 시 늦게 도착한 응답이 현재 포즈를 덮는 race 차단
 *
 * =============================================================================
 * @Jason - PKH 2026.05.18 (수정) :: Motion finally ABA 수정 — token 소유권 확인 후 motionOffset 리셋
 *
 * # 변경
 * - _ShakeAsync / _BounceAsync finally: motionOffset = Vector2.zero 앞에
 *   ctsByChannel.TryGetValue(Motion) + activeCts.Token == ct 소유권 검증 추가.
 *   소유자일 때만 reset + _ApplyAnchoredPosition 수행.
 *
 * # 이유
 * - AgentReview P1 (2026-05-17 19:35:19).
 * - Shake 취소 후 Bounce가 motionOffset을 갱신하기 시작하면, 늦게 깨어난 Shake finally가
 *   Bounce의 offset을 Vector2.zero로 덮어쓰는 ABA 문제.
 * - _Cleanup은 token 검증을 하지만 그 전에 이미 offset 리셋이 실행되어 순서 문제 발생.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: motionOffset 도입 — Shake/Bounce 취소 시 anchoredPosition 정합성 복구
 *
 * # 변경
 * - Vector2 currentPoseOffset, motionOffset 필드 추가.
 * - _ApplyAnchoredPosition(): anchoredPosition = slot.AnchorPos + currentPoseOffset + motionOffset 단일 적용자.
 * - SetSlot / _ApplyPoseImmediate / _ShakeAsync / _BounceAsync 4곳의 anchoredPosition 직접 대입을 단일 적용자로 통합.
 * - _ShakeAsync / _BounceAsync: origin 캡처 제거, motionOffset에 변위 기록.
 *   finally: motionOffset = Vector2.zero + _ApplyAnchoredPosition() → 취소/정상/예외 경로 모두 복구.
 *
 * # 이유
 * - AgentReview P1 #3 (2026-05-17 19:13:03).
 * - shake 도중 bounce 호출 시 잔여 shake offset이 bounce origin을 오염 → 누적 변위.
 * - 옵션 B: origin try-외부 끌어내기(옵션 A)는 motion 중 SetSlot/SetPose 호출 시 stale origin 회귀 위험.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: Warning 보강 — 상수화·CurrentSlot·_ApplyScale 주석·destroyCancellationToken 연결
 *
 * # 변경
 * - SHAKE_DURATION/MAGNITUDE / BOUNCE_DURATION/HEIGHT 상수 추출 (매직 넘버 제거)
 * - CurrentSlot: `currentSlot.Slot` — FacingDirection 직접 반환 (string 프로퍼티 → enum 타입 변경)
 * - _ApplyScale: "원본 스프라이트는 Left facing 기준" 주석 추가
 * - _RegisterToken: `CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken)`
 *   — Unity 객체 파괴 시 destroyCancellationToken 신호를 CTS와 연동하는 이중 안전망 확보
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: Show IsVisible 순서 수정 + _HighlightAsync Cleanup 추가
 *
 * # 변경
 * - Show(): 비동기 경로에서 `IsVisible = true` 즉시 설정 제거.
 *   Instant/Duration<=0 경로에만 남기고, 비동기는 _ShowAsync 완료 시점(alpha=1 후)에만 설정.
 * - _HighlightAsync: duration <= 0f 얼리 리턴 전 `_Cleanup(Highlight, ct)` 추가.
 *
 * # 이유
 * - Show 비동기 경로: alpha=0 상태에서 IsVisible==true 노출 → 외부 구독자 오작동 가능.
 *   UniTask 비동기 패턴에서 상태 갱신은 await 이후(완료 시점)에 두는 것이 원칙.
 * - _HighlightAsync: 얼리 리턴 시 finally에 미도달 → CTS가 Dispose 없이 GC에 방치됨.
 *   IDisposable 명시 관리 패턴과 일관성 유지.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: private 접근제어자 전면 명시 + OnDestroy region 이동
 *
 * # 변경
 * - Transition Async 7개 메서드 : _ShowAsync / _HideAsync / _SetPoseAsync /
 *   _HighlightAsync / _ShakeAsync / _BounceAsync / _LerpAlphaAsync → private 추가
 * - Private Helpers 7개 메서드 : _ApplyPoseImmediate / _ApplyScale / _SetAlpha /
 *   _GetAlpha / _Cancel / _RegisterToken / _Cleanup → private 추가
 * - OnDestroy : #region Private Helpers → #region Unity Lifecycle 이동 + private 추가
 *
 * # 이유
 * - 컨벤션(private 항상 명시, 라이프사이클 포함) 준수.
 * - OnDestroy는 Unity 라이프사이클 메서드이므로 Private Helpers가 아닌
 *   Unity Lifecycle region에 위치해야 독자가 해당 region만으로 생명주기 흐름 파악 가능.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 CTS 채널 딕셔너리 + RGBA 분리
 *
 * # 변경
 * - appearanceCts / highlightCts 필드 제거 → Dictionary<TransitionChannel, CTS> ctsByChannel.
 * - TransitionChannel enum(Appearance, Highlight) 내부 nested 정의.
 * - _Cancel / _RegisterToken / _Cleanup(channel, token) 3 헬퍼로 통합.
 * - _HighlightAsync: image.color 전체 기록 → RGB만 기록(alpha 보존).
 *
 * # 이유
 * - 확장성: 채널 추가 시 enum 한 줄만 늘어남. 기존 2채널/4헬퍼 패턴의 선형 증가 차단.
 * - RGBA 충돌 수정: Highlight(RGB) + Appearance(alpha) 병렬 실행 시 alpha 덮어쓰기 제거.
 * - _Cleanup token 검증: ABA 패턴 차단 — 취소 후 신규 등록된 CTS를 옛 finally가 지우지 않음.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 Awake 초기 상태 숨김 처리
 *
 * # 변경
 * - Awake(): alpha=0, image.enabled=false, IsVisible=false 로 초기화
 *
 * # 이유
 * - 프리팹 인스턴스화 직후 Image 기본 alpha=1 → Show() 호출 전까지 1프레임 노출 → 깜빡임.
 * - Awake에서 숨김 처리 시 Show()의 _SetAlpha(0f) → fade in 흐름과 충돌 없음.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 appearanceCts / highlightCts 분리 (→ Dictionary로 대체됨)
 *
 * # 변경
 * - transitionCts 단일 CTS → appearanceCts + highlightCts 분리
 *
 * # 이유
 * - EnterLine → SetHighlight가 진행 중인 SetPose Crossfade를 취소해
 *   _ApplyPoseImmediate 실행 전 취소되는 버그. 분리로 채널 간 간섭 차단.
 * - (2026.05.17 후속) Dictionary 패턴으로 흡수됨.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 CharacterPortraitController 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 4-C — 슬롯 단위 포트레이트 렌더/트랜지션 핵심 컴포넌트
 *
 * # 설계 결정
 * - Facing: localScale.x 부호 반전. 비대칭 의상은 Phase 5+에서 LeftSprite 필드 확장.
 * - async UniTaskVoid + try/catch OperationCanceledException: .Forget()보다 명확한 취소 핸들링.
 * - Crossfade: 절반 Fade out → 스프라이트 교체 → 절반 Fade in.
 * - highlightStyle은 Bind() 시 전달 — Controller가 StageLayoutSO를 직접 참조하지 않음.
 *
 * =============================================================================
 */
#endif
