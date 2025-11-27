using System;
using UnityEngine;
using HUtil.Diagnosis;

namespace HGame.Player {
    [Serializable]
    [CreateAssetMenu(
        fileName = "PlayerRef", 
        menuName = "Game/Player/Reference",
        order = 0)]
    public sealed class PlayerRefSO : ScriptableObject {
        PlayerStatus reference = null;

        public IPlayerReadOnly ReadOnly { get; private set; }
        public IPlayerCommand Command { get; private set; }

        public void Set(PlayerStatus status) {
            if (!status) return;
            HDebug.StackTraceLog("Set Player Status");
            reference = status;
            ReadOnly = status;
            Command = status;
        }

        public void Clear(PlayerStatus status) {
            if (reference == status) {
                HDebug.StackTraceLog("Clear Player Status");
                ReadOnly = null;
                Command = null;
            }
        }
    }
}

/* Dev Log
 * @Jason - PKH
 * + 스크립터블 오브젝트를 통한 레퍼런스 플레이어 스탯 참조 클래스
 * ++ 전역이 아닌 필요한 곳에서만 해당 SO를 할당 받아 접근 할 수 있도록 설정
 * ++ 싱글플레이만 지원하는 환경에서 플레이어 스탯을 최소한의 결합을 추구하기 위해 작성
 * ++ 전역 상태 오염을 피하기 위해 존재
 * + 게임의 규모가 커지거나 멀티플레이 등 수정사항이 발생시, 싱글톤 혹은 다른 방법을 구상해볼만하다.
 */