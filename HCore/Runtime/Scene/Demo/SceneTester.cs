#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * SceneLoader / BaseSceneManager 동작 확인용 에디터 전용 데모 스크립트입니다.
 *
 * 주의사항 ::
 * 데모 전용이라 #if UNITY_EDITOR 로 가드되어 빌드에서 제외됩니다.
 * =========================================================
 */

using HCore.Scene;
using System.Collections;
using UnityEngine;

public class SceneTester : MonoBehaviour {
    public SceneKey NextScene;
    public float WaitTime;

    private void Start() {
        StartCoroutine(_TestRoutine(WaitTime));
    }

    private IEnumerator _TestRoutine(float duration) {
        Debug.Log($"@@@@ Started at {Time.time}, waiting for {duration} seconds");
        yield return new WaitForSeconds(duration);
        Debug.Log($"@@@@ Ended at {Time.time}");
        BaseSceneManager.Instance.LoadSceneAsync(NextScene);
    }
}

/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.08.07 UNITY_EDITOR 가드 적용
 *
 * # 수정
 * - 별도 asmdef 가 없어 HCore Runtime asmdef 에 포함되어 빌드에 실리던 것을
 *   #if UNITY_EDITOR 로 가드해 빌드에서 제외했다.
 * - 전역 네임스페이스 정리, fire-and-forget LoadSceneAsync 호출은 리포트 범위 밖으로 미착수.
 *
 * =============================================================================
 */
#endif
