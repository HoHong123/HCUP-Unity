using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;
using Sirenix.OdinInspector;
using HGame.Player;

namespace HGame.Skill {
    public sealed class SkillManager : HUtil.Core.SingletonBehaviour<SkillManager> {
        [Title("Player")]
        [SerializeField][Required]
        PlayerRefSO playerRef;

        [Title("Skill")]
        [SerializeField][Required]
        SkillCatalogSO catalog;
        [SerializeField][Required]
        SkillStats stats;

        int pendingLevelUps = 0;
        bool processingQueue = false;

        readonly List<SkillOffer> selectedSkills = new(3);
        readonly Dictionary<BaseSkillSO, int> stacks = new();

        public event Action<BaseSkillSO, int> OnStacksChanged;
        
        public IReadOnlyDictionary<BaseSkillSO, int> CurrentStacks => stacks;

        public void OnPrepareGame() {
            playerRef.ReadOnly.OnLevelUp += _OnLevelUp;
            //OnStacksChanged += UI에 변경내용 적용;
        }

        public void OnGameOver() {
            pendingLevelUps = 0;
            processingQueue = false;
            playerRef.ReadOnly.OnLevelUp -= _OnLevelUp;
        }


        public async UniTask OnLevelUpAsync() {
            _GenerateOffers(selectedSkills);
            if (selectedSkills.Count == 0) return;
            int picked = await _ShowChoicesAsync(selectedSkills);
            if (picked < 0 || picked >= selectedSkills.Count) return;

            var (skill, rarity) = (selectedSkills[picked].Skill, selectedSkills[picked].Rarity);

            int currentStack = stacks.TryGetValue(skill, out var stack) ? stack : 0;
            skill.ApplyWithRarity(stats, rarity, ref currentStack);
            stacks[skill] = currentStack;

            OnStacksChanged?.Invoke(skill, currentStack);
        }


        private void _OnLevelUp(int level) {
            pendingLevelUps++;
            if (!processingQueue) _ProcessLevelUpQueueAsync().Forget();
        }

        private void _GenerateOffers(List<SkillOffer> buffer) {
            buffer.Clear();
            int loop = 100;
            while (buffer.Count < SkillConst.SKILL_CHOICE_COUNT && loop-- > 0) {
                var pool = catalog.Skills;
                if (pool.Count == 0) continue;

                var candidate = pool[Random.Range(0, pool.Count)];
                var rarity = candidate.TryGetFixedRarity(out var fixedRarity) ? fixedRarity : _GetRandomRarity();
                int currentStack = stacks.TryGetValue(candidate, out var stack) ? stack : 0;

                if (!candidate.CanOffer(stats, currentStack)) continue;
                if (buffer.Exists(skill => skill.Skill == candidate)) continue;

                buffer.Add(new SkillOffer(candidate, rarity));
            }
        }

        private SkillRarity _GetRandomRarity() {
            float sum = 0f;
            foreach (var weight in SkillConst.RarityWeights) sum += weight;
            float rare = Random.Range(0f, sum);

            int index = 0;
            while (index < SkillConst.RarityWeights.Length) {
                if (rare < SkillConst.RarityWeights[index]) break;
                rare -= SkillConst.RarityWeights[index];
                index++;
            }
            index = Mathf.Clamp(index, 0, 3);

            return (SkillRarity)index;
        }

        private async UniTask<int> _ShowChoicesAsync(List<SkillOffer> offers) {
            // UI를 통해 스킬 선택
            //int picked = await ui.SkillSelect.ShowAsync(offers);
            int picked = 0;
            return picked; // -1 이면 취소로 간주 가능
        }

        private async UniTaskVoid _ProcessLevelUpQueueAsync() {
            processingQueue = true;
            // 게임 일시정지
            //await MaGameManager.Instance.GamePauseAsync();

            try {
                while (pendingLevelUps > 0) {
                    await OnLevelUpAsync();
                    pendingLevelUps--;
                    await UniTask.Yield();
                }
            }
            finally {
                // 게임 재개
                //await MaGameManager.Instance.GameRunAsync();
                processingQueue = false;
            }
        }
    }
}
