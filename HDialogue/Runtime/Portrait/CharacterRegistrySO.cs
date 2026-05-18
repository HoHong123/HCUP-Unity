#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 전체 캐릭터 포트레이트 집합 레지스트리 ScriptableObject.
 *
 * 특징 / 지원기능 ::
 * + characters List<CharacterPortraitSetSO> - 등록된 캐릭터 집합
 * + TryGet(characterKey, out set)           - O(n) 키 탐색
 *
 * 주의사항 ::
 * CharacterStageDirector.Bind() 에 전달되어 런타임 캐릭터 참조 기반으로 사용.
 * =========================================================
 */
#endif

using System.Collections.Generic;
using HInspector;
using UnityEngine;

namespace HDialogue {
    [CreateAssetMenu(menuName = "HWindows/Dialogue/Character Registry")]
    public sealed class CharacterRegistrySO : ScriptableObject {
        #region Fields
        [HTitle("Characters")]
        [SerializeField]
        List<CharacterPortraitSetSO> characters = new();
        #endregion

        public IReadOnlyList<CharacterPortraitSetSO> Characters => characters;

        public bool TryGet(string characterKey, out CharacterPortraitSetSO set) {
            foreach (CharacterPortraitSetSO c in characters) {
                if (c != null && c.CharacterKey == characterKey) {
                    set = c;
                    return true;
                }
            }
            set = null;
            return false;
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 CharacterRegistrySO 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 4-A — 씬/카탈로그 단위 캐릭터 집합 관리
 *
 * =============================================================================
 */
#endif
