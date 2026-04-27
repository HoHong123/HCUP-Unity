using HWindows.NodeWindow.Identity;
using UnityEditor;
using UnityEngine;

namespace HWindows.Editor.NodeWindow.Identity {
    [FilePath("ProjectSettings/NodeUIDRegistry.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class NodeUIDRegistry : ScriptableSingleton<NodeUIDRegistry> {
        #region Fields
        [SerializeField] int nextValue = 1;
        #endregion

        #region Public - Issue
        // 단조 증가 발급. 매 호출마다 즉시 디스크 flush — 에디터 크래시 시 카운터 유실 방지.
        public NodeUID Issue() {
            NodeUID issued = new(nextValue);
            nextValue++;
            Save(saveAsText: true);
            return issued;
        }

        public int PeekNext() => nextValue;
        #endregion
    }
}

#if UNITY_EDITOR
// =============================================================================
// Dev Log
// =============================================================================
// @Jason - PKH 2026-04-22 NodeUIDRegistry 의 역할 - 프로젝트 전역 NodeUID 발급기
//
//   [역할]
//   - NodeUID 의 단일 source of truth. 단조 증가 정수 카운터.
//   - ScriptableSingleton<T> 패턴으로 에디터 세션·Assembly 리로드에 안전.
//
//   [저장 위치] ProjectSettings/NodeUIDRegistry.asset
//   - FilePathAttribute.Location.ProjectFolder 명시 → git 추적 + 팀 공유 가능.
//   - 기본값 Library/ 는 git ignored 라 부적합.
//
//   [Save 정책]
//   - 매 Issue() 마다 즉시 디스크 flush (saveAsText: true).
//   + 에디터 크래시 / 강제 종료 시에도 카운터 유실 방지.
//   + 디스크 쓰기 비용 있으나 노드 생성은 사용자 편집 액션이라 빈도 제한적.
//
//   [Thread Safety]
//   - Interlocked.Increment 미사용. 에디터 메인 스레드 단일 진입 가정.
//   + DataOwnerIdGenerator 는 런타임 multi-thread 가능성 때문에 Interlocked 사용.
//   + Registry 는 에디터 전용이라 해당 없음.
//
//   [#if UNITY_EDITOR 가드 미사용]
//   - 이 파일은 Editor asmdef 거주 → 빌드에서 자동 제외.
//   - 본문에 추가 가드 중복은 가독성 손해 + 잘못된 인상 ("Runtime 가능?") 유발.
//
//   [한계 - 팀 병합 충돌]
//   - 두 개발자가 다른 브랜치에서 동시에 Issue() 호출 시 같은 nextValue 발급.
//   - 머지 시 NodeUID 가 두 노드에 중복 할당되는 데이터 충돌 발생 가능.
//   - 현재 단일 개발자 환경이라 미해결로 둠. 팀 확장 시 후속 과제:
//     + UID 레인지 분배 (개발자 A: 1M~, 개발자 B: 2M~)
//     + 충돌 해결 프로토콜 (머지 후 UID 재할당 도구)
// =============================================================================
#endif
