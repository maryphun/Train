# Battle scene integration

`Assets/Scenes/Battle.unity` can be played directly with the sample setup on its **Battle Scene Controller** object. The sample values are placeholders, not approved balancing. Change characters, skills, Energy, turn limit, portrait stages, and status rules in that Inspector.

To enter from another scene, create a `Train.Battle.BattleSetup` and call `BattleFlow.Enter(setup, callback)`. The setup is copied, so the caller may reuse or edit its original after entry. `Battle.unity` and the return scene must be enabled in Unity's build scene list. If `returnSceneName` is empty, combat returns to the scene from which `Enter` was called.

```csharp
using System.Collections.Generic;
using Train.Battle;
using UnityEngine;

// Example only: populate names, sprites, values, and starting statuses from your game data.
var setup = new BattleSetup
{
    heroine = new BattleCharacter { id = "toka", displayName = "桃香", portrait = heroineSprite },
    monster = new BattleCharacter { id = "scorpion", displayName = "怪人", portrait = monsterSprite },
    startingEnergy = 100,
    turnLimit = 5,
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
    foreach (BattleStatusChange change in result.statusChanges)
    {
        // Apply change.after to your character store by change.statusId, then save it there.
        Debug.Log($"{change.statusId}: {change.before} -> {change.after} ({change.delta:+#;-#;0})");
    }
});
```

The callback runs after the return scene loads. `BattleFlow.LastResult` also holds the result. Combat deliberately does **not** write `PlayerProfile` or a save file: the current profile has no persistent heroine-status fields, and the caller owns persistence.

Current provisional loop: each monster skill uses one turn, reduces PurePri Energy, and accumulates separate physical, pleasure, and confusion damage. An Energy of zero wins for the monster; exhausting turns first ends the battle. A portrait stage can switch the heroine portrait and add turns when one damage total crosses its threshold. There is no magical-girl counteraction until its behavior is defined.

Each status rule converts accumulated damage into a lasting change using `(physical × physicalRate + pleasure × pleasureRate + confusion × confusionRate) × outcomeMultiplier`, rounded to an integer. The sample Inspector has victory multiplier `1`, turn-limit multiplier `0.25`, and rules for `淫乱度`, `闇堕度`, and `疲労度`. Replace these freely. Battle-time damage totals are included in `BattleResult`, but are not themselves saved as lasting statuses.
