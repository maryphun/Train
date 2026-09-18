using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Train.Battle
{
    // These are battle-local values. Lasting character status is represented separately below.
    public enum BattleDamageType
    {
        Physical,
        Pleasure,
        Confusion
    }

    public enum BattleOutcome
    {
        InProgress,
        MonsterVictory,
        TurnLimitReached,
        HeroineVictory
    }

    [Serializable]
    public sealed class BattleCharacter
    {
        public string id;
        public string displayName;
        public Sprite portrait;

        public BattleCharacter Copy()
        {
            return (BattleCharacter)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class BattleSkill
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        [Min(0)] public int energyDamage;
        [Min(0)] public int physicalDamage;
        [Min(0)] public int pleasureDamage;
        [Min(0)] public int confusionDamage;
        public Sprite cutIn;

        public BattleSkill Copy()
        {
            return (BattleSkill)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class BattleHeroineSkill
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        [Min(0)] public int monsterDamage;
        [Min(0)] public int energyCost;
        [Min(0)] public int energyRecovery;
        [Tooltip("Multiplier on all damage from the monster's response this turn. 0.5 halves it.")]
        [Range(0f, 1f)] public float incomingDamageMultiplier = 1f;
        public Sprite cutIn;

        public BattleHeroineSkill Copy()
        {
            return (BattleHeroineSkill)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class BattlePortraitStage
    {
        public string label;
        public BattleDamageType damageType;
        [Min(1)] public int threshold;
        [Min(0)] public int bonusTurns;
        public Sprite heroinePortrait;

        public BattlePortraitStage Copy()
        {
            return (BattlePortraitStage)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class BattleStatusRule
    {
        // The caller owns these IDs. Add, remove, or rename rows without changing battle code.
        public string statusId;
        public string displayName;
        public int startingValue;
        public float physicalDamageRate;
        public float pleasureDamageRate;
        public float confusionDamageRate;

        public BattleStatusRule Copy()
        {
            return (BattleStatusRule)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class BattleSetup
    {
        public BattleCharacter heroine = new BattleCharacter();
        public BattleCharacter monster = new BattleCharacter();
        [Min(1)] public int startingEnergy;
        [Min(1)] public int startingMonsterHealth = 100;
        [Tooltip("Remaining time, measured in completed turns (not real-time seconds).")]
        [Min(1)] public int turnLimit;
        public string returnSceneName;
        [FormerlySerializedAs("defeatStatusMultiplier")]
        [Min(0)] public float heroineVictoryStatusMultiplier = 0.25f;
        [FormerlySerializedAs("victoryStatusMultiplier")]
        [Min(0)] public float heroineDefeatStatusMultiplier = 1f;
        public List<BattleHeroineSkill> heroineSkills = new List<BattleHeroineSkill>();
        [Tooltip("The monster automatically cycles through this list, in order, after each heroine action.")]
        public List<BattleSkill> monsterSkills = new List<BattleSkill>();
        public List<BattlePortraitStage> portraitStages = new List<BattlePortraitStage>();
        public List<BattleStatusRule> statusRules = new List<BattleStatusRule>();

        public BattleSetup Copy()
        {
            var copy = (BattleSetup)MemberwiseClone();
            copy.heroine = heroine?.Copy();
            copy.monster = monster?.Copy();
            copy.heroineSkills = CopyList(heroineSkills, skill => skill.Copy());
            copy.monsterSkills = CopyList(monsterSkills, skill => skill.Copy());
            copy.portraitStages = CopyList(portraitStages, stage => stage.Copy());
            copy.statusRules = CopyList(statusRules, rule => rule.Copy());
            return copy;
        }

        private static List<T> CopyList<T>(List<T> source, Func<T, T> copyItem) where T : class
        {
            var result = new List<T>();
            if (source == null) return result;
            foreach (T item in source) result.Add(item == null ? null : copyItem(item));
            return result;
        }

        public void Validate()
        {
            if (heroine == null || string.IsNullOrWhiteSpace(heroine.displayName))
                throw new ArgumentException("BattleSetup requires one magical girl with a display name.");
            if (monster == null || string.IsNullOrWhiteSpace(monster.displayName))
                throw new ArgumentException("BattleSetup requires one monster with a display name.");
            if (startingEnergy <= 0 || startingMonsterHealth <= 0 || turnLimit <= 0)
                throw new ArgumentException("Starting Energy, monster health, and turn limit must be positive.");
            if (heroineVictoryStatusMultiplier < 0 || heroineDefeatStatusMultiplier < 0 ||
                float.IsNaN(heroineVictoryStatusMultiplier) || float.IsInfinity(heroineVictoryStatusMultiplier) ||
                float.IsNaN(heroineDefeatStatusMultiplier) || float.IsInfinity(heroineDefeatStatusMultiplier))
                throw new ArgumentException("Status multipliers cannot be negative.");
            if (heroineSkills == null || heroineSkills.Count == 0)
                throw new ArgumentException("BattleSetup requires at least one heroine skill.");
            if (monsterSkills == null || monsterSkills.Count == 0)
                throw new ArgumentException("BattleSetup requires at least one monster skill.");

            var skillIds = new HashSet<string>(StringComparer.Ordinal);
            bool hasFreeSkill = false;
            foreach (BattleHeroineSkill skill in heroineSkills)
            {
                if (skill == null || string.IsNullOrWhiteSpace(skill.id) ||
                    string.IsNullOrWhiteSpace(skill.displayName) || !skillIds.Add(skill.id))
                    throw new ArgumentException("Every heroine skill needs a unique ID and display name.");
                if (skill.monsterDamage < 0 || skill.energyCost < 0 || skill.energyRecovery < 0 ||
                    float.IsNaN(skill.incomingDamageMultiplier) ||
                    skill.incomingDamageMultiplier < 0f || skill.incomingDamageMultiplier > 1f)
                    throw new ArgumentException("Heroine skill values must be nonnegative; incoming damage multiplier must be between 0 and 1.");
                hasFreeSkill |= skill.energyCost == 0;
            }
            if (!hasFreeSkill)
                throw new ArgumentException("Keep at least one heroine skill with zero Energy cost so low Energy cannot block all actions.");

            skillIds.Clear();
            foreach (BattleSkill skill in monsterSkills)
            {
                if (skill == null || string.IsNullOrWhiteSpace(skill.id) ||
                    string.IsNullOrWhiteSpace(skill.displayName) || !skillIds.Add(skill.id))
                    throw new ArgumentException("Every monster skill needs a unique ID and display name.");
                if (skill.energyDamage < 0 || skill.physicalDamage < 0 ||
                    skill.pleasureDamage < 0 || skill.confusionDamage < 0)
                    throw new ArgumentException("Skill damage values cannot be negative.");
            }

            if (portraitStages != null)
            {
                foreach (BattlePortraitStage stage in portraitStages)
                {
                    if (stage == null || stage.threshold <= 0 || stage.bonusTurns < 0)
                        throw new ArgumentException("Portrait stages need a positive threshold and nonnegative bonus turns.");
                }
            }

            var statusIds = new HashSet<string>(StringComparer.Ordinal);
            if (statusRules == null) return;
            foreach (BattleStatusRule rule in statusRules)
            {
                if (rule == null || string.IsNullOrWhiteSpace(rule.statusId) ||
                    string.IsNullOrWhiteSpace(rule.displayName) || !statusIds.Add(rule.statusId))
                    throw new ArgumentException("Every character status needs a unique ID and display name.");
            }
        }
    }

    public sealed class BattleStatusChange
    {
        public string statusId;
        public string displayName;
        public int before;
        public int delta;
        public int after;
    }

    public sealed class BattleResult
    {
        public BattleOutcome outcome;
        public string heroineId;
        public string monsterId;
        public int turnsUsed;
        public int turnsRemaining;
        public int energyRemaining;
        public int monsterHealthRemaining;
        public int physicalDamage;
        public int pleasureDamage;
        public int confusionDamage;
        public List<BattleStatusChange> statusChanges;

        public bool MonsterWon => outcome == BattleOutcome.MonsterVictory;
        public bool HeroineWon => outcome == BattleOutcome.HeroineVictory || outcome == BattleOutcome.TurnLimitReached;
    }

    public sealed class BattleTurnReport
    {
        public BattleHeroineSkill heroineSkill;
        // Null when the heroine defeats the monster before it can respond.
        public BattleSkill monsterSkill;
        public int energyBefore;
        public int energyAfter;
        public int energyRecovered;
        public int incomingEnergyDamage;
        public int monsterHealthBefore;
        public int monsterHealthAfter;
        public int turnsRemaining;
        public List<BattlePortraitStage> reachedStages;
        public BattleOutcome outcome;
    }
}
