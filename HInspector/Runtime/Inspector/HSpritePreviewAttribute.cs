#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * string 또는 Object 필드에 부착해 인스펙터 우측 인라인 Sprite 미리보기를 활성화합니다.
 *
 * 사용 예 ::
 * [HSpritePreview]         public string Icon;   // 기본 48px
 * [HSpritePreview(64f)]    public string Icon;   // 커스텀 크기
 *
 * 특징 ::
 * - string 필드 : Resources 경로 또는 Addressables 키로 탐색
 * - Object 필드 : Sprite / Texture2D 순으로 대응
 *
 * 주의사항 ::
 * - UNITY_EDITOR 전용 — 클래스 정의 자체가 #if UNITY_EDITOR 가드 내부
 * - HInspectorAttribute 파생 제외 — HInspectorPropertyDrawer 처리 대상에서 분리
 * =========================================================
 */

using UnityEngine;

namespace HInspector {
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class HSpritePreviewAttribute : PropertyAttribute {
        public float Size { get; }
        public HSpritePreviewAttribute(float size = 48f) => Size = size;
    }
}
#endif

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.13 클래스 본체 #if UNITY_EDITOR 가드 적용
 *
 * # 변경
 * - 헤더 주석과 클래스 본체를 단일 #if UNITY_EDITOR 블록으로 통합
 *
 * # 이유
 * - [Conditional("UNITY_EDITOR")] 는 사용처(call site) IL 제거 — 클래스 정의는 런타임 빌드 잔류
 * - #if UNITY_EDITOR 가드로 클래스 자체를 런타임 바이너리에서 완전히 제거
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 HSpritePreviewAttribute 베이스 코드 생성
 *
 * # 목적
 * - HData.NPOI.Core.Editor 의 SpritePreviewAttribute 를 HInspector 패키지로 이전
 * - PropertyAttribute 기반 인라인 미리보기는 NPOI 도메인 종속이 아닌 범용 인스펙터 유틸리티
 *
 * # 구현 결정
 * - PropertyAttribute 직접 상속 유지 (HInspectorAttribute 파생 시 HInspectorPropertyDrawer
 *   의 Field-only 사전 검증 경로로 충돌 가능 — HTitleAttribute 와 동일 우회 패턴)
 * - [Conditional("UNITY_EDITOR")] : 빌드에서 사용처(call site) IL 추가 제거
 *
 * =============================================================================
 */
#endif
