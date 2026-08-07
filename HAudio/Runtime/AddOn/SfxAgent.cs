#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 카탈로그 단위 SFX preload / release 를 담당하는 컴포넌트입니다.
 *
 * 특징 / 지원기능 ::
 * + 대상은 클립 하나가 아니라 카탈로그(카테고리) 묶음입니다.
 * + Start 시점에 preload, OnDestroy 시점에 release 자동 수행.
 * + release 는 prewarm 완료를 기다린 뒤 실행합니다.
 *
 * 주의사항 ::
 * + preloadCatalogs 의 SfxView.Catalogs 는 인스펙터에서 미리 채워야 합니다.
 * + Prewarm 호출 중 예외 발생 시 catch 가 GameObject 이름 + 예외 타입/메시지를 stack trace 와 함께 출력합니다.
 * + 클립 하나를 재생하는 API 는 여기에 없습니다. AudioManager 를 직접 부르세요
 *   (게임 측 확장 메서드로 AudioManager.Instance.Play(AudioClips.Click) 형태가 됩니다).
 *
 * 사용법 ::
 * + 인스펙터에서 preloadCatalogs 에 SfxView 추가 (각 SfxView 안에 Catalogs 채움).
 * =========================================================
 */
#endif

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using HInspector;
using HDiagnosis.Logger;
using HDiagnosis.HDebug;

namespace HAudio.AddOn {
    public class SfxAgent : MonoBehaviour {
        #region Fields
        [HTitle("Clips")]
        [SerializeField, FormerlySerializedAs("preloadCatalogs")]
        SfxView preloadCatalogs = new();

        UniTask prewarmTask = UniTask.CompletedTask;
        #endregion

        #region Properties
        public SfxView PreloadCatalogs => preloadCatalogs;
        #endregion

        #region Unity Life Cycle
#if UNITY_EDITOR
        private void OnValidate() {
            preloadCatalogs?.EditorRebuildPreview();
        }
#endif

        private void Start() {
            if (!AudioManager.HasInstance) return;
            prewarmTask = _PrewarmViews();
        }

        private void OnDestroy() {
            if (!Application.isPlaying) return;
            if (!AudioManager.HasInstance) return;
            _ReleaseViewsAfterPrewarm().Forget();
        }
        #endregion

        #region Private - Prewarm
        private async UniTask _PrewarmViews() {
            try {
                await AudioManager.Instance.PrewarmSfxView(preloadCatalogs);
            }
            catch (System.Exception ex) {
                HDebug.StackTraceError($"[AudioManager] {gameObject.name} : {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}", 20);
                HLogger.Exception(ex);
            }
        }

        private async UniTaskVoid _ReleaseViewsAfterPrewarm() {
            // prewarm 이 in-flight 인 채로 release 가 먼저 실행되면 등록이 뒤늦게 도착해
            // registry refCount 가 영구 잔류한다 — 완료를 기다린 뒤 해제한다.
            await prewarmTask;
            if (!AudioManager.HasInstance) return;
            AudioManager.Instance.ReleaseSfxView(preloadCatalogs);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.08.06 클립 단위 재생 API 4종 제거 + 데브로그 정정
 *
 * # 삭제
 * - Play / PlayUI / Play3D(Transform) / Play3D(Vector3) — 전부 string token 을 받아
 *   AudioManager.Instance 로 그대로 넘기는 포워딩이었다.
 *
 * # 이유
 * - 고유 동작이 없다. HasInstance 검사 하나를 더한 포워딩 계층이라 유지 비용만 남는다.
 * - 이 컴포넌트의 존재 이유는 카탈로그 단위 preload/release 다. 클립 단위 재생은 축이 다르다.
 * - UnityEvent 배선 용도로 남길 근거도 약하다. int 를 받아도 인스펙터에 숫자 입력칸이 뜰
 *   뿐이고, 인스펙터 주도 클릭음은 BaseSfxAddon 이 [HDropdown] 필드로 처리한다.
 * - 게임 코드는 AudioManager.Instance.Play(AudioClips.Click) 을 직접 부른다 (ADR-0001).
 *
 * # 정정
 * - 2026.08.04 데브로그가 "Legacy Support 리전을 Public - Uid API 로 개칭 (uid/enum
 *   오버로드는 AudioManager uid partial 로 포워딩 유지)" 라고 서술했으나 사실이 아니었다.
 *   58fc15a 가 uid 오버로드를 전부 제거해 실제 리전은 Public - Token API 였고 uid
 *   오버로드는 0개였다. 문서가 코드에 대해 거짓을 말하는 상태였으므로 해당 항목을 걷어냈다.
 * - 헤더의 "호출 인자는 string token 단일 체계" / "외부 호출은 Play / PlayUI / Play3D"
 *   서술도 함께 제거했다.
 *
 * =============================================================================
 * @Jason - PKH 2026.08.04 이중 매니저 분기 제거 - AudioManager 단일화
 *
 * # 삭제
 * - useNewManager 직렬화 필드 및 SoundManager(구형) 폴백 분기 전체 제거.
 * - _HasTargetManager 제거 - Start/OnDestroy 에서 AudioManager.HasInstance 직접 확인.
 *
 * # 변경
 * - _PrewarmViews / _ReleaseViews 를 AudioManager 단일 경로로 축소.
 *
 * # 이유
 * - 구형 SoundManager 는 외부 참조 0건으로 삭제 확정 (Phase 1 HCUP 정리). 라우팅 분기는 dead path.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.01 _PrewarmViews catch 진단 정보 회복 + 헤더/데브로그 정본화
 *
 * # 변경
 * - _PrewarmViews 의 두 catch 블록을 `catch (System.Exception ex)` 로 변경.
 * - 출력 메시지에 ex.GetType().Name + ex.Message + ex.StackTrace 를 포함.
 * - UnityEngine.Debug.LogException(ex) 호출을 추가해 Unity 콘솔의 자동 hyperlink 와 inner stack 활용.
 * - 헤더 / 데브로그 블록 신규 추가 (정본 형식 적용).
 *
 * # 이유
 * - 기존 catch 가 예외 객체를 캡처하지 않아 [C] Player 의 SfxAgent.Start 호출 stack trace 만 노출되고 실제 throw 원인 (예외 타입 / 메시지) 이 가려진 상태였음.
 * - HDebug.StackTraceError(message, depth) 는 caller 측 stack frame 만 capture 하여 throw point 의 inner stack 이 메시지에 안 들어가는 한계가 있음. ex.StackTrace 와 LogException 으로 보완.
 * - LoadGate Preserve 패치 후에도 같은 throw 가 동일 위치에서 재현되어 throw 의 정확한 await point 식별이 우선 과제로 전환.
 *
 * =============================================================================
 */
#endif
