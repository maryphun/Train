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
| Background | Optional sprite stretched across the battle background Image; empty means black |
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

## Prepare a battle before dialogue

Use `BattleFlow.Setup(setup, callback)` to validate and store a copy of the battle data without
loading a scene. `BattleFlow.Enter` still prepares the data and immediately loads Battle.
The next Battle scene load consumes the prepared data once, including when DialogueFlow
loads Battle after dialogue. Intermediate scenes do not consume it.

For TitleManager's existing prologue flow, `SetupBattle(enemyData)` now calls `BattleFlow.Setup`:

```csharp
if (DialogueFlow.Setup("Prologue", BattleFlow.SceneName))
{
    SetupBattle(enemyData);
    SceneTransitionManager.Instance.LoadScene("Dialogue", 0.75f);
}
```

This produces Title → Dialogue → Battle. Battle's result return uses
`BattleSetup.returnSceneName`, independently of DialogueFlow's destination.
An empty battle return scene is resolved when `BattleFlow.Setup` is called.
Assign the enemy data and heroine portrait on TitleManager in the Inspector.
Only one pending battle setup is allowed. If the flow is abandoned before Battle loads,
call `BattleFlow.CancelSetup()` to release it; cancellation returns false after consumption.

## Background setup

Set `BattleSetup.background` to the Sprite you want to show. An empty reference produces an
opaque black background. The sprite stretches to fill the background Image, as requested.
TitleManager exposes **Battle Background** in its Inspector and passes it into its battle setup.

```csharp
setup.background = backgroundSprite; // null uses black
BattleFlow.Setup(setup);
```

## Design the Canvas in Unity

Create your Canvas in `Assets/Scenes/Battle.unity` while Play Mode is stopped. Add
`BattleUIReferences` to the Canvas, assign its UI fields, and drag that component into
**Scene UI** on the existing **Battle Scene Controller**. When Scene UI is assigned, the
controller uses those objects instead of creating a second Canvas. Missing required references
are reported in the Console. Leaving Scene UI empty retains the generated prototype interface.

The existing prototype uses Screen Space Overlay and Scale With Screen Size at 1920×1080;
you can use that configuration as your starting point. The controller supplies an EventSystem
if one is missing. It updates data and input without changing your authored text fonts, colors,
button dimensions or panel arrangement.

Required connections:

- **Background Image:** use an Image directly under the Canvas, stretched in both directions
  with zero offsets. Its sprite/color come from battle setup, and it does not block raycasts.
- **Action Popup:** your centered window. Anchor and pivot it at the middle of the screen.
- **Skill Button Parent:** a container inside the window for generated skill buttons.
- **Skill Button Template:** a prefab, or an inactive scene template, with `Button` and
  `BattleSkillButtonUI`. Assign the template's TMP **Label**; its TMP **Values** field is optional.
  Give the template a `LayoutElement` with your chosen preferred dimensions.
- **Result Overlay** and **Return Button:** the controller opens the overlay at the end of
  presentation and connects the button to battle return automatically.

Portraits, name/number labels, received-damage totals, status previews, log text, result text
and the cut-in Image are optional. Assigned gauge Images must use **Image Type = Filled**;
the controller changes `fillAmount`, and you choose their fill direction and styling.

Gauge changes now ease over **0.5 seconds**. Both Energy and enemy HP show their initial
values instantly; subsequent changes animate from the value currently visible. On **Battle Scene
Controller > Gauge Animation**, adjust **Gauge Animation Duration** and **Gauge Ease** (default
Out Cubic). A duration of zero applies values instantly. Unchanged UI refreshes do not restart a
transition, and a new value cancels/replaces the previous tween from its current displayed fill.
This also supports the generated prototype gauges. Tweens use unscaled time and stop when the
controller is disabled; re-enabling draws the current values immediately. Battle calculations and
the timing of gauge updates remain as before.

For a vertically expanding window, add a **Vertical Layout Group** and **Content Size Fitter**
to the popup, and set its Vertical Fit to **Preferred Size**. Add another Vertical Layout Group
to the button container. The popup's Layout Group sizes the container from its preferred height;
the container does not need its own Content Size Fitter for this arrangement. Disable **Child
Force Expand Height** on both Layout Groups. The button template's Layout Element supplies its
preferred height. Configure the popup's width, padding and spacing in Unity. Avoid having a parent
Layout Group and a Content Size Fitter both control the same object's size on the same axis.
An inactive scene template is excluded from the visible layout; the controller creates one
visible clone for each `heroineSkills` entry.

The saved Battle scene now uses the authored **wide button in a vertical list**, as confirmed
by the user. `Action pop up` contains `Title` and the new `Skill Buttons` container. The latter
is assigned as **Skill Button Parent** and holds the inactive template. Both objects have
Vertical Layout Groups; the popup's Content Size Fitter uses **Vertical Fit = Preferred Size**
and **Horizontal Fit = Unconstrained**, keeping its existing width and centered vertical pivot.
The template's Layout Element preserves its **600 × 65** size and existing normal/pressed sprites.

The initial layout values come from the authored single-button arrangement: title size
598.15 × 74.769, approximately 24 units above the title and 72 below the button (rounded to
whole units for Unity's padding fields), and 12.932455 units between the title and button.
That same gap separates subsequent buttons. The container's **Min Height = 65** keeps room
for one row while its template is inactive; additional clones increase its preferred height.
To adjust the design in Unity, edit popup padding, `Skill Buttons` spacing, and the template's
Layout Element preferred dimensions. Layout Groups drive their child RectTransforms.

The action popup appears only while the battle is awaiting the player's choice. It hides after
a skill is selected and stays hidden through the heroine presentation, enemy response and
their waits. It reappears for the next choice, or stays hidden when the battle ends.
When a popup has a CanvasGroup, the controller also updates alpha, interactable and
blocksRaycasts so a hidden Inspector state does not leave the window invisible or unclickable.

The turn counter uses `LocalizationManager.Localize("Battle.TurnLeft", remainingTurns)`.
The current Japanese text is `残り{0}ターン`; its number refreshes after each resolved turn,
including bonus turns, and when the localization language changes. Spell cut-ins and action
animation events are optional and can remain empty until those assets are ready.

On Battle Scene Controller, **Heroine Action Delay** and **Monster Action Delay** are additional
waits after each respective cut-in (or after the action event if there is no cut-in). Both default
to zero until you choose durations. With **Spell Animation** assigned, each wait begins after
the animation completes. **Cut In Duration** controls only the fallback **Cut In Image**, retaining
its existing 0.8-second default. Presentation and waits use unscaled time. A defeated enemy skips
its response and wait.

## Ability graphic and spell animation

Both `BattleHeroineSkill` and `BattleSkill` have a **Cut In** Sprite and an **Animation Direction**.
Set them separately for each ability while building the setup, before calling `BattleFlow.Setup`
or `BattleFlow.Enter`. Existing callers remain compatible: the default direction is **Right To Left**,
matching the authored example. An empty Cut In skips the spell animation, while the action event
and configured additional delay still run.

```csharp
// Assign your own ability sprites to the skills already present in the setup.
setup.heroineSkills[0].cutIn = heroineAbilityGraphic;
setup.heroineSkills[0].animationDirection = BattleAnimationDirection.RightToLeft;

setup.monsterSkills[0].cutIn = enemyAbilityGraphic;
setup.monsterSkills[0].animationDirection = BattleAnimationDirection.LeftToRight;
```

For a directly opened Battle scene, edit these fields on **Battle Scene Controller > Standalone
Setup > Heroine Skills / Monster Skills**. When entering through TitleManager, assign `cutIn`
and `animationDirection` in its `SetupBattle` skill initializers. Its current `attack` and `attack2`
use **Sprite Punch** and **Sprite Kick** respectively. Both Inspector fields are now saved in
Title.unity, connected to `CG_Battle_Punch` and `CG_Battle_Kick`. A null source field becomes
a null `cutIn` and skips animation. Save reference assignments outside Play Mode; to pick up
the repaired scene from an already open Editor, stop Play Mode, reload Title and start a new battle.
The sample Image sprite in SpellAnimation is a design preview, and does
not serve as a fallback for an ability with no graphic.

On **Battle UI References**, **Spell Animation** is now connected to the authored component on
`Back Fill (Ability)`. Its existing **Canvas Group**, **Ability Name**, and moving **Image**
references supply the overlay, label and ability artwork. The controller passes each selected
skill's name, sprite and direction to `SpellAnimation.InitAnimation`, waits until `IsPlaying` is
false, applies the action delay, and proceeds to the enemy or next player choice/result.
The old **Cut In Image** reference is cleared because that Image was the dim background;
the fallback remains available for scenes without a SpellAnimation component.

The right-to-left path is unchanged: x `955 → 100 → -100 → -955`. Left-to-right mirrors it:
`-955 → -100 → 100 → 955`. The supplied artwork is not flipped. The original phase durations
remain 0.25 / 0.5 / 1 / 0.5 / 0.25 seconds, totaling **2.5 seconds**. Your ribbon Animator and
AbilityEffect clip remain independent. Replaying cancels the previous tween sequence; completion,
explicit `EndAnimation`, and disabling the object clear animation state and raycast blocking.
The graphic now follows one continuous motion curve: a smooth entry, constant-speed center glide,
and smooth exit. Matching tangents remove speed jumps at the center boundaries, and the movement
fills the entrance/exit fades so the earlier 0.2-second stage pauses disappear. Overlay and graphic
alpha use In Out Sine easing. Positions, mirrored directions and the total duration are preserved.
The scene's enemy HP fill is corrected from Sliced to Filled so UI validation and `fillAmount` work.

Verification: runtime and Editor assemblies compile, 51 headless checks cover graphics/names,
continuous mirrored motion, speed at the glide boundaries, fades, timing, setup copies, cancellation,
presentation order and gauge initialization/interpolation/retargeting/lifecycle behavior. Serialized
scene connections and the saved 0.5-second gauge timing are checked. Unity MCP is unavailable,
so live Play Mode playback remains unverified.

Connect future animation methods to **On Heroine Action Started** and **On Monster Action
Started**. They fire once for the corresponding presentation phase. Animation scripts can read
`BattleSceneController.CurrentTurnReport` for the selected skills and before/after values.
The existing combat calculation runs when a skill is selected; the events and waits control
presentation and input. No animation assets or new combat rules are added by these hooks.

For callers of the earlier prototype: replace `UseMonsterSkill` with `UseHeroineSkill`, provide `heroineSkills`, and use `heroineVictoryStatusMultiplier` / `heroineDefeatStatusMultiplier`. Previously serialized scene multiplier values migrate to the matching heroine outcome automatically.
