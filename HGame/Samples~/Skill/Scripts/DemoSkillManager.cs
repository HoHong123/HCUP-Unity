using System;
using System.Text;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HCore;
using HInspector;
using HDiagnosis.Logger;

namespace HGame.Skill.Sample {
    public sealed class DemoSkillManager : SingletonBehaviour<DemoSkillManager> {
        #region Serialized Fields
        [HTitle("References")]
        [SerializeField]
        SkillCatalogSO catalog;
        [SerializeField]
        SkillStats stats;

        [HTitle("Flow")]
        [SerializeField, Min(1)]
        int offerCount = SkillConst.SKILL_CHOICE_COUNT;
        [SerializeField]
        bool rollOnStart = true;

        [HTitle("Canvas UI")]
        [SerializeField]
        Button rollOffersButton;
        [SerializeField]
        Button applyFirstOfferButton;
        [SerializeField]
        Button applySecondOfferButton;
        [SerializeField]
        Button applyThirdOfferButton;
        [SerializeField]
        Button resetAllButton;
        [SerializeField]
        TMP_Text offersText;
        [SerializeField]
        TMP_Text stacksText;
        [SerializeField]
        TMP_Text statsText;
        [SerializeField]
        bool autoBindButtons = true;

#if UNITY_EDITOR
        [HTitle("Debug Preview")]
        [SerializeField, HReadOnly]
        string currentOffersPreview;
        [SerializeField, HReadOnly]
        string currentStacksPreview;
        [SerializeField, HReadOnly]
        string currentStatsPreview;
#endif

        readonly List<SkillOffer> currentOffers = new();
        readonly Dictionary<BaseSkillSO, int> stacks = new();
        readonly StringBuilder textBuilder = new();
        #endregion

        #region Event Delegates
        public event Action<IReadOnlyList<SkillOffer>> OnOffersRolled;
        public event Action<BaseSkillSO, SkillRarity, int> OnSkillApplied;
        #endregion

        #region Properties
        public IReadOnlyList<SkillOffer> CurrentOffers => currentOffers;
        public IReadOnlyDictionary<BaseSkillSO, int> CurrentStacks => stacks;
        #endregion

        #region Initialization
        public void Initialize(bool shouldRollOffer) {
            _ValidateReferencesOrThrow();
            _RefreshAllViews();
            if (shouldRollOffer) RollOffers();
        }
        #endregion

        #region Unity Life Cycle
        protected override void Awake() {
            base.Awake();
            Initialize(rollOnStart);
        }

        private void OnEnable() {
            if (!autoBindButtons) return;
            _BindButtons();
        }

        private void OnDisable() {
            if (!autoBindButtons) return;
            _UnbindButtons();
        }
        #endregion

        #region Public Methods
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#else
        [ContextMenu("")]
#endif
        public void RollOffers() {
            currentOffers.Clear();

            int guard = 200;
            while (currentOffers.Count < offerCount && guard-- > 0) {
                BaseSkillSO candidate = _PickRandomSkill();
                if (candidate == null) break;
                if (_ContainsOffer(candidate)) continue;

                SkillRarity rarity = candidate.TryGetFixedRarity(out SkillRarity fixedRarity)
                    ? fixedRarity
                    : _GetRandomRarity();

                int stack = _GetCurrentStack(candidate);
                if (!candidate.CanOffer(stats, stack)) continue;

                currentOffers.Add(new SkillOffer(candidate, rarity));
            }

            _RefreshAllViews();
            OnOffersRolled?.Invoke(currentOffers);
        }

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#else
        [ContextMenu("")]
#endif
        public bool ApplyOfferByIndex(int offerIndex) {
            if (offerIndex < 0 || offerIndex >= currentOffers.Count)
                return false;

            SkillOffer offer = currentOffers[offerIndex];
            BaseSkillSO skill = offer.Skill;
            if (skill == null) return false;

            int currentStack = _GetCurrentStack(skill);
            if (!skill.CanOffer(stats, currentStack)) return false;

            skill.ApplyWithRarity(stats, offer.Rarity, ref currentStack);
            stacks[skill] = currentStack;

            _RefreshAllViews();
            OnSkillApplied?.Invoke(skill, offer.Rarity, currentStack);
            return true;
        }

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#else
        [ContextMenu("ApplyFirstOffer")]
#endif
        public void ApplyFirstOffer() => ApplyOfferByIndex(0);

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#else
        [ContextMenu("ApplySecondOffer")]
#endif
        public void ApplySecondOffer() => ApplyOfferByIndex(1);

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#else
        [ContextMenu("ApplyThirdOffer")]
#endif
        public void ApplyThirdOffer() => ApplyOfferByIndex(2);

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#else
        [ContextMenu("ResetAll")]
#endif
        public void ResetAll() {
            stacks.Clear();
            currentOffers.Clear();
            stats.ResetAll();
            _RefreshAllViews();
        }
#endregion

        #region Private Methods
        private void _BindButtons() {
            rollOffersButton.onClick.AddListener(RollOffers);
            applyFirstOfferButton.onClick.AddListener(ApplyFirstOffer);
            applySecondOfferButton.onClick.AddListener(ApplySecondOffer);
            applyThirdOfferButton.onClick.AddListener(ApplyThirdOffer);
            resetAllButton.onClick.AddListener(ResetAll);
        }

        private void _UnbindButtons() {
            rollOffersButton.onClick.RemoveListener(RollOffers);
            applyFirstOfferButton.onClick.RemoveListener(ApplyFirstOffer);
            applySecondOfferButton.onClick.RemoveListener(ApplySecondOffer);
            applyThirdOfferButton.onClick.RemoveListener(ApplyThirdOffer);
            resetAllButton.onClick.RemoveListener(ResetAll);
        }

        private void _RefreshAllViews() {
            string offers = _BuildOffersText();
            string stackState = _BuildStacksText();
            string statsState = _BuildStatsText();

            _UpdateCanvasText(offersText, offers);
            _UpdateCanvasText(stacksText, stackState);
            _UpdateCanvasText(statsText, statsState);

#if UNITY_EDITOR
            currentOffersPreview = offers;
            currentStacksPreview = stackState;
            currentStatsPreview = statsState;
#endif
        }

        private void _UpdateCanvasText(TMP_Text targetText, string value) {
            if (targetText == null) return;
            targetText.text = value;
        }

        private BaseSkillSO _PickRandomSkill() {
            List<BaseSkillSO> pool = catalog.Skills;
            if (pool == null || pool.Count == 0) return null;
            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        private bool _ContainsOffer(BaseSkillSO skill) {
            for (int k = 0; k < currentOffers.Count; k++) {
                if (currentOffers[k].Skill == skill) return true;
            }
            return false;
        }

        private int _GetCurrentStack(BaseSkillSO skill) {
            if (skill == null) return 0;
            return stacks.TryGetValue(skill, out int currentStack) ? currentStack : 0;
        }

        private SkillRarity _GetRandomRarity() {
            float sum = 0f;
            for (int k = 0; k < SkillConst.RarityWeights.Length; k++) {
                sum += SkillConst.RarityWeights[k];
            }

            if (sum <= 0f) return SkillRarity.Normal;

            float roll = UnityEngine.Random.Range(0f, sum);
            for (int k = 0; k < SkillConst.RarityWeights.Length; k++) {
                float weight = SkillConst.RarityWeights[k];
                if (roll < weight) return (SkillRarity)k;
                roll -= weight;
            }

            return SkillRarity.Epic;
        }

        private void _ValidateReferencesOrThrow() {
            if (catalog == null)
                HLogger.Throw(new InvalidOperationException($"[{nameof(DemoSkillManager)}] catalog reference is null."));
            if (stats == null)
                HLogger.Throw(new InvalidOperationException($"[{nameof(DemoSkillManager)}] stats reference is null."));
        }

        private string _BuildOffersText() {
            if (currentOffers.Count == 0) return "(No offers)";

            textBuilder.Clear();
            for (int k = 0; k < currentOffers.Count; k++) {
                SkillOffer offer = currentOffers[k];
                string name = offer.Skill == null ? "<Null Skill>" : offer.Skill.DisplayName;
                textBuilder.Append(k)
                    .Append(". ")
                    .Append(name)
                    .Append(" [")
                    .Append(offer.Rarity)
                    .AppendLine("]");
            }

            return textBuilder.ToString().TrimEnd();
        }

        private string _BuildStacksText() {
            if (stacks.Count == 0) return "(No stacks)";

            textBuilder.Clear();
            foreach (KeyValuePair<BaseSkillSO, int> pair in stacks) {
                if (pair.Key == null) continue;
                textBuilder.Append(pair.Key.DisplayName)
                    .Append(": ")
                    .AppendLine(pair.Value.ToString());
            }

            return textBuilder.Length == 0 ? "(No stacks)" : textBuilder.ToString().TrimEnd();
        }

        private string _BuildStatsText() {
            textBuilder.Clear();
            textBuilder
                .Append("ATK x")
                .Append(stats.AttackMul.ToString("0.###"))
                .Append(" | SPD x")
                .Append(stats.AttackSpeedMul.ToString("0.###"))
                .Append(" | ULT x")
                .Append(stats.UltCooldownMul.ToString("0.###"))
                .Append(" | KB x")
                .Append(stats.KnockbackMul.ToString("0.###"))
                .AppendLine()
                .Append("Explosive: ")
                .Append(stats.EnableExplosive ? "ON" : "OFF")
                .Append(" | Chance ")
                .Append((stats.ExplosiveChance * 100f).ToString("0.#"))
                .Append("% | DMG x")
                .Append(stats.ExplosiveDamageMul.ToString("0.###"))
                .Append(" | Radius x")
                .Append(stats.ExplosiveRadiusMul.ToString("0.###"));
            return textBuilder.ToString();
        }
        #endregion
    }
}
