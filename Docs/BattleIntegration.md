# Battle scene integration

The player controls 桃香. After each chosen heroine action, the monster automatically responds with the next skill in its list. The scene uses both monster HP (`体力`) and a turn countdown (`残された時間`). The countdown is measured in turns, not seconds.

## Test in Unity

1. Open `Assets/Scenes/Battle.unity` and press Play.
2. Choose `攻撃`, `防御`, or `回復` in `桃香の行動`. The monster's next action is shown above its HP.
3. With the unchanged sample, choose `攻撃` four times: monster HP reaches zero, 桃香 wins with 40 Energy, and the defeated monster does not retaliate.
4. Restart Play and choose `防御` five times: time expires with monster HP remaining; 桃香 survives with 50 Energy and wins when the monster retreats.
5. To test defeat, stop Play, temporarily set **Starting Energy** to `10`, and Play again. One `攻撃` results in defeat after the monster responds. Restore your original setting afterward.
6. Choose Return on the result screen to load `MainMenu`.

For automatic logic checks, select **Tests > Battle > Run Combat Checks** from Unity's menu. It reports eight passing cases in the Console or throws the first failing check. It does not change scene or profile data.

Edit settings while Play is stopped. The session copies its setup at entry, so Inspector changes during a running battle do not change that session.

## Customize the sample

Select **Battle Scene Controller** in the Battle scene and expand **Standalone Setup**.

| Field | Effect |
| --- | --- |
| Heroine / Monster | IDs, display names, and portrait sprites |
| Starting Energy | 桃香's initial Energy and recovery cap |
| Starting Monster Health | Initial/max monster `体力` |
| Turn Limit | Starting `残された時間`, in turns |
| Heroine Skills | The player's buttons: monster damage, Energy cost, Energy recovery, and damage multiplier for the monster's response that turn |
| Monster Skills | Automatic enemy sequence; cycles in list order. Each skill reduces Energy and adds its physical/pleasure/confusion damage |
| Portrait Stages | Each threshold fires once, may change the heroine's sprite, and may add remaining turns |
| Status Rules | Starting status values and rates converting received damage into lasting changes |
| Heroine Victory / Defeat Status Multiplier | Outcome scaling from the player's perspective |
| Return Scene Name | Scene to load after results; the standalone sample uses `MainMenu` |

Example heroine skills: `攻撃` deals 30 monster damage; `防御` multiplies incoming Energy and all three damage types by `0.5` for that response; `回復` restores up to 25 Energy before the monster responds. All sample values are provisional and editable. Keep at least one zero-cost heroine skill so the player can always act at low Energy. A skill's cost must leave at least one Energy before recovery; otherwise its button is disabled.

The current monster behavior cycles through **Monster Skills** in order. Reorder, add, or remove entries to change its pattern; change `BattleSession.NextMonsterSkill` for a different selection strategy. Only heroine skills are selectable by the player.

## End conditions and status changes

- Monster `体力` reaches zero: `BattleOutcome.HeroineVictory`; skip its response.
- 桃香's Energy reaches zero: `BattleOutcome.MonsterVictory` (player defeat).
- `残された時間` reaches zero with Energy remaining: `BattleOutcome.TurnLimitReached`; the monster retreats and 桃香 wins. This preserves the earlier time-limit outcome as a provisional rule.
- A portrait stage adds its bonus time before the time-limit check. Energy depletion takes priority when Energy and time both reach zero on a monster response.

Use `result.HeroineWon` to test player victory. The result also carries `monsterHealthRemaining`, `energyRemaining`, remaining/used turns, the three received-damage totals, and each status's before/delta/after values.

Status delta is `(physical × physicalRate + pleasure × pleasureRate + confusion × confusionRate) × outcomeMultiplier`, rounded to an integer. Guard reduces the damage that contributes to both portrait stages and status changes. The live status display previews a **heroine victory**; the final result uses the actual outcome. The sample multipliers are `0.25` for heroine victory and `1` for heroine defeat, preserving the previous damage-to-status scale while making the perspective explicit.

## Enter from another scene

Create a `Train.Battle.BattleSetup` and call `BattleFlow.Enter(setup, callback)`. This setup replaces the scene's Standalone Setup. Both Battle and the return scene must be enabled in the build scene list. Empty `returnSceneName` means return to the calling scene.

```csharp
using System.Collections.Generic;
using Train.Battle;
using UnityEngine;

// Inside your launch method; obtain these sprites and current status from your own data.
var setup = new BattleSetup
{
    heroine = new BattleCharacter { id = "toka", displayName = "桃香", portrait = heroineSprite },
    monster = new BattleCharacter { id = "scorpion", displayName = "怪人", portrait = monsterSprite },
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
        new BattleSkill { id = "attack", displayName = "攻撃", energyDamage = 25, physicalDamage = 50 }
    },
    statusRules = new List<BattleStatusRule>
    {
        new BattleStatusRule
        {
            statusId = "fatigue", displayName = "疲労度", startingValue = currentFatigue,
            physicalDamageRate = 0.1f
        }
    }
};

BattleFlow.Enter(setup, result =>
{
    Debug.Log(result.HeroineWon ? "桃香 wins" : "桃香 loses");
    foreach (BattleStatusChange change in result.statusChanges)
        Debug.Log($"{change.statusId}: {change.before} -> {change.after}");
    // Apply change.after to your character store, then save it there.
});
```

The callback runs after the return scene loads. `BattleFlow.LastResult` also holds the result. Combat returns status changes; the caller applies and persists them. The current `PlayerProfile` does not have these status fields. Avoid callbacks that access scene objects destroyed when entering Battle.

For callers of the earlier prototype: replace `UseMonsterSkill` with `UseHeroineSkill`, provide `heroineSkills`, and use `heroineVictoryStatusMultiplier` / `heroineDefeatStatusMultiplier`. Previously serialized scene multiplier values migrate to the matching heroine outcome automatically.
