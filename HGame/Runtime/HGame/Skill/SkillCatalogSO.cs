using System.Collections.Generic;
using UnityEngine;
using HInspector;

namespace HGame.Skill {
    [CreateAssetMenu(
        fileName = "SkillCatalog",
        menuName = "HCUP/Skill/Catalog",
        order = 0)]
    public class SkillCatalogSO : ScriptableObject {
        [HTitle("Skills")]
        [SerializeField]
        List<BaseSkillSO> skills = new();

        public List<BaseSkillSO> Skills => skills;
    }
}
