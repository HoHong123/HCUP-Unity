using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HGame.Flow;
using HInspector;
using TMPro;
using Cysharp.Threading.Tasks;

namespace HGame.Sample.Module {
    public class DemoGameManager : GameManager<DemoGameManager> {
        [HTitle("Log")]
        [SerializeField]
        TMP_Text logTxt;

        [HTitle("Test Btns")]
        [SerializeField]
        Button prepareBtn;
        [SerializeField]
        Button startBtn;
        [SerializeField]
        Button runningBtn;
        [SerializeField]
        Button pauseBtn;
        [SerializeField]
        Button resumeBtn;
        [SerializeField]
        Button overBtn;
        [SerializeField]
        Button exitBtn;

        [HTitle("Logs")]
        [SerializeField]
        string format;
        [SerializeField, HReadOnly]
        int logStack = 1;
        [SerializeField, HReadOnly]
        List<string> logs = new();


        protected override void Start() {
            base.Start();
            logStack = 1;

            prepareBtn.onClick.AddListener(_OnClickPrepare);
            startBtn.onClick.AddListener(_OnClickStart);
            runningBtn.onClick.AddListener(_OnClickRun);
            pauseBtn.onClick.AddListener(_OnClickPause);
            resumeBtn.onClick.AddListener(_OnClickResume);
            overBtn.onClick.AddListener(_OnClickOver);
            exitBtn.onClick.AddListener(_OnClickExit);
        }


        public void StackLog(string log) {
            logs.Add(log);
            logTxt.text += string.Format(format, logStack++, log);
        }


        private void _OnClickPrepare() => SwitchGamePhaseAsync(GamePhaseType.Prepare).Forget();
        private void _OnClickStart() => SwitchGamePhaseAsync(GamePhaseType.Start).Forget();
        private void _OnClickRun() => SwitchGamePhaseAsync(GamePhaseType.Running).Forget();
        private void _OnClickPause() => SwitchGamePhaseAsync(GamePhaseType.Pause).Forget();
        private void _OnClickResume() => SwitchGamePhaseAsync(GamePhaseType.Resume).Forget();
        private void _OnClickOver() => SwitchGamePhaseAsync(GamePhaseType.Over).Forget();
        private void _OnClickExit() => SwitchGamePhaseAsync(GamePhaseType.Exit).Forget();
    }
}
