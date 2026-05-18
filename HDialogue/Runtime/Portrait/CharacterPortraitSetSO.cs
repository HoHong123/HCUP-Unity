#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 캐릭터 1종의 포즈 집합 ScriptableObject.
 *
 * 특징 / 지원기능 ::
 * + characterKey / displayName - 식별자 및 표시명
 * + poses List<PortraitPose>   - 포즈 정의 목록
 * + defaultPoseKey             - 포즈 미지정 시 사용 ("neutral")
 * + pivotOffset / defaultFacing - 슬롯 기준 위치 보정 + 기본 방향
 * + TryGetPose(key, out pose)  - O(n) 키 탐색 (포즈 수 적어 충분)
 *
 * 주의사항 ::
 * CharacterRegistrySO.characters 목록에 등록해야 StageDirector에서 참조 가능.
 * =========================================================
 */
#endif

using System.Collections.Generic;
using HInspector;
using UnityEngine;

namespace HDialogue {
    [CreateAssetMenu(menuName = "HWindows/Dialogue/Character Portrait Set")]
    public sealed class CharacterPortraitSetSO : ScriptableObject {
        #region Fields
        [HTitle("Identity")]
        [SerializeField]
        string characterKey;
        [SerializeField]
        string displayName;

        [HTitle("Poses")]
        [SerializeField]
        string defaultPoseKey = "neutral";
        [SerializeField]
        List<PortraitPose> poses = new();

        [HTitle("Layout")]
        [SerializeField]
        Vector2 pivotOffset;
        [SerializeField]
        FacingDirection defaultFacing = FacingDirection.Right;
        #endregion

        #region Properties
        public string CharacterKey => characterKey;
        public string DisplayName => displayName;
        public string DefaultPoseKey => defaultPoseKey;
        public IReadOnlyList<PortraitPose> Poses => poses;
        public Vector2 PivotOffset => pivotOffset;
        public FacingDirection DefaultFacing => defaultFacing;
        #endregion

        public bool TryGetPose(string key, out PortraitPose pose) {
            foreach (PortraitPose p in poses) {
                if (p.Key == key) {
                    pose = p;
                    return true;
                }
            }
            pose = default;
            return false;
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 CharacterPortraitSetSO 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 4-A — 캐릭터별 포즈 집합 에셋 타입
 *
 * # 설계 결정
 * - TryGetPose: O(n) linear search. 포즈 최대 ~15개 예상으로 Dictionary 오버헤드 불필요.
 * - sealed: CreateAssetMenu menuName 오염 방지.
 *
 * =============================================================================
 */
#endif
