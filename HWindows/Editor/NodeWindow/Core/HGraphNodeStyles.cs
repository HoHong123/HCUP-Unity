using System;
using UnityEngine;

namespace HWindows.Editor.NodeWindow {
    public static class HGraphNodeStyles {
        #region Const
        // Q8 Shader Graph 스타일 시안 채택 색 (#4A6FA5)
        public static readonly Color DefaultHeaderColor = new Color(0.29f, 0.435f, 0.647f);

        // 루트 노드 전용 색 (노란색). 도메인 커스터마이즈와 무관하게 항상 이 색 우선 적용.
        // 사용자 지시: "루트 노드만의 고유 색상은 반드시 지켜져야 하는 규칙"
        public static readonly Color RootHeaderColor = new Color(0.85f, 0.7f, 0.2f);
        #endregion

        #region Public
        /// <summary>
        /// 노드 타입별 헤더 색 조회.
        /// Phase 1-A: 타입 무관하게 DefaultHeaderColor 반환 (SimpleNode 하나뿐).
        /// 최초 도메인 서브클래스 추가 시점에 메커니즘 확정 (attribute 감지 또는 타입별 매핑).
        /// 주의: 루트 노드 색은 이 메서드를 우회해야 함 (HGraphNode 가 isRoot 분기로 처리).
        /// </summary>
        public static Color GetHeaderColorFor(Type nodeType) {
            return DefaultHeaderColor;
        }
        #endregion
    }
}

#if UNITY_EDITOR
// =============================================================================
// Dev Log
// =============================================================================
// @Jason - PKH 2026-04-24 HGraphNodeStyles 의 역할 - 노드 외형 상수 집약 + 도메인 확장 stub
//
//   [역할]
//   - 노드 외형 관련 수치 상수 (색·크기·여백 등) 의 단일 집약 지점.
//   - 도메인 서브클래스별 헤더 색 커스터마이즈의 확장 포인트 (Phase 1-A 에선 stub).
//
//   [Phase 1-A 결정]
//   - GetHeaderColorFor 는 타입 무관하게 DefaultHeaderColor 반환.
//   + SimpleNode 하나뿐이라 타입 분기의 실질 가치 0.
//   + 최초 도메인 서브(DialogueNode 등) 추가 시점에 메커니즘 확정:
//      옵션 a: [HNodeHeaderColor("#...")] attribute
//      옵션 b: virtual Color GetHeaderColor() on BaseNode
//      옵션 c: 외부 레지스트리 (nodeType => Color) 매핑
//
//   [확장 위치]
//   - 스타일 관련 추가 상수 (모서리 둥글기·여백 수치) 가 필요하면 이 파일에 집약.
//   - USS 와의 분담: 정적 수치 = C# 상수, 동적 상호작용 스타일 = USS.
// =============================================================================
#endif
