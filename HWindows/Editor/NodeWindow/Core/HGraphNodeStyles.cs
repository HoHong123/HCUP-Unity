using System;
using System.Collections.Generic;
using UnityEngine;
using HWindows.NodeWindow;

namespace HWindows.Editor.NodeWindow {
    public static class HGraphNodeStyles {
        #region Const
        // Q8 Shader Graph 스타일 시안 채택 색 (#4A6FA5)
        public static readonly Color DefaultHeaderColor = new Color(0.29f, 0.435f, 0.647f);

        // 루트 노드 전용 색 (노란색). 도메인 커스터마이즈와 무관하게 항상 이 색 우선 적용.
        public static readonly Color RootHeaderColor = new Color(0.85f, 0.7f, 0.2f);

        // Phase 3 — CatalogNode 전용 헤더 색 (청록). 일반 노드(파란)·루트(노란)와 시각 구분.
        public static readonly Color CatalogNodeHeaderColor = new Color(0.15f, 0.52f, 0.48f);
        // Phase 5 — 도메인 노드 타입별 헤더 색. RegisterHeaderColor 로 외부 어셈블리가 등록.
        static readonly Dictionary<Type, Color> domainHeaderColors = new();
        #endregion

        #region Public
        // 도메인 노드 타입에 헤더 색 등록. [InitializeOnLoadMethod] 에서 호출.
        public static void RegisterHeaderColor(Type nodeType, Color color) {
            if (nodeType != null) domainHeaderColors[nodeType] = color;
        }

        // 노드 타입별 헤더 색 조회. 도메인 등록 색 우선, 루트 색은 HGraphNode 가 별도 처리.
        public static Color GetHeaderColorFor(Type nodeType) {
            if (domainHeaderColors.TryGetValue(nodeType, out Color c)) return c;
            if (nodeType == typeof(CatalogNode)) return CatalogNodeHeaderColor;
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
