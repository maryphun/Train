using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Train.Battle
{
    /// <summary>Inspector connections for a battle Canvas designed in the scene.</summary>
    [DisallowMultipleComponent]
    public sealed class BattleUIReferences : MonoBehaviour
    {
        [Header("Background (required; stretch this Image across the Canvas)")]
        public Image backgroundImage;

        [Header("Characters (optional)")]
        public Image heroineImage;
        public Image monsterImage;
        public TMP_Text heroineNameText;
        public TMP_Text monsterNameText;

        [Header("Gauges (optional; Images must use Filled type)")]
        public Image energyFill;
        public Image monsterHealthFill;
        public TMP_Text energyText;
        public TMP_Text monsterHealthText;
        public TMP_Text turnsText;

        [Header("Additional information (optional)")]
        public TMP_Text monsterIntentText;
        public TMP_Text damageText;
        public TMP_Text projectedStatusText;
        public TMP_Text logText;

        [Header("Action pop-up (required)")]
        public GameObject actionPopup;
        [Tooltip("Use a Layout Group for button arrangement and Content Size Fitter for window sizing.")]
        public Transform skillButtonParent;
        [Tooltip("A prefab, or an inactive scene template excluded from the visible button layout.")]
        public BattleSkillButtonUI skillButtonTemplate;
        public TMP_Text skillsHeadingText;

        [Header("Skill presentation (optional)")]
        public SpellAnimation spellAnimation;
        [Tooltip("Fallback for the prototype cut-in. Used only when Spell Animation is empty.")]
        public Image cutInImage;

        [Header("Results (overlay and return button required)")]
        public GameObject resultOverlay;
        public TMP_Text resultTitle;
        public TMP_Text resultStatusText;
        public Button returnButton;

        internal void ValidateReferences()
        {
            if (backgroundImage == null)
                throw new InvalidOperationException("Assign Background Image on BattleUIReferences.");
            if (actionPopup == null || skillButtonParent == null || skillButtonTemplate == null)
                throw new InvalidOperationException("Assign Action Popup, Skill Button Parent and Skill Button Template on BattleUIReferences.");
            if (resultOverlay == null || returnButton == null)
                throw new InvalidOperationException("Assign Result Overlay and Return Button on BattleUIReferences.");
            if (energyFill != null && energyFill.type != Image.Type.Filled ||
                monsterHealthFill != null && monsterHealthFill.type != Image.Type.Filled)
                throw new InvalidOperationException("Set the scene Energy Fill and Monster Health Fill Images to Filled type.");
            if (!skillButtonParent.IsChildOf(actionPopup.transform) && skillButtonParent != actionPopup.transform)
                throw new InvalidOperationException("Skill Button Parent must belong to Action Popup.");
            if (skillButtonParent == skillButtonTemplate.transform ||
                skillButtonParent.IsChildOf(skillButtonTemplate.transform))
                throw new InvalidOperationException("Skill Button Parent must be a container outside the inactive Skill Button Template.");
            if (skillButtonTemplate.gameObject.scene.IsValid() && skillButtonTemplate.gameObject.activeSelf)
                throw new InvalidOperationException("Keep a scene Skill Button Template inactive; the controller creates the visible skill buttons.");
            skillButtonTemplate.ValidateReferences();
            if (spellAnimation != null) spellAnimation.ValidateReferences();
        }
    }
}
