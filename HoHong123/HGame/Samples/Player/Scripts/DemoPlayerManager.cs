using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HUtil.Inspector;
using HGame.Player;

namespace HGame.Sample.Player {
    public sealed class DemoPlayerManager : MonoBehaviour {
        #region Serialized Fields
        [HTitle("Player")]
        [SerializeField]
        PlayerConfig config;
        [SerializeField]
        PlayerRefSO playerRef;

        [HTitle("HP UI")]
        [SerializeField]
        TMP_Text hpTxt;
        [SerializeField]
        Slider hpSlid;

        [HTitle("EXP UI")]
        [SerializeField]
        TMP_Text levelTxt;
        [SerializeField]
        TMP_Text expTxt;
        [SerializeField]
        Slider expSlid;

        [HTitle("Attack Presentation UI")]
        [SerializeField]
        TMP_Text attackCooldownTxt;
        [SerializeField]
        Slider attackCooldownSlid;
        [SerializeField]
        TMP_Text attackDamageTxt;

        [HTitle("Ultimate Presentation UI")]
        [SerializeField]
        TMP_Text ultCooldownTxt;
        [SerializeField]
        Slider ultCooldownSlid;
        [SerializeField]
        TMP_Text ultDamageTxt;

        [HTitle("Hit / Heal Presentation UI")]
        [SerializeField]
        TMP_Text hitDamageTxt;
        [SerializeField]
        TMP_Text healAmountTxt;
        [SerializeField]
        TMP_Text eventFeedTxt;

        [HTitle("Presentation Control")]
        [SerializeField]
        float eventMessageDuration = 1.5f;
        #endregion

        #region Internal Fields
        PlayerStatus playerStatus;
        float attackCooldownLeft;
        float ultCooldownLeft;

        Coroutine attackDamageRoutine;
        Coroutine ultDamageRoutine;
        Coroutine hitDamageRoutine;
        Coroutine healAmountRoutine;
        Coroutine eventFeedRoutine;
        #endregion

        #region Properties
        public PlayerStatus PlayerStatus => playerStatus;
        #endregion

        #region Initialization
        private void _InitializePlayer() {
            playerStatus = new PlayerStatus();
            playerStatus.Init(config);
            playerRef.Set(playerStatus);
        }

        private void _InitializeUi() {
            _RefreshHpUi(playerStatus.Hp);
            _RefreshExpUi(playerStatus.Level, playerStatus.Exp, playerStatus.ExpToNext);

            _RefreshAttackCooldownUi(0f);
            _RefreshUltCooldownUi(0f);

            _SetText(attackDamageTxt, "ATK DMG: -");
            _SetText(ultDamageTxt, "ULT DMG: -");
            _SetText(hitDamageTxt, "HIT: -");
            _SetText(healAmountTxt, "HEAL: -");
            _SetText(eventFeedTxt, string.Empty);
        }
        #endregion

        #region Unity Life Cycle
        private void Awake() {
            if (!_ValidateReferences()) return;

            _InitializePlayer();
            _InitializeUi();
        }

        private void OnEnable() {
            _BindPlayerEvents();
        }

        private void OnDisable() {
            _UnbindPlayerEvents();
        }

        private void Update() {
            _TickCooldown();
        }
        #endregion

        #region Public Methods
        public void DebugGainExp(float gainExp) {
            if (playerStatus == null) return;
            playerStatus.GainExp(gainExp);
        }

        public void DebugTakeDamage(int damage) {
            if (playerStatus == null) return;
            playerStatus.TakeDamage(damage);
        }

        public void DebugHeal(int heal) {
            if (playerStatus == null) return;
            playerStatus.Heal(heal);
        }

        public void DebugAttack() {
            if (playerStatus == null) return;
            if (attackCooldownLeft > 0f) {
                _ShowEvent($"공격 쿨타임 {attackCooldownLeft:0.0}s");
                return;
            }

            playerStatus.Attack();
        }

        public void DebugUlt() {
            if (playerStatus == null) return;
            if (ultCooldownLeft > 0f) {
                _ShowEvent($"궁극기 쿨타임 {ultCooldownLeft:0.0}s");
                return;
            }

            playerStatus.UseUlt();
        }
        #endregion

        #region Private Methods
        private void _BindPlayerEvents() {
            if (playerStatus == null) return;

            playerStatus.OnLevelUp += _OnLevelUp;
            playerStatus.OnExpChanged += _OnExpChanged;
            playerStatus.OnDamage += _OnDamage;
            playerStatus.OnHeal += _OnHeal;
            playerStatus.OnHpChanged += _OnHpChanged;
            playerStatus.OnAttack += _OnAttack;
            playerStatus.OnUltUsed += _OnUlt;
            playerStatus.OnDeath += _OnDeath;
        }

        private void _UnbindPlayerEvents() {
            if (playerStatus == null) return;

            playerStatus.OnLevelUp -= _OnLevelUp;
            playerStatus.OnExpChanged -= _OnExpChanged;
            playerStatus.OnDamage -= _OnDamage;
            playerStatus.OnHeal -= _OnHeal;
            playerStatus.OnHpChanged -= _OnHpChanged;
            playerStatus.OnAttack -= _OnAttack;
            playerStatus.OnUltUsed -= _OnUlt;
            playerStatus.OnDeath -= _OnDeath;
        }

        private void _RefreshHpUi(int hp) {
            if (hpSlid != null) {
                hpSlid.maxValue = playerStatus.MaxHp;
                hpSlid.value = hp;
            }

            _SetText(hpTxt, $"HP: {hp}/{playerStatus.MaxHp}");
        }

        private void _RefreshExpUi(int level, float exp, float expToNext) {
            _SetText(levelTxt, $"Lv. {level}");
            _SetText(expTxt, $"EXP: {exp:0}/{expToNext:0}");

            if (expSlid == null) return;
            expSlid.maxValue = Mathf.Max(1f, expToNext);
            expSlid.value = Mathf.Clamp(exp, 0f, expSlid.maxValue);
        }

        private void _RefreshAttackCooldownUi(float remainTime) {
            float max = Mathf.Max(0.01f, config.AttackCooldown);
            float normalized = 1f - Mathf.Clamp01(remainTime / max);

            if (attackCooldownSlid != null) {
                attackCooldownSlid.maxValue = 1f;
                attackCooldownSlid.value = normalized;
            }

            _SetText(attackCooldownTxt, $"ATK CD: {remainTime:0.0}s");
        }

        private void _RefreshUltCooldownUi(float remainTime) {
            float max = Mathf.Max(0.01f, config.SpecialCooldown);
            float normalized = 1f - Mathf.Clamp01(remainTime / max);

            if (ultCooldownSlid != null) {
                ultCooldownSlid.maxValue = 1f;
                ultCooldownSlid.value = normalized;
            }

            _SetText(ultCooldownTxt, $"ULT CD: {remainTime:0.0}s");
        }

        private void _TickCooldown() {
            if (attackCooldownLeft > 0f) {
                attackCooldownLeft = Mathf.Max(0f, attackCooldownLeft - Time.deltaTime);
                _RefreshAttackCooldownUi(attackCooldownLeft);
            }

            if (ultCooldownLeft <= 0f) return;
            ultCooldownLeft = Mathf.Max(0f, ultCooldownLeft - Time.deltaTime);
            _RefreshUltCooldownUi(ultCooldownLeft);
        }

        private void _ShowEvent(string message) {
            _SetText(eventFeedTxt, message);

            if (eventFeedRoutine != null) StopCoroutine(eventFeedRoutine);
            eventFeedRoutine = StartCoroutine(_CoClearEventText());
        }

        private IEnumerator _CoShowTempText(TMP_Text target, string message) {
            if (target == null) yield break;

            target.gameObject.SetActive(true);
            target.text = message;

            yield return new WaitForSeconds(eventMessageDuration);

            target.text = string.Empty;
            target.gameObject.SetActive(false);
        }

        private IEnumerator _CoClearEventText() {
            yield return new WaitForSeconds(eventMessageDuration);
            _SetText(eventFeedTxt, string.Empty);
            eventFeedRoutine = null;
        }

        private void _PlayTempText(ref Coroutine cache, TMP_Text target, string message) {
            if (cache != null) StopCoroutine(cache);
            cache = StartCoroutine(_CoShowTempText(target, message));
        }

        private int _RollDamage() {
            if (config.RollCrit(out float rate)) return Mathf.RoundToInt(config.RollBaseDamage() * rate);
            return config.RollBaseDamage();
        }

        private void _SetText(TMP_Text target, string value) {
            if (target == null) return;
            target.text = value;
        }

        private bool _ValidateReferences() {
            if (config != null && playerRef != null) return true;

            Debug.LogError($"[{nameof(DemoPlayerManager)}] PlayerConfig / PlayerRefSO는 반드시 연결되어야 합니다.", this);
            enabled = false;
            return false;
        }
        #endregion

        #region Player Events
        private void _OnLevelUp(int level) {
            _RefreshExpUi(level, playerStatus.Exp, playerStatus.ExpToNext);
            _ShowEvent($"레벨업! Lv.{level}");
        }

        private void _OnExpChanged(float exp, float nextExp) {
            _RefreshExpUi(playerStatus.Level, exp, nextExp);
        }

        private void _OnHpChanged(int hp) {
            _RefreshHpUi(hp);
        }

        private void _OnDamage(int damage) {
            _PlayTempText(ref hitDamageRoutine, hitDamageTxt, $"-{damage}");
            _ShowEvent($"피격: {damage}");
        }

        private void _OnHeal(int heal) {
            _PlayTempText(ref healAmountRoutine, healAmountTxt, $"+{heal}");
            _ShowEvent($"회복: {heal}");
        }

        private void _OnAttack() {
            attackCooldownLeft = config.AttackCooldown;
            _RefreshAttackCooldownUi(attackCooldownLeft);

            int damage = _RollDamage();
            _PlayTempText(ref attackDamageRoutine, attackDamageTxt, $"ATK {damage}");
            _ShowEvent($"공격 데미지: {damage}");
        }

        private void _OnUlt() {
            ultCooldownLeft = config.SpecialCooldown;
            _RefreshUltCooldownUi(ultCooldownLeft);

            int damage = Mathf.RoundToInt(_RollDamage() * 2.5f);
            _PlayTempText(ref ultDamageRoutine, ultDamageTxt, $"ULT {damage}");
            _ShowEvent($"궁극기 데미지: {damage}");
        }

        private void _OnDeath() {
            _ShowEvent("플레이어 사망");
        }
        #endregion

#if UNITY_EDITOR
        #region Debug
        [ContextMenu("[Debug] Gain 50 Exp")]
        private void _DebugGainExp50() {
            DebugGainExp(50f);
        }

        [ContextMenu("[Debug] Take 10 Damage")]
        private void _DebugTakeDamage10() {
            DebugTakeDamage(10);
        }

        [ContextMenu("[Debug] Heal 10")]
        private void _DebugHeal10() {
            DebugHeal(10);
        }

        [ContextMenu("[Debug] Attack")]
        private void _DebugAttack() {
            DebugAttack();
        }

        [ContextMenu("[Debug] Ult")]
        private void _DebugUlt() {
            DebugUlt();
        }
        #endregion
#endif
    }
}
