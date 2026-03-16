using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HUtil.Inspector;
using HGame.Player;

namespace HGame.Sample.Player {
    public class DemoPlayerManager : MonoBehaviour {
        #region Fields
        [HTitle("Player")]
        [SerializeField]
        PlayerConfig config;
        [SerializeField]
        PlayerRefSO playerRef;

        PlayerStatus playerStatus;

        [HTitle("UI")]
        [SerializeField]
        TMP_Text hpTxt;
        [SerializeField]
        Slider hpSlid;

        [SerializeField]
        TMP_Text levelTxt;
        [SerializeField]
        TMP_Text expTxt;
        [SerializeField]
        Slider expSlid;
        #endregion

        private void Start() {
            playerStatus = new();
            playerRef.Set(playerStatus);

            hpTxt.text = config.BaseHp.ToString();
            hpSlid.maxValue = config.BaseHp;
            hpSlid.value = config.BaseHp;

            expTxt.text = "0";
            expSlid.value = 0;
            expSlid.maxValue = config.GetRequiredExpForLevel(1);
        }


        #region Player Events
        private void _OnLevelUp() {
        }

        private void _OnExpChanged() {
        }

        private void _OnDamage() {
        }

        private void _OnHeal() {
        }

        private void _OnAttack() {
        }

        private void _OnUlt() {
        }

        private void _OnDeath() {
        }
        #endregion
    }
}
