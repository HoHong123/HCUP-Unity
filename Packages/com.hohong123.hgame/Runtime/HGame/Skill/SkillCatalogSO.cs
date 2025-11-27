using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace HGame.Skill {
    [CreateAssetMenu(
        fileName = "SkillCatalog",
        menuName = "Game/Skill/Catalog",
        order = 0)]
    public class SkillCatalogSO : ScriptableObject {
        [Title("Skills")]
        [SerializeField]
        List<BaseSkillSO> skills = new();

        public List<BaseSkillSO> Skills => skills;
    }
}
