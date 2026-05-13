#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 900000_Goods.xlsx Goods 시트 단일 행 직렬화 데이터 클래스입니다.
 *
 * 특징 ::
 * - [HSpritePreview] : Icon 필드에 Addressables / Resources 기반 인라인 Foldout 미리보기
 * - [HTitle] : 인스펙터 시각 그룹 분리
 *
 * 주의사항 ::
 * - [HTitle] 시각 효과는 HInspectorBehaviour / HInspectorScriptableObject
 *   상속 타겟의 CustomEditor 경로에서만 렌더됩니다.
 * =========================================================
 */
#endif

using System;
using HInspector;

namespace HData.NPOI.Samples {
    [Serializable]
    public class GoodsData {
        [HTitle("기본 정보")]
        public int    Id;
        public string Name;
        public int    Local;
        [HTitle("에셋 & 표기")]
        [HSpritePreview]
        public string Icon;
        public string Display;
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.13 HTitle 시각 그룹 + HSpritePreview Foldout 교체
 *
 * # 변경
 * - [HTitle("기본 정보")] : Id 필드 위 그룹 헤더 추가 (Id / Name / Local)
 * - [HTitle("에셋 & 표기")] : Icon 필드 위 그룹 헤더 추가 (Icon / Display)
 * - [SpritePreview] → [HSpritePreview] (HInspector 패키지 이전에 따른 교체)
 * - using HData.NPOI.Core.Editor → using HInspector
 *
 * =============================================================================
 */
#endif
