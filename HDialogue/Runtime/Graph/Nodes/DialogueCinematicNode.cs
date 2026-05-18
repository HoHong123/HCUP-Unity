#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대사 없는 무대 연출 전용 노드.
 *
 * 특징 / 지원기능 ::
 * + instructions      — CinematicInstruction 목록 (대상 캐릭터 × PortraitVerb × arg)
 * + waitForTransition — true 시 트랜지션 완료까지 대기 (WaitForActiveTransitionsAsync)
 * + waitForInput      — true 시 트랜지션 완료 후 Next 명령 대기. false(기본) 시 자동 진행.
 *
 * 주의사항 ::
 * 화자 1명 포트레이트는 DialogueLineNode 필드를 사용할 것. 이 노드는 다중 캐릭터 연출 전용.
 * waitForInput=true + waitForTransition=false 조합 시 지시 직후 즉시 입력 대기 상태로 진입.
 * =========================================================
 */
#endif

using System.Collections.Generic;
using HInspector;
using HWindows.NodeWindow;
using UnityEngine;

namespace HDialogue {
    public sealed class DialogueCinematicNode : BaseNode {
        #region Fields
        [HTitle("Cinematic")]
        [SerializeField]
        bool waitForInput;
        [SerializeField]
        bool waitForTransition = true;
        [SerializeField]
        List<CinematicInstruction> instructions = new List<CinematicInstruction>();
        #endregion

        #region Properties
        public IReadOnlyList<CinematicInstruction> Instructions => instructions;
        public bool WaitForTransition => waitForTransition;
        public bool WaitForInput => waitForInput;
        #endregion

        public override string ClipboardMagic => "HGRAPH_DIALOGUE_CINEMATIC_NODE_V1";
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: autoNext → waitForInput 이름 반전 — 직렬화 기본값 정합
 *
 * # 변경
 * - `bool autoNext = true` → `bool waitForInput` (기본값 false, 이름 의미 반전).
 * - `bool AutoNext` getter → `bool WaitForInput` getter.
 * - 헤더 주석 업데이트.
 *
 * # 이유
 * - Unity 직렬화는 기존 .asset에 필드가 없으면 C# 이니셜라이저가 아닌 default(bool)=false를 사용.
 *   `autoNext = true` 기본값은 런타임 new()에만 적용되고 기존 에셋 역직렬화에는 무시됨.
 * - `waitForInput = false`(이름 반전) 채택: false = 자동 진행(기존 동작 유지). 기존 에셋 미마이그레이션.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.19 (최초) :: waitForInput 필드 추가 — 트랜지션 후 자동 진행/Next 대기 분기
 *
 * # 변경
 * - `bool waitForInput` 직렬화 필드 추가.
 * - `bool WaitForInput` getter 추가.
 *
 * # 이유
 * - 트랜지션 완료 후 자동으로 다음 노드로 진행(false)하거나,
 *   사용자의 Next 명령을 기다린 뒤 진행(true)하는 두 흐름을 선택 가능하게.
 *
 * # 결과
 * - DialogueDirector._ProcessCinematicNode에서 WaitForInput 분기 추가.
 * - HGraphDialogueCinematicNode에서 true 시 "wait next" 라벨 표시.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.16 DialogueCinematicNode 신규 생성
 *
 * # 목적
 * - DialoguePortraitNode 대안 — 대사 없는 다중 캐릭터 무대 연출 노드.
 *
 * # 설계 결정
 * - 이름 DialogueCinematicNode: Portrait(단일 자화상) 대신 Cinematic(영상 연출) 선택.
 *   화자 1명 바인딩 없이 캐릭터 N명을 자유 조합하는 이 노드의 성격에 부합.
 * - waitForTransition 기본값 true: 연출 지시 후 즉시 다음 노드로 넘어가는 것이 드물기 때문.
 * - IReadOnlyList<CinematicInstruction>: 외부 변경 방지. Director는 읽기만.
 *
 * =============================================================================
 */
#endif
