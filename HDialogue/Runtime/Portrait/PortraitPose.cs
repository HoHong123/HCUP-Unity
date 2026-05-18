#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 단일 포즈 데이터 구조체. CharacterPortraitSetSO.poses 원소.
 *
 * 특징 / 지원기능 ::
 * + Key        - 포즈 식별자 ("neutral" / "happy" 등)
 * + SpriteKey  - Static 포즈 Addressable 키 ([HSpritePreview] 미리보기 지원)
 * + ClipKey    - Animated 포즈 Animator.Play 인자 (Animator에 등록된 클립명과 일치해야 함)
 * + Type       - 렌더링 방식 (Static / Animated / Sequence)
 * + PoseOffset - 슬롯 기준 위치 보정값 (Vector2)
 *
 * 주의사항 ::
 * SpriteKey는 Addressable 키 — 직접 Sprite 참조 없음. SO 저장 용량 최소화.
 * SerializeField 가 있는 struct — Inspector 인라인 편집 대상.
 * =========================================================
 */
#endif

using System;
using HInspector;
using UnityEngine;

namespace HDialogue {
    [Serializable]
    public struct PortraitPose {
        [HTitle("Identity")]
        public string Key;

        [HTitle("Renderer")]
        [HSpritePreview]
        public string SpriteKey;
        public string ClipKey;
        public PortraitPoseType Type;

        [HTitle("Layout")]
        public Vector2 PoseOffset;
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: Sprite/AnimationClip → SpriteKey/ClipKey Addressable 키 전환
 *
 * # 변경
 * - public Sprite Sprite → [HSpritePreview] public string SpriteKey (Addressable 키)
 * - public AnimationClip Clip → public string ClipKey (Animator.Play 직접 인자)
 *
 * # 이유
 * - SO 저장 데이터 최소화: 스프라이트 에셋 직접 참조 제거, 키만 직렬화
 * - [HSpritePreview]: Inspector에서 Addressable 키로 스프라이트 미리보기 지원
 * - ClipKey: Animator 클립명이 곧 키 — AnimationClip 에셋 직접 참조 불필요
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: HInspector HTitle 그룹 추가 (Identity / Renderer / Layout)
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 PortraitPose 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 4-A — CharacterPortraitSetSO.poses 항목 계약
 *
 * # 설계 결정
 * - Sprite / AnimationClip 직접 필드 허용: PortraitPose는 BaseNode 하위가 아니라
 *   CharacterPortraitSetSO(ScriptableObject)의 인스펙터 전용 데이터이므로 직접 참조 가능.
 * - BaseNode 규칙("UnityEngine.Object 금지")은 NodeCatalog SO 노드 타입에만 적용.
 *
 * =============================================================================
 */
#endif
