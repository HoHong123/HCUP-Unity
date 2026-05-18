#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 플레이어 선택지 분기 노드. HubNode 상속으로 다중 출구 포트 보유.
 *
 * 특징 / 지원기능 ::
 * + promptText         - 선택지 위에 표시할 질문 텍스트 (생략 가능)
 * + List<ChoiceData> choices - Key / DisplayText / ConditionKey
 * + fallbackChoiceKey  - 유효 선택지 0개 시 자동 선택할 Key (빈 문자열이면 워닝+Finished)
 * + ConditionKey 빈 문자열 = 무조건 표시
 * + DialogueDirector가 variables 평가 후 유효 선택지만 OnChoicePresent 발행
 *
 * 주의사항 ::
 * choices[i].Key 와 HubNode 출구 포트 키(Entries[i].Key)는 반드시 일치해야 함.
 * HubNode entries mutation은 Author 경유 필수 (Editor asmdef internal 전용).
 * 현재 OnValidate는 경고 전용 — Phase 5 HDialogue.Editor 브리지에서 실제 동기화.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using HDiagnosis.Logger;
using HInspector;
using HWindows.NodeWindow;
using UnityEngine;

namespace HDialogue {
    public sealed class DialogueChoiceNode : HubNode {
        #region Nested Types
        [Serializable]
        public struct ChoiceData {
            public string Key;
            public string DisplayText;
            public string ConditionKey;
        }
        #endregion

        #region Fields
        [HTitle("Choice")]
        [TextArea(2, 4)]
        [SerializeField]
        string promptText;
        [SerializeField]
        List<ChoiceData> choices = new();
        [SerializeField]
        string fallbackChoiceKey;
        #endregion

        #region Properties
        public string PromptText => promptText;
        public IReadOnlyList<ChoiceData> Choices => choices;
        public string FallbackChoiceKey => fallbackChoiceKey;
        #endregion

        public override string ClipboardMagic => "HGRAPH_DIALOGUE_CHOICE_NODE_V1";

#if UNITY_EDITOR
        void OnValidate() {
            if (choices.Count != PortCount) {
                HLogger.Warning(
                    $"[DialogueChoiceNode] '{Title}' — choices.Count({choices.Count}) != PortCount({PortCount}). " +
                    "HubNode entries sync required via HDialogue.Editor bridge (Phase 5)."
                );
            }
        }
#endif
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 Phase 2 — fallbackChoiceKey 필드 추가
 *
 * # 추가
 * - fallbackChoiceKey string 필드 + FallbackChoiceKey 프로퍼티
 * - DialogueDirector._ProcessChoiceNode에서 유효 선택지 0개 시 사용
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueChoiceNode 베이스 코드 생성
 *
 * # 목적
 * - 플레이어 선택지 분기 허브 노드 — HCUP-2.3.0 Phase 1
 *
 * # 설계 결정
 * - HubNode 상속: choices[i].Key ↔ HubNode 출구 포트 키 1:1 매핑 의존
 * - ChoiceData.ConditionKey: 빈 문자열 = 무조건 표시 (variables null에서도 통과)
 * - OnValidate 경고 전용: HubNode.AddEntry/SetEntryKey는 internal (HWindows 어셈블리).
 *   HCUP.HDialogue 런타임에서 호출 불가 → Phase 5에서 HCUP.HDialogue.Editor 브리지 구현
 *
 * # 사용 흐름
 * - 그래프 에디터 Author.AddHubEntry(choiceKey) → entries 동기화
 * - DialogueDirector._ProcessChoiceNode → choices 조건 필터 → OnChoicePresent
 * - SelectChoice(key) → HubNodeEdge 매칭 → 다음 노드
 *
 * =============================================================================
 */
#endif
