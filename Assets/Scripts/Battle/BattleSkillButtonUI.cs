using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Train.Battle
{
    [RequireComponent(typeof(Button))]
    public sealed class BattleSkillButtonUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text values;

        private UnityAction clickHandler;
        public Button Button => GetComponent<Button>();

        internal void ValidateReferences()
        {
            if (label == null)
                throw new InvalidOperationException("Assign Label on the BattleSkillButtonUI template.");
        }

        internal void Bind(BattleHeroineSkill skill, Action onClick)
        {
            ValidateReferences();
            label.text = skill.displayName;
            if (values != null)
            {
                var text = new StringBuilder();
                if (skill.monsterDamage > 0) text.Append($"体力 -{skill.monsterDamage}\n");
                if (skill.energyCost > 0) text.Append($"エナジー -{skill.energyCost}\n");
                if (skill.energyRecovery > 0) text.Append($"エナジー +{skill.energyRecovery}\n");
                if (skill.incomingDamageMultiplier < 1f)
                    text.Append($"被ダメージ ×{skill.incomingDamageMultiplier:0.##}");
                values.text = text.ToString().TrimEnd();
            }
            if (clickHandler != null) Button.onClick.RemoveListener(clickHandler);
            clickHandler = () => onClick();
            Button.onClick.AddListener(clickHandler);
        }

        private void OnDestroy()
        {
            if (clickHandler != null && Button != null) Button.onClick.RemoveListener(clickHandler);
        }
    }
}
