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
            TurnsRemaining = setup.turnLimit;
            HeroinePortrait = setup.heroine.portrait;
        }

        // Assumption for the initial version: one monster skill consumes one turn.
        // No magical-girl counteraction is invented; add it here when its rules are specified.
        public BattleTurnReport UseMonsterSkill(int skillIndex)
        {
            if (Outcome != BattleOutcome.InProgress)
                throw new InvalidOperationException("This battle has already ended.");
            if (skillIndex < 0 || skillIndex >= setup.monsterSkills.Count)
                throw new ArgumentOutOfRangeException(nameof(skillIndex));

            BattleSkill skill = setup.monsterSkills[skillIndex];
            var report = new BattleTurnReport
            {
                skill = skill,
                energyBefore = EnergyRemaining,
                reachedStages = new List<BattlePortraitStage>()
            };

            EnergyRemaining = Mathf.Max(0, EnergyRemaining - skill.energyDamage);
            PhysicalDamage += skill.physicalDamage;
            PleasureDamage += skill.pleasureDamage;
            ConfusionDamage += skill.confusionDamage;
            TurnsUsed++;
            TurnsRemaining--;

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

            if (EnergyRemaining == 0)
                Finish(BattleOutcome.MonsterVictory);
            else if (TurnsRemaining <= 0)
                Finish(BattleOutcome.TurnLimitReached);

            report.energyAfter = EnergyRemaining;
            report.turnsRemaining = TurnsRemaining;
            report.outcome = Outcome;
            return report;
        }

        public List<BattleStatusChange> PreviewStatusChanges(BattleOutcome assumedOutcome)
        {
            if (assumedOutcome == BattleOutcome.InProgress)
                throw new ArgumentException("Choose a finished outcome for a status preview.", nameof(assumedOutcome));

            float multiplier = assumedOutcome == BattleOutcome.MonsterVictory
                ? setup.victoryStatusMultiplier
                : setup.defeatStatusMultiplier;
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
                physicalDamage = PhysicalDamage,
                pleasureDamage = PleasureDamage,
                confusionDamage = ConfusionDamage,
                statusChanges = PreviewStatusChanges(outcome)
            };
        }
    }
}
