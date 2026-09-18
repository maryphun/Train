using System;
using System.Collections.Generic;
using UnityEngine;

namespace Train.Battle
{
    // No scene, profile, or UI dependencies: the same setup can be tested without loading Battle.unity.
    public sealed class BattleSession
    {
        private readonly BattleSetup setup;
        private readonly HashSet<int> reachedStageIndexes = new HashSet<int>();
        private BattleResult result;

        public BattleSetup Setup => setup;
        public int EnergyRemaining { get; private set; }
        public int MonsterHealthRemaining { get; private set; }
        public int TurnsRemaining { get; private set; }
        public int TurnsUsed { get; private set; }
        public int PhysicalDamage { get; private set; }
        public int PleasureDamage { get; private set; }
        public int ConfusionDamage { get; private set; }
        public Sprite HeroinePortrait { get; private set; }
        public BattleOutcome Outcome { get; private set; } = BattleOutcome.InProgress;
        public BattleResult Result => result;

        public BattleSession(BattleSetup source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            source.Validate();
            setup = source.Copy();
            EnergyRemaining = setup.startingEnergy;
            MonsterHealthRemaining = setup.startingMonsterHealth;
            TurnsRemaining = setup.turnLimit;
            HeroinePortrait = setup.heroine.portrait;
        }

        public BattleSkill NextMonsterSkill => Outcome == BattleOutcome.InProgress
            ? setup.monsterSkills[TurnsUsed % setup.monsterSkills.Count] : null;

        public bool CanUseHeroineSkill(int skillIndex)
        {
            return Outcome == BattleOutcome.InProgress && skillIndex >= 0 &&
                skillIndex < setup.heroineSkills.Count &&
                setup.heroineSkills[skillIndex].energyCost < EnergyRemaining;
        }

        // One chosen heroine action and one automatic monster response form one turn.
        public BattleTurnReport UseHeroineSkill(int skillIndex)
        {
            if (Outcome != BattleOutcome.InProgress)
                throw new InvalidOperationException("This battle has already ended.");
            if (skillIndex < 0 || skillIndex >= setup.heroineSkills.Count)
                throw new ArgumentOutOfRangeException(nameof(skillIndex));
            if (!CanUseHeroineSkill(skillIndex))
                throw new InvalidOperationException("Not enough Energy: a skill must leave at least one Energy before recovery.");

            BattleHeroineSkill skill = setup.heroineSkills[skillIndex];
            BattleSkill response = NextMonsterSkill;
            var report = new BattleTurnReport
            {
                heroineSkill = skill,
                energyBefore = EnergyRemaining,
                monsterHealthBefore = MonsterHealthRemaining,
                reachedStages = new List<BattlePortraitStage>()
            };

            EnergyRemaining -= skill.energyCost;
            report.energyRecovered = Mathf.Min(skill.energyRecovery, setup.startingEnergy - EnergyRemaining);
            EnergyRemaining += report.energyRecovered;
            MonsterHealthRemaining = Mathf.Max(0, MonsterHealthRemaining - skill.monsterDamage);
            TurnsUsed++;
            TurnsRemaining--;

            if (MonsterHealthRemaining == 0)
            {
                Finish(BattleOutcome.HeroineVictory);
            }
            else
            {
                report.monsterSkill = response;
                report.incomingEnergyDamage = Mathf.Min(EnergyRemaining,
                    ScaleDamage(response.energyDamage, skill.incomingDamageMultiplier));
                EnergyRemaining -= report.incomingEnergyDamage;
                PhysicalDamage += ScaleDamage(response.physicalDamage, skill.incomingDamageMultiplier);
                PleasureDamage += ScaleDamage(response.pleasureDamage, skill.incomingDamageMultiplier);
                ConfusionDamage += ScaleDamage(response.confusionDamage, skill.incomingDamageMultiplier);
                ApplyPortraitStages(report);

                // Energy depletion takes priority if time also expires on this response.
                if (EnergyRemaining == 0)
                    Finish(BattleOutcome.MonsterVictory);
                else if (TurnsRemaining <= 0)
                    Finish(BattleOutcome.TurnLimitReached);
            }

            report.energyAfter = EnergyRemaining;
            report.monsterHealthAfter = MonsterHealthRemaining;
            report.turnsRemaining = TurnsRemaining;
            report.outcome = Outcome;
            return report;
        }

        private static int ScaleDamage(int damage, float multiplier)
        {
            return Mathf.RoundToInt(damage * multiplier);
        }

        private void ApplyPortraitStages(BattleTurnReport report)
        {
            if (setup.portraitStages != null)
            {
                for (int i = 0; i < setup.portraitStages.Count; i++)
                {
                    BattlePortraitStage stage = setup.portraitStages[i];
                    if (reachedStageIndexes.Contains(i) || GetDamage(stage.damageType) < stage.threshold)
                        continue;

                    reachedStageIndexes.Add(i);
                    TurnsRemaining += stage.bonusTurns;
                    if (stage.heroinePortrait != null) HeroinePortrait = stage.heroinePortrait;
                    report.reachedStages.Add(stage);
                }
            }

        }

        public List<BattleStatusChange> PreviewStatusChanges(BattleOutcome assumedOutcome)
        {
            if (assumedOutcome == BattleOutcome.InProgress)
                throw new ArgumentException("Choose a finished outcome for a status preview.", nameof(assumedOutcome));

            float multiplier = assumedOutcome == BattleOutcome.MonsterVictory
                ? setup.heroineDefeatStatusMultiplier
                : setup.heroineVictoryStatusMultiplier;
            var changes = new List<BattleStatusChange>();
            if (setup.statusRules == null) return changes;

            foreach (BattleStatusRule rule in setup.statusRules)
            {
                float raw = PhysicalDamage * rule.physicalDamageRate
                    + PleasureDamage * rule.pleasureDamageRate
                    + ConfusionDamage * rule.confusionDamageRate;
                int delta = Mathf.RoundToInt(raw * multiplier);
                changes.Add(new BattleStatusChange
                {
                    statusId = rule.statusId,
                    displayName = rule.displayName,
                    before = rule.startingValue,
                    delta = delta,
                    after = rule.startingValue + delta
                });
            }

            return changes;
        }

        private int GetDamage(BattleDamageType type)
        {
            switch (type)
            {
                case BattleDamageType.Physical: return PhysicalDamage;
                case BattleDamageType.Pleasure: return PleasureDamage;
                case BattleDamageType.Confusion: return ConfusionDamage;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private void Finish(BattleOutcome outcome)
        {
            Outcome = outcome;
            result = new BattleResult
            {
                outcome = outcome,
                heroineId = setup.heroine.id,
                monsterId = setup.monster.id,
                turnsUsed = TurnsUsed,
                turnsRemaining = TurnsRemaining,
                energyRemaining = EnergyRemaining,
                monsterHealthRemaining = MonsterHealthRemaining,
                physicalDamage = PhysicalDamage,
                pleasureDamage = PleasureDamage,
                confusionDamage = ConfusionDamage,
                statusChanges = PreviewStatusChanges(outcome)
            };
        }
    }
}
