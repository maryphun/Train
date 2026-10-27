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
        CheckDeferredActionEffects();
        CheckDeferredFinishingAttack();
        CheckPhasedActionGuards();
        CheckDeferredStagesAndOutcome();
        CheckInterruptedTurnCompletion();
        Debug.Log("Battle checks passed: 13 cases (combat rules, deferred action effects, response order, terminal outcomes and turn isolation).");
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

    private static void CheckDeferredActionEffects()
    {
        BattleSetup setup = Setup();
        setup.heroineSkills[0].energyCost = 5;
        var session = new BattleSession(setup);
        BattleTurnReport report = session.BeginHeroineSkill(0);
        Require(session.IsResolvingTurn && session.MonsterHealthRemaining == 100 &&
            session.EnergyRemaining == 100 && session.TurnsUsed == 0 && session.PhysicalDamage == 0,
            "Selecting an action must reserve it without applying either character's effects.");
        session.ApplyHeroineAction(report);
        Require(session.MonsterHealthRemaining == 70 && session.EnergyRemaining == 95 &&
            session.PhysicalDamage == 0 && session.TurnsUsed == 1 && session.TurnsRemaining == 4,
            "The heroine effect step must apply only her own damage/cost and consume one turn.");
        Require(report.heroineActionResolved && !report.monsterActionResolved && report.energyAfter == 95 &&
            session.NextMonsterSkill.id == "attack", "The selected monster response must remain queued.");
        session.ApplyMonsterResponse(report);
        Require(session.EnergyRemaining == 70 && session.PhysicalDamage == 50 &&
            report.monsterActionResolved && report.incomingEnergyDamage == 25 && !session.IsResolvingTurn &&
            session.NextMonsterSkill.id == "pleasure", "Enemy effects must apply only in the response step.");
    }

    private static void CheckDeferredFinishingAttack()
    {
        BattleSetup setup = Setup();
        setup.startingMonsterHealth = 30;
        var session = new BattleSession(setup);
        BattleTurnReport report = session.BeginHeroineSkill(0);
        Require(session.Result == null && session.MonsterHealthRemaining == 30,
            "A selected finishing move must wait for its effect step.");
        session.ApplyHeroineAction(report);
        Require(session.Result.HeroineWon && report.monsterSkill == null && !session.IsResolvingTurn &&
            !report.monsterActionResolved && session.EnergyRemaining == 100,
            "A finishing heroine effect must end the turn and skip the enemy response.");
        ExpectFailure<InvalidOperationException>(() => session.ApplyMonsterResponse(report));
    }

    private static void CheckPhasedActionGuards()
    {
        var session = new BattleSession(Setup());
        BattleTurnReport report = session.BeginHeroineSkill(0);
        Require(!session.CanUseHeroineSkill(0), "Input must remain locked while a turn is pending.");
        ExpectFailure<InvalidOperationException>(() => session.BeginHeroineSkill(1));
        ExpectFailure<InvalidOperationException>(() => session.ApplyMonsterResponse(report));
        ExpectFailure<ArgumentException>(() => session.ApplyHeroineAction(new BattleTurnReport()));
        session.ApplyHeroineAction(report);
        ExpectFailure<InvalidOperationException>(() => session.ApplyHeroineAction(report));
        session.ApplyMonsterResponse(report);
        ExpectFailure<InvalidOperationException>(() => session.ApplyMonsterResponse(report));
        Require(session.EnergyRemaining == 75 && session.TurnsUsed == 1 && session.CanUseHeroineSkill(0),
            "Rejected duplicate phases must not apply extra damage or consume extra turns.");
    }

    private static void CheckDeferredStagesAndOutcome()
    {
        BattleSetup setup = Setup();
        setup.turnLimit = 1;
        setup.portraitStages.Add(new BattlePortraitStage
        { label = "stage", damageType = BattleDamageType.Physical, threshold = 50, bonusTurns = 1 });
        var session = new BattleSession(setup);
        BattleTurnReport report = session.BeginHeroineSkill(0);
        session.ApplyHeroineAction(report);
        Require(session.TurnsRemaining == 0 && session.Result == null && report.reachedStages.Count == 0,
            "Time expiry and response stages must wait for the enemy effect step.");
        session.ApplyMonsterResponse(report);
        Require(session.TurnsRemaining == 1 && session.Result == null && report.reachedStages.Count == 1,
            "Enemy damage stages must add time before checking expiry.");

        setup.startingEnergy = 10;
        setup.portraitStages.Clear();
        session = new BattleSession(setup);
        report = session.BeginHeroineSkill(0);
        session.ApplyHeroineAction(report);
        Require(session.Result == null && session.EnergyRemaining == 10,
            "Pending enemy damage must not end the battle early.");
        session.ApplyMonsterResponse(report);
        Require(session.Result.MonsterWon && session.Result.energyRemaining == 0,
            "Enemy Energy depletion must still take priority over time expiry.");
    }

    private static void CheckInterruptedTurnCompletion()
    {
        var baseline = new BattleSession(Setup());
        BattleTurnReport expected = baseline.UseHeroineSkill(0);
        for (int completedSteps = 0; completedSteps < 2; completedSteps++)
        {
            var session = new BattleSession(Setup());
            BattleTurnReport report = session.BeginHeroineSkill(0);
            if (completedSteps == 1) session.ApplyHeroineAction(report);
            session.CompleteTurn(report);
            Require(session.EnergyRemaining == baseline.EnergyRemaining &&
                session.MonsterHealthRemaining == baseline.MonsterHealthRemaining &&
                session.PhysicalDamage == baseline.PhysicalDamage && session.TurnsUsed == 1 &&
                report.incomingEnergyDamage == expected.incomingEnergyDamage && !session.IsResolvingTurn,
                "Finishing an interrupted presentation must apply each remaining effect exactly once.");
        }
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
