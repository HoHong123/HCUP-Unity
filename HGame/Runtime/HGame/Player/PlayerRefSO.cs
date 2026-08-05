using System;
using UnityEngine;
using HDiagnosis.HDebug;

namespace HGame.Player {
    [Serializable]
    [CreateAssetMenu(
        fileName = "PlayerRef", 
        menuName = "HCUP/Player/Reference",
        order = 0)]
    public sealed class PlayerRefSO : ScriptableObject {
        PlayerStatus reference = null;

        public IPlayerReadOnly ReadOnly { get; private set; }
        public IPlayerCommand Command { get; private set; }

        public void Set(PlayerStatus status) {
            if (status == null || reference != null) return;
            HDebug.StackTraceLog("Set Player Status");
            reference = status;
            ReadOnly = status;
            Command = status;
        }

        public void Clear(PlayerStatus status) {
            if (reference == status) {
                HDebug.StackTraceLog("Clear Player Status");
                // reference 를 남기면 Set 의 조기 반환 조건에 걸려 재설정이 영구 불가.
                // SO 특성상 씬 재진입에도 상태가 남으므로 반드시 함께 비운다.
                reference = null;
                ReadOnly = null;
                Command = null;
            }
        }
    }
}

#if UNITY_EDITOR
/* Dev Log
 * @Jason - PKH
 * 스크립터블 오브젝트를 통한 레퍼런스 플레이어 스탯 참조 클래스
 * 게임의 규모가 커지거나 멀티플레이 등 수정사항이 발생시, 싱글톤 혹은 서비스 로케이터 등 다른 방법을 구상해볼만하다.
 * + 전역이 아닌 필요한 곳에서만 해당 SO를 할당 받아 접근 할 수 있도록 설정
 * + 싱글플레이만 지원하는 환경에서 플레이어 스탯을 최소한의 결합을 추구하기 위해 작성
 * + 전역 상태 오염을 피하기 위해 존재
 */
#endif
