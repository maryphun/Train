using System;
using System.Collections.Generic;
using Train.Battle;
using UnityEditor;
using UnityEngine;

// Run from Unity's Tests menu. These checks do not load scenes or alter saved game data.
public static class BattleSessionChecks
{
    [MenuItem("Tests/Battle/Run Combat Checks")]
    public static void Run()
    {
        CheckHeroineAttackAndAutomaticResponse();
        CheckFinishingAttack();
        CheckGuardExpires();
        CheckRecoveryCap();
        CheckTimeLimit();
        CheckEnergyDefeat();
        CheckPortraitBonusOnlyOnce();
        CheckInputAndSetupIsolation();
        Debug.Log("Battle checks passed: 8 cases (heroine controls, automatic responses, HP victory, guard, recovery, time, defeat, status results and setup isolation).");
    }

    private static BattleSetup Setup()
    {
        return new BattleSetup
        {
            heroine = new BattleCharacter { id = "heroine", displayName = "桃香" },
            monster = new BattleCharacter { id = "monster", displayName = "怪人" },
            startingEnergy = 100,
            startingMonsterHealth = 100,
            turnLimit = 5,
            heroineSkills = new List<BattleHeroineSkill>
            {
                new BattleHeroineSkill { id = "attack", displayName = "攻撃", monsterDamage = 30 },
                new BattleHeroineSkill { id = "guard", displayName = "防御", incomingDamageMultiplier = 0.5f },
                new BattleHeroineSkill { id = "recover", displayName = "回復", energyRecovery = 25 }
            },
            monsterSkills = new List<BattleSkill>
            {
                new BattleSkill { id = "attack", displayName = "攻撃", energyDamage = 25, physicalDamage = 50 },
                new BattleSkill { id = "pleasure", displayName = "快楽", energyDamage = 15, pleasureDamage = 60 },
                new BattleSkill { id = "confusion", displayName = "混乱", energyDamage = 20, confusionDamage = 50 }
            },
            statusRules = new List<BattleStatusRule>
            {
                new BattleStatusRule { statusId = "fatigue", displayName = "疲労度", startingValue = 7, physicalDamageRate = 0.1f }
            }
        };
    }

    private static void CheckHeroineAttackAndAutomaticResponse()
    {
        var session = new BattleSession(Setup());
        BattleTurnReport report = session.UseHeroineSkill(0);
        Require(session.MonsterHealthRemaining == 70 && session.EnergyRemaining == 75,
            "Attack must damage the monster, followed by its automatic attack.");
        Require(report.heroineSkill.id == "attack" && report.monsterSkill.id == "attack" &&
            session.TurnsUsed == 1 && session.TurnsRemaining == 4 && session.NextMonsterSkill.id == "pleasure",
            "One player action must resolve one turn and advance the monster sequence.");
    }

    private static void CheckFinishingAttack()
    {
        BattleSetup setup = Setup();
        setup.startingMonsterHealth = 30;
        var session = new BattleSession(setup);
        BattleTurnReport report = session.UseHeroineSkill(0);
        Require(report.monsterSkill == null && session.EnergyRemaining == 100 && session.PhysicalDamage == 0,
            "A defeated monster must not retaliate.");
        Require(session.Result.HeroineWon && !session.Result.MonsterWon &&
            session.Result.outcome == BattleOutcome.HeroineVictory && session.Result.monsterHealthRemaining == 0,
            "A finishing attack must return a heroine victory and zero monster HP.");
        Require(!session.CanUseHeroineSkill(0), "Finished combat must reject further actions.");
        ExpectFailure<InvalidOperationException>(() => session.UseHeroineSkill(0));
    }

    private static void CheckGuardExpires()
    {
        var session = new BattleSession(Setup());
        session.UseHeroineSkill(1);
        Require(session.EnergyRemaining == 88 && session.PhysicalDamage == 25,
            "Guard must halve both Energy damage and accumulated damage (rounded).");
        session.UseHeroineSkill(0);
        Require(session.EnergyRemaining == 73 && session.PleasureDamage == 60,
            "Guard must expire before the following turn.");
    }

    private static void CheckRecoveryCap()
    {
        var session = new BattleSession(Setup());
        session.UseHeroineSkill(0);
        BattleTurnReport report = session.UseHeroineSkill(2);
        Require(report.energyRecovered == 25 && session.EnergyRemaining == 85,
            "Recovery must happen before the monster response.");
        report = session.UseHeroineSkill(2);
        Require(report.energyRecovered == 15 && session.EnergyRemaining == 80,
            "Recovery must stop at starting Energy.");
    }

    private static void CheckTimeLimit()
    {
        BattleSetup setup = Setup();
        setup.turnLimit = 1;
        var session = new BattleSession(setup);
        session.UseHeroineSkill(0);
        Require(session.Result.outcome == BattleOutcome.TurnLimitReached && session.Result.HeroineWon &&
            session.Result.monsterHealthRemaining == 70, "Time expiry must end combat even if the monster still has HP.");
        BattleStatusChange status = session.Result.statusChanges[0];
        Require(status.before == 7 && status.delta == 1 && status.after == 8,
            "Survival must use the heroine victory status multiplier.");
    }

    private static void CheckEnergyDefeat()
    {
        BattleSetup setup = Setup();
        setup.startingEnergy = 10;
        setup.turnLimit = 1;
        var session = new BattleSession(setup);
        session.UseHeroineSkill(0);
        Require(session.Result.MonsterWon && !session.Result.HeroineWon && session.Result.energyRemaining == 0,
            "Energy depletion must be defeat, including on the final turn.");
        Require(session.Result.statusChanges[0].after == 12,
            "Defeat must use the heroine defeat status multiplier.");
    }

    private static void CheckPortraitBonusOnlyOnce()
    {
        BattleSetup setup = Setup();
        setup.startingMonsterHealth = 1000;
        setup.turnLimit = 2;
        setup.monsterSkills.RemoveRange(1, 2);
        setup.portraitStages.Add(new BattlePortraitStage
        { label = "stage", damageType = BattleDamageType.Physical, threshold = 100, bonusTurns = 2 });
        var session = new BattleSession(setup);
        session.UseHeroineSkill(0);
        BattleTurnReport report = session.UseHeroineSkill(0);
        Require(report.reachedStages.Count == 1 && session.TurnsRemaining == 2 && session.Result == null,
            "A new damage stage must extend remaining time before checking time expiry.");
        report = session.UseHeroineSkill(0);
        Require(report.reachedStages.Count == 0 && session.TurnsRemaining == 1,
            "The same stage must not add time again.");
    }

    private static void CheckInputAndSetupIsolation()
    {
        BattleSetup setup = Setup();
        setup.heroineSkills[0].energyCost = 100;
        var session = new BattleSession(setup);
        Require(!session.CanUseHeroineSkill(0) && session.CanUseHeroineSkill(1),
            "Unaffordable skills must be disabled while free skills remain usable.");
        ExpectFailure<InvalidOperationException>(() => session.UseHeroineSkill(0));
        ExpectFailure<ArgumentOutOfRangeException>(() => session.UseHeroineSkill(99));
        Require(session.TurnsUsed == 0 && session.EnergyRemaining == 100, "Rejected input must not consume a turn.");
        setup.heroineSkills[1].monsterDamage = 999;
        setup.monsterSkills[0].energyDamage = 999;
        session.UseHeroineSkill(1);
        Require(session.MonsterHealthRemaining == 100 && session.EnergyRemaining == 88,
            "A running battle must not share mutable skill data with its caller.");
        setup.heroineSkills.Clear();
        ExpectFailure<ArgumentException>(() => new BattleSession(setup));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Battle check failed: " + message);
    }

    private static void ExpectFailure<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Battle check expected " + typeof(T).Name);
    }
}
