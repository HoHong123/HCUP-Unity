#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 카탈로그 단위 BGM + sfx.* 이벤트 SFX 컨트롤러 (HCUP-2.4.0 Phase 1).
 *
 * 특징 / 지원기능 ::
 * + Bind(DialogueDirector) / Unbind() — 디렉터 이벤트 구독·해제
 * + OnCatalogStart  → AudioManager.PlayBGM(catalog.BgmKey) — BgmKey 비어 있으면 스킵
 * + OnCatalogExit   → AudioManager.StopBGM(bgmFadeOut) — BGM 재생 중일 때만 정지
 * + OnEventFired    → "sfx." 접두 키 감지 → AudioManager.Play(arg)
 *
 * 주의사항 ::
 * sfx.* arg 토큰은 AudioManager에 미리 PrewarmToken 필요.
 * DialogueManager SerializeField 슬롯에 Inspector 연결 후 Bind() 호출.
 * =========================================================
 */
#endif

using System;
using HAudio;
using HInspector;
using UnityEngine;

namespace HDialogue {
    public sealed class DialogueAudioController : MonoBehaviour {
        #region Const
        const string SFX_PREFIX = "sfx.";
        #endregion

        #region Fields
        [HTitle("BGM")]
        [SerializeField]
        float bgmFadeOut = 0.5f;

        DialogueDirector director;
        bool isBgmPlaying;
        #endregion

        #region Unity Life Cycle
        private void OnDestroy() {
            Unbind();
        }
        #endregion

        #region Public
        public void Bind(DialogueDirector target) {
            if (director != null) Unbind();
            director = target;
            director.OnCatalogStart += _OnCatalogStart;
            director.OnCatalogExit += _OnCatalogExit;
            director.OnEventFired += _OnEventFired;
        }

        public void Unbind() {
            if (director == null) return;
            director.OnCatalogStart -= _OnCatalogStart;
            director.OnCatalogExit -= _OnCatalogExit;
            director.OnEventFired -= _OnEventFired;
            director = null;
        }
        #endregion

        #region Private
        private void _OnCatalogStart(DialogueCatalogSO catalog) {
            if (string.IsNullOrEmpty(catalog.BgmKey)) return;
            if (AudioManager.Instance == null) return;
            AudioManager.Instance.PlayBGM(catalog.BgmKey);
            isBgmPlaying = true;
        }

        private void _OnCatalogExit(DialogueCatalogSO catalog, string exitKey) {
            if (!isBgmPlaying) return;
            if (AudioManager.Instance == null) return;
            AudioManager.Instance.StopBGM(bgmFadeOut);
            isBgmPlaying = false;
        }

        private void _OnEventFired(string key, string arg) {
            if (!key.StartsWith(SFX_PREFIX, StringComparison.Ordinal)) return;
            if (string.IsNullOrEmpty(arg)) return;
            if (AudioManager.Instance == null) return;
            AudioManager.Instance.Play(arg);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.19 DialogueAudioController 베이스 코드 생성
 *
 * # 목적
 * - 카탈로그 단위 BGM 자동 재생·정지 + sfx.* 이벤트 SFX 재생 — HCUP-2.4.0 Phase 1
 *
 * # 설계 결정
 * - Bind/Unbind 패턴: DialogueManager.Awake()에서 wire. OnDestroy Unbind로 리스너 누수 방지.
 * - isBgmPlaying 플래그: BgmKey=""인 카탈로그 Exit 시 불필요한 StopBGM 방지.
 * - bgmFadeOut SerializeField(기본 0.5s): 씬별 페이드 튜닝 가능.
 * - SFX_PREFIX const "sfx.": PortraitEventParser의 "portrait." 접두 패턴과 동일 규약.
 * - StartsWith(StringComparison.Ordinal): 문화권 무관 바이트 비교, 이벤트 키 파싱 관용구.
 *
 * =============================================================================
 */
#endif
