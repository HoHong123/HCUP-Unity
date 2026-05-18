#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대사 없는 무대 연출 전용 노드.
 *
 * 특징 / 지원기능 ::
 * + instructions — CinematicInstruction 목록 (대상 캐릭터 × PortraitVerb × arg)
 * + waitForTransition — true 시 지시 적용 후 1프레임 대기 (전환 애니메이션 시작 보장)
 *
 * 주의사항 ::
 * 화자 1명 포트레이트는 DialogueLineNode 필드를 사용할 것. 이 노드는 다중 캐릭터 연출 전용.
 * waitForTransition 은 UniTask.NextFrame 수준 — 트랜지션 완료 대기(ms 단위)는 미지원.
 * =========================================================
 */
#endif

using System.Collections.Generic;
using HWindows.NodeWindow;
using UnityEngine;

namespace HDialogue {
    public sealed class DialogueCinematicNode : BaseNode {
        #region Fields
        [SerializeField]
        List<CinematicInstruction> instructions = new List<CinematicInstruction>();
        [SerializeField]
        bool waitForTransition = true;
        #endregion

        #region Properties
        public IReadOnlyList<CinematicInstruction> Instructions => instructions;
        public bool WaitForTransition => waitForTransition;
        #endregion

        public override string ClipboardMagic => "HGRAPH_DIALOGUE_CINEMATIC_NODE_V1";
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
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
