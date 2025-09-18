#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 게임 오브젝트와 데모 전역 풀링시스템 매니지먼트 시스템을 연결하는 컴포넌트 스크립트입니다.
 * =========================================================
 */
#endif

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Util.Pooling {
    public class UnityPoolConnector : MonoBehaviour {
        [Title("Entities")]
        [InfoBox("Must use 'IPoolable' prefab", InfoMessageType.Warning)]
        [SerializeField]
        List<UnityPoolEntity> poolEntity = new();


        private void Awake() {
            foreach (var entity in poolEntity) {
                UnityPoolMaster.Register(entity);
            }
        }

        private void OnDestroy() {
            foreach (var entity in poolEntity) {
                UnityPoolMaster.Remove(entity);
            }
        }
    }
}

#if UNITY_EDITOR
/* @Jason
 * Created at May. 29. 2025
 * 1. Create for example.
 */
#endif