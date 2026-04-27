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
